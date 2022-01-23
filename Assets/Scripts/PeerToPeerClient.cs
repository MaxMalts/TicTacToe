using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



namespace Network {

	public class PeerToPeerClient : MonoBehaviour {

		public class PackageReceiveEvent : UnityEvent<byte[]> { }
		public PackageReceiveEvent PackageReceived { get; } = new PackageReceiveEvent();

		public class DisconnectedEvent : UnityEvent { }
		public DisconnectedEvent Disconnected { get; } = new DisconnectedEvent();

		public bool Connected {
			get {
				return Convert.ToBoolean(connected);
			}
		}

		public static bool NetworkAvailable {
			get {
				return UdpBroadcastClient.GetWifiIP() != null;
			}
		}

		const string beaconMessage = "PeerToPeerClient-beacon";
		const int listenPort = 48888;
		const int beaconIntervalMs = 1000;
		readonly byte[] pingMessage = Encoding.UTF8.GetBytes("PeerToPeerClient-ping");
		const int pingIntervalMs = 1000;
		const int disconnectTimeoutMs = 7000;

		UdpBroadcastClient broadcastClient;
		TcpListener listener;
		TcpClient tcpClient;
		NetworkStreamWrapper stream;
		volatile int connected = 0;  // bool
		TaskCompletionSource<IPAddress> receiveBeacon;
		object connectingLock;
		bool disconnectedEventPending = false;
		Stopwatch pingTimer = new Stopwatch();

		bool IsReceiving {
			get {
				return readingTask != null && !readingTaskCT.IsCancellationRequested;
			}
		}
		//Stopwatch disconnectTimer = new Stopwatch();

		Task connectTask;
		CancellationTokenSource connectTaskCT;

		ConcurrentQueue<byte[]> receivedPackages;
		Task readingTask;
		CancellationTokenSource readingTaskCT;

		bool disposed = false;


		/// <summary>
		/// Searches for other PeerToPeerClient in local network and connects to it.
		/// If already connected, disconnects first.
		/// </summary>
		/// <returns>Task which returns when connected.</returns>
		public Task ConnectToOtherClient() {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}

			if (Connected) {
				Disconnect();
			}

			Reset();

			if (connectTask == null ||
				connectTask.Status != TaskStatus.Running) {

				receiveBeacon = new TaskCompletionSource<IPAddress>();

				broadcastClient.MessageReceived.AddListener(OnBroadcastReceived);
				try {
					broadcastClient.StartListeningBroadcast();
				} catch (NoNetworkException exception) {
					throw new NoNetworkException("No network for sending and receiving beacons.", exception);
				}

				connectTaskCT = new CancellationTokenSource();
				CancellationToken token = connectTaskCT.Token;
				connectTask = Task.Run(() => {
					SearchAndConnect(token);
				}, token);
			}

			return connectTask;
		}

		public void Send(byte[] data, int? offset = null, int? size = null) {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}
			if (!Connected) {
				throw new NotConnectedException("Called Send but not connected to other client.");
			}

			try {
				stream.Write(data, offset, size);

			} catch (IOException exception) when (
				exception.InnerException.GetType() == typeof(SocketException)
			) {
				if (Interlocked.Exchange(ref connected, 0) == 1) {
					disconnectedEventPending = true;
				}
				throw new NotConnectedException("Socket error, PeerToPeerClient was disconnected.",
						exception);
			}

#if NETWORK_LOG
			if (!StartsWith(data, pingMessage)) {
				UnityEngine.Debug.Log("PeerToPeerClient sent package.");
			}
#endif
		}

		//public byte[] Read() {
		//	if (!connected) {
		//		Debug.LogWarning("Called Read but not connected to other client.");
		//		return new byte[] { };
		//	}

		//	return stream.Read();
		//}

		//public async Task<byte[]> ReadAsync() {
		//	if (!connected) {
		//		Debug.LogWarning("Called ReadAsync but not connected to other client.");
		//		return new byte[] { };
		//	}

		//	return await stream.ReadAsync();
		//}


		/// <summary>
		/// Starts reading packages and invoking PackageReceived event.
		/// When client disconnects, it automatically stops receiving, so after
		/// reconnect you should call this method to start receiving again.
		/// </summary>
		public void StartReceiving() {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}
			if (!Connected) {
				throw new NotConnectedException("Called StartReceiving but not connected to other client.");
			}
			if (IsReceiving) {
				return;
			}

			readingTaskCT = new CancellationTokenSource();
			CancellationToken token = readingTaskCT.Token;
			readingTask = Task.Run(() => {
				// TcpClient.ReceiveTimeout does the thing
				//
				//				disconnectTimer.Restart();
				//				bool disconnected = false;

				//				while (!disposed) {
				//					if (disconnected ||
				//						disconnectTimer.ElapsedMilliseconds > disconnectTimeoutMs) {

				//						connected = false;
				//						disconnectedEventPending = true;
				//						disconnectTimer.Reset();
				//						readingTaskCT.Cancel();
				//					}

				//					token.ThrowIfCancellationRequested();

				//					byte[] data;
				//					try {
				//						data = stream.Read();

				//					} catch (IOException exception) when (
				//						exception.InnerException.GetType() == typeof(SocketException)
				//					) {
				//						disconnected = true;
				//						UnityEngine.Debug.LogError(((SocketException)exception.InnerException).ErrorCode);
				//						continue;

				//					} catch (Exception) {
				//						disconnectTimer.Reset();
				//						throw;
				//					}

				//					disconnectTimer.Restart();

				//					if (StartsWith(data, pingMessage)) {
				//						continue;
				//					}

				//					receivedPackages.Enqueue(data);

				//#if NETWORK_LOG
				//					UnityEngine.Debug.Log("PeerToPeerClient received package.");
				//#endif
				//				}

				//				disconnectTimer.Reset();


				while (true) {
					if (disposed) {
						readingTaskCT.Cancel();
					}

					token.ThrowIfCancellationRequested();

					byte[] data;
					try {
						data = stream.Read();

					} catch (IOException exception) when (
						exception.InnerException.GetType() == typeof(SocketException)
					) {
						if (Interlocked.Exchange(ref connected, 0) == 1) {
							disconnectedEventPending = true;
						}
						readingTaskCT.Cancel();
						continue;
					}

					if (!StartsWith(data, pingMessage)) {
						receivedPackages.Enqueue(data);
#if NETWORK_LOG
						UnityEngine.Debug.Log("PeerToPeerClient received package.");
#endif
					}
				}
			}, token);
		}

		public void StopReceiving() {
			if (disposed) {
				return;
			}
			if (!IsReceiving) {
				return;
			}

			readingTaskCT.Cancel();
		}

		public void Disconnect() {
			if (Interlocked.Exchange(ref connected, 0) == 1) {
				disconnectedEventPending = true;
			}

			connectTaskCT.Cancel();
			StopReceiving();
			listener.Stop();
			pingTimer.Reset();
			tcpClient?.Close();
		}

		public void Reset() {
			broadcastClient = UdpBroadcastClient.Instance;
			listener?.Stop();
			listener = new TcpListener(IPAddress.Any, listenPort);
			tcpClient?.Close();
			tcpClient = null;
			stream = null;
			connected = 0;
			receiveBeacon = null;
			connectingLock = new object();
			disconnectedEventPending = false;
			pingTimer = new Stopwatch();

			connectTask = null;
			connectTaskCT?.Cancel();
			connectTaskCT = null;

			receivedPackages = new ConcurrentQueue<byte[]>(); ;
			readingTask = null;
			readingTaskCT?.Cancel();
			readingTaskCT = null;
			UnityEngine.Debug.Log("Reset");
		}

		void Dispose() {
			disposed = true;
			Disconnect();
		}

		void Update() {
			if (!disposed) {
				byte[] package;
				while (IsReceiving && receivedPackages.TryDequeue(out package)) {
					PackageReceived.Invoke(package);
				}

				if (disconnectedEventPending) {
					pingTimer.Reset();
					Disconnect();
					Disconnected.Invoke();

					disconnectedEventPending = false;
				}

				if (Connected) {
					if (!pingTimer.IsRunning) {
						pingTimer.Restart();
					}

					if (pingTimer.ElapsedMilliseconds >= pingIntervalMs) {
						try {
							Send(pingMessage);  // will handle disconnection by itself
						} catch (NotConnectedException) {
							if (Interlocked.Exchange(ref connected, 0) == 1) {
								disconnectedEventPending = true;
							}
							pingTimer.Reset();
						}

						pingTimer.Restart();
					}
				}
			}
		}

		void SearchAndConnect(CancellationToken token) {
			try {
				if (disposed) {
					token.ThrowIfCancellationRequested();
					throw new ObjectDisposedException("PeerToPeerClient");
				}
				token.ThrowIfCancellationRequested();

				listener.Start();
				listener.BeginAcceptTcpClient(OnTcpAccept, listener);

				while (!Connected) {
					if (disposed) {
						token.ThrowIfCancellationRequested();
						throw new ObjectDisposedException("PeerToPeerClient");
					}
					token.ThrowIfCancellationRequested();

					try {
						broadcastClient.Send(beaconMessage);
					} catch (ObjectDisposedException exception) {
						token.ThrowIfCancellationRequested();
						throw new ObjectDisposedException("broadcastClient needed to search " +
							"for other PeerToPeerClient disposed", exception);
					}
					token.ThrowIfCancellationRequested();

					Stopwatch beaconWaitTime = new Stopwatch();
					beaconWaitTime.Start();
					if (receiveBeacon.Task.Wait(beaconIntervalMs)) {
						Assert.IsTrue(receiveBeacon.Task.Status == TaskStatus.RanToCompletion,
							"Bad receiveBeacon status.");

						TcpClient connectingTcpClient = new TcpClient();
						connectingTcpClient.BeginConnect(receiveBeacon.Task.Result,
							listenPort, OnConnect, connectingTcpClient);

						receiveBeacon = new TaskCompletionSource<IPAddress>();
					}
					beaconWaitTime.Stop();
					token.ThrowIfCancellationRequested();

					int timeLeft =
						beaconIntervalMs - (int)beaconWaitTime.ElapsedMilliseconds;
					if (timeLeft < 0) {
						timeLeft = 0;
					}
					Task.Delay(timeLeft).Wait();
				}

				token.ThrowIfCancellationRequested();
				broadcastClient.StopListeningBroadcast();

			} catch (OperationCanceledException) {
				throw;
			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
				throw;
			}
		}

		void OnConnect(IAsyncResult ar) {
			try {
				if (disposed) {
					return;
				}

				TcpClient connectingTcpClient = ar.AsyncState as TcpClient;
				Assert.IsNotNull(connectingTcpClient);

				lock (connectingLock) {
					if (Connected) {
						connectingTcpClient.Close();
					}

					try {
						connectingTcpClient.EndConnect(ar);
					} catch (ObjectDisposedException) {
						return;
					} catch (NullReferenceException) {
						// There is a bug in TcpClient, NullReferenceException is
						// thrown instead ObjectDisposedException
						return;
					} catch (SocketException) {
						return;
					}

					tcpClient = connectingTcpClient;
					tcpClient.ReceiveTimeout = disconnectTimeoutMs;
					stream = new NetworkStreamWrapper(tcpClient.GetStream());
					connected = 1;
					pingTimer.Restart();
				}

			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}
		}

		void OnTcpAccept(IAsyncResult ar) {
			try {
				if (disposed) {
					return;
				}

				TcpListener listener = ar.AsyncState as TcpListener;
				Assert.IsNotNull(listener);

				lock (connectingLock) {
					if (Connected) {
						return;
					}

					TcpClient curTcpClient;
					try {
						curTcpClient = listener.EndAcceptTcpClient(ar);
					} catch (ObjectDisposedException) {
						return;
					}

					listener.Stop();
					tcpClient = curTcpClient;
					tcpClient.ReceiveTimeout = disconnectTimeoutMs;
					stream = new NetworkStreamWrapper(tcpClient.GetStream());
					connected = 1;
				}
				
			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}
		}

		void OnBroadcastReceived(Message message) {
			if (disposed) {
				return;
			}

			if (message.data != beaconMessage) {
				return;
			}

			receiveBeacon.TrySetResult(message.source.Address);
		}

		void OnDestroy() {
			Dispose();
		}

		static bool StartsWith(byte[] array, byte[] start) {
			if (array.Length < start.Length) {
				return false;
			}

			for (int i = 0; i < start.Length; ++i) {
				if (array[i] != start[i]) {
					return false;
				}
			}

			return true;
		}
	}
}
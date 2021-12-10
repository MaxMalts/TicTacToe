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
				return connected;
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

		UdpBroadcastClient groupClient;
		TcpListener listener;
		TcpClient tcpClient;
		NetworkStreamWrapper stream;
		volatile bool connected = false;
		TaskCompletionSource<IPAddress> receiveBeacon;
		object connectingLock;
		Task connectTask;
		bool disconnectedEventPending = false;
		Stopwatch pingTimer = new Stopwatch();
		//Stopwatch disconnectTimer = new Stopwatch();

		ConcurrentQueue<byte[]> receivedPackages;
		Task readingTask;
		CancellationTokenSource readingTaskCT;

		bool disposed = false;


		/// <summary>
		/// Searches for other PeerToPeerClient in
		/// local network and connects to it.
		/// </summary>
		public Task ConnectToOtherClient() {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}

			if (connectTask == null ||
				connectTask.Status != TaskStatus.Running) {

				receiveBeacon = new TaskCompletionSource<IPAddress>();

				groupClient.MessageReceived.AddListener(OnBroadcastReceived);
				try {
					groupClient.StartListeningBroadcast();
				} catch (NoNetworkException exception) {
					throw new NoNetworkException("No network for sending and receiving beacons.", exception);
				}

				connectTask = Task.Run(SearchAndConnect);
			}

			return connectTask;
		}

		public void Send(byte[] data, int? offset = null, int? size = null) {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}
			if (!connected) {
				throw new NotConnectedException("Called Send but not connected to other client.");
			}

			try {
				stream.Write(data, offset, size);

			} catch (IOException exception) when (
				exception.InnerException.GetType() == typeof(SocketException)
			) {
				connected = false;
				disconnectedEventPending = true;
				throw new NotConnectedException("Socket error, PeerToPeerClient was disconnected.",
						exception);
			}

#if NETWORK_LOG
			UnityEngine.Debug.Log("PeerToPeerClient sent package.");
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
			if (!connected) {
				throw new NotConnectedException("Called StartReceiving but not connected to other client.");
			}

			if (readingTask != null && readingTask.Status != TaskStatus.Canceled) {
				UnityEngine.Debug.LogWarning("Called StartReceiving twice.");
				return;
			}

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


				while (!disposed) {
					token.ThrowIfCancellationRequested();

					byte[] data;
					try {
						data = stream.Read();

					} catch (IOException exception) when (
						exception.InnerException.GetType() == typeof(SocketException)
					) {
						connected = false;
						disconnectedEventPending = true;
						readingTaskCT.Cancel();
						continue;

					}

#if NETWORK_LOG
					UnityEngine.Debug.Log("PeerToPeerClient received package.");
#endif

					if (!StartsWith(data, pingMessage)) {
						receivedPackages.Enqueue(data);
					}
				}

			}, token);
		}

		public void StopReceiving() {
			if (disposed) {
				return;
			}

			if (readingTask == null || readingTask.Status == TaskStatus.Canceled) {
				UnityEngine.Debug.LogWarning("Called StopReceiving before StartReceiving.");
				return;
			}

			readingTaskCT.Cancel();
		}

		public void Close() {
			disposed = true;
			connected = false;
			readingTaskCT.Cancel();
			listener.Stop();
			pingTimer.Reset();
			tcpClient?.Close();

#if NETWORK_LOG
			UnityEngine.Debug.Log("PeerToPeerClient closed.");
#endif
		}

		void Awake() {
			groupClient = UdpBroadcastClient.Instance;
			listener = new TcpListener(IPAddress.Any, listenPort);
			receivedPackages = new ConcurrentQueue<byte[]>();
			connectingLock = new object();
			readingTaskCT = new CancellationTokenSource();
		}

		void Update() {
			if (!disposed) {
				byte[] package;
				while (receivedPackages.TryDequeue(out package)) {
					PackageReceived.Invoke(package);
				}

				if (disconnectedEventPending) {
					if (!connected) {
						pingTimer.Reset();
						Disconnected.Invoke();
					}

					disconnectedEventPending = false;
				}

				if (connected) {
					if (!pingTimer.IsRunning) {
						pingTimer.Restart();
					}

					if (pingTimer.ElapsedMilliseconds >= pingIntervalMs) {
						try {
							Send(pingMessage);  // will handle disconnection by itself
						} catch (SocketException) {
							pingTimer.Reset();
						}

						pingTimer.Restart();
					}
				}
			}
		}

		void SearchAndConnect() {
			try {
				if (disposed) {
					return;
				}

				listener.Start();
				listener.BeginAcceptTcpClient(OnTcpAccept, listener);

				while (!connected && !disposed) {
					try {
						groupClient.Send(beaconMessage);
					} catch (ObjectDisposedException) {
						return;
					}

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
					int timeLeft =
						beaconIntervalMs - (int)beaconWaitTime.ElapsedMilliseconds;
					if (timeLeft < 0)
						timeLeft = 0;
					Task.Delay(timeLeft).Wait();
				}

				groupClient.StopListeningBroadcast();

			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
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
					if (connected) {
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
					}

					tcpClient = connectingTcpClient;
					tcpClient.ReceiveTimeout = disconnectTimeoutMs;
					stream = new NetworkStreamWrapper(tcpClient.GetStream());
					connected = true;
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
					if (connected) {
						listener.Stop();
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
					connected = true;
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
			Close();
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
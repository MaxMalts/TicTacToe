using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



namespace Network {

	public class PeerToPeerClient : MonoBehaviour {

		public class PackageReceiveEvent : UnityEvent<byte[]> { }
		public PackageReceiveEvent PackageReceived { get; } = new PackageReceiveEvent();

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

		UdpBroadcastClient groupClient;
		TcpListener listener;
		TcpClient tcpClient;
		NetworkStreamWrapper stream;
		volatile bool connected = false;
		TaskCompletionSource<IPAddress> receiveBeacon;
		object connectingLock;
		Task connectTask;

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
				while (!disposed) {
					token.ThrowIfCancellationRequested();

					byte[] data;
					try {
						data = stream.Read();

					} catch (IOException exception) when (
						exception.InnerException.GetType() == typeof(SocketException)
					) {
						connected = false;
						readingTaskCT.Cancel();
						break;
					}

					receivedPackages.Enqueue(data);

#if NETWORK_LOG
					UnityEngine.Debug.Log("PeerToPeerClient received package.");
#endif
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
			tcpClient?.Close();
			listener.Stop();

#if NETWORK_LOG
			UnityEngine.Debug.Log("PeerToPeerClient closed connection.");
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
					stream = new NetworkStreamWrapper(tcpClient.GetStream());
					connected = true;
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
	}
}
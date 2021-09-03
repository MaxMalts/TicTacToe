using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



namespace Network {

	public class PeerToPeerClient : MonoBehaviour {

		public class PackageReceiveEvent : UnityEvent<byte[]> { }
		public PackageReceiveEvent packageReceived;

		const string beaconMessage = "PeerToPeerClient-beacon";
		const int listenPort = 875;
		const int beaconIntervalMs = 1000;

		UdpGroupClient groupClient;
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

				groupClient.groupMessageReceived.AddListener(OnGroupMessageReceived);
				groupClient.StartListeningBroadcast();

				connectTask = Task.Run(SearchAndConnect);
			}

			return connectTask;
		}

		public void Send(byte[] data, int? offset = null, int? size = null) {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}

			if (!connected) {
				UnityEngine.Debug.LogWarning("Called Send but not connected to other client.");
				return;
			}
			
			stream.Write(data, offset, size);
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

		public void StartReceiving() {
			if (disposed) {
				throw new ObjectDisposedException("Network.PeerToPeerClient");
			}

			if (!connected) {
				UnityEngine.Debug.LogWarning("Called StartReceiving but not connected to other client.");
				return;
			}

			if (readingTask != null) {
				UnityEngine.Debug.LogWarning("Called StartReceiving twice.");
				return;
			}

			CancellationToken token = readingTaskCT.Token;
			readingTask = Task.Run(() => {
				while (!disposed) {
					token.ThrowIfCancellationRequested();
					byte[] data = stream.Read();
					receivedPackages.Enqueue(data);
				}
			}, token);
		}

		public void StopReceiving() {
			if (disposed) {
				return;
			}

			if (readingTask == null) {
				UnityEngine.Debug.LogWarning("Called StopReceiving before StartReceiving.");
				return;
			}

			readingTaskCT.Cancel();
		}

		public void Close() {
			disposed = true;
			readingTaskCT.Cancel();
			tcpClient?.Close();
			listener.Stop();
		}

		void Awake() {
			groupClient = UdpGroupClient.Instance;
			listener = new TcpListener(IPAddress.Any, listenPort);
			packageReceived = new PackageReceiveEvent();
			receivedPackages = new ConcurrentQueue<byte[]>();
			connectingLock = new object();
			readingTaskCT = new CancellationTokenSource();
		}

		void Update() {
			if (!disposed) {
				byte[] package;
				while (receivedPackages.TryDequeue(out package)) {
					packageReceived.Invoke(package);
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
						groupClient.SendBroadcast(beaconMessage);
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

		void OnGroupMessageReceived(Message message) {
			if (disposed) {
				return;
			}

			if (message.data != beaconMessage) {
				return;
			}

			bool test = receiveBeacon.TrySetResult(message.source.Address);
		}

		void OnDestroy() {
			Close();
		}
	}
}
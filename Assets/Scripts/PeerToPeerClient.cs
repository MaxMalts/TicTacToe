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

		const string beaconMessage = "peer-to-peer-request";
		const int listenPort = 875;
		const int beaconIntervalMs = 1000;

		UdpGroupClient groupClient;
		TcpClient tcpClient;
		NetworkStreamWrapper stream;
		volatile bool connected = false;
		TaskCompletionSource<IPAddress> receiveBeacon;
		object connectingLock;
		Task connectTask;

		ConcurrentQueue<byte[]> receivedPackages;
		Task readingTask;
		CancellationTokenSource readingTaskCT;


		/// <summary>
		/// Searches for other PeerToPeerClient in
		/// local network and connects to it.
		/// </summary>
		public Task ConnectToOtherClient() {
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
			if (!connected) {
				UnityEngine.Debug.LogWarning("Called StartReceiving but not connected to other client.");
				return;
			}

			if (readingTask != null) {
				UnityEngine.Debug.LogWarning("Called StartReceiving twice.");
				return;
			}

			readingTaskCT = new CancellationTokenSource();
			CancellationToken token = readingTaskCT.Token;
			readingTask = Task.Run(() => {
				while (true) {
					token.ThrowIfCancellationRequested();
					byte[] data = stream.Read();
					receivedPackages.Enqueue(data);
				}
			}, token);
		}

		public void StopReceiving() {
			if (readingTask == null) {
				UnityEngine.Debug.LogWarning("Called StopReceiving before StartReceiving.");
				return;
			}

			readingTaskCT.Cancel();
		}


		void Awake() {
			groupClient = UdpGroupClient.Instance;
			packageReceived = new PackageReceiveEvent();
			receivedPackages = new ConcurrentQueue<byte[]>();
			connectingLock = new object();
		}

		void Update() {
			byte[] package;
			while (receivedPackages.TryDequeue(out package)) {
				packageReceived.Invoke(package);
			}
		}

		void SearchAndConnect() {
			try {
				TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
				listener.Start();
				listener.BeginAcceptTcpClient(OnTcpAccept, listener);

				while (!connected) {
					UnityEngine.Debug.Log("There");
					groupClient.SendBroadcast(beaconMessage);

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
					Task.Delay(beaconIntervalMs -
						(int)beaconWaitTime.ElapsedMilliseconds).Wait();
				}

				groupClient.StopListeningBroadcast();

			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}
		}

		void OnConnect(IAsyncResult ar) {
			try {
				TcpClient connectingTcpClient = ar.AsyncState as TcpClient;
				Assert.IsNotNull(connectingTcpClient);

				lock (connectingLock) {
					if (connected) {
						connectingTcpClient.Close();
					}

					try {
						UnityEngine.Debug.Log(connectingTcpClient == null);
						connectingTcpClient.EndConnect(ar);
					} catch (ObjectDisposedException) {
						return;
					} catch (NullReferenceException) {
						// There is a bug, NullReferenceException is
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

					tcpClient = curTcpClient;
					stream = new NetworkStreamWrapper(tcpClient.GetStream());
					connected = true;
				}
				
			} catch (Exception exception) {
				UnityEngine.Debug.LogException(exception);
			}
		}

		void OnGroupMessageReceived(Message message) {
			if (message.data != beaconMessage) {
				return;
			}

			bool test = receiveBeacon.TrySetResult(message.source.Address);
		}
	}
}
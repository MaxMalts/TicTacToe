using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;

#if (UNITY_ANDROID) && !UNITY_EDITOR
using UnityEngine.Android;
#endif



namespace Network {

	public class NoNetworkException : Exception {
		public NoNetworkException() { }

		public NoNetworkException(string message)
			: base(message) { }

		public NoNetworkException(string message, Exception inner)
			: base(message, inner) { }
	}


	public struct Message {
		public IPEndPoint source;
		public string data;
	}


	/// <summary>
	/// Used to send and receive UDP broadcast datagrams (datagrams sent to
	/// 255.255.255.255). Automatically handles network changes and tries to
	/// bind to any wifi network avaiable.
	/// </summary>
	public class UdpBroadcastClient : Singleton<UdpBroadcastClient> {

		public class MessageEvent : UnityEvent<Message> { }
		public MessageEvent MessageReceived { get; } = new MessageEvent();

		public class NoNetworkEvent : UnityEvent { }
		public NoNetworkEvent NoNetwork { get; } = new NoNetworkEvent();

		public static bool NetworkAvailable {
			get {
				return UdpBroadcastClient.GetWifiIP() != null;
			}
		}

		const string broadcastAddr = "255.255.255.255";
		const int sendingPort = 48888;
		const int receivingPort = 48889;
		IPEndPoint receivingEP;  // to not always parse

		const byte firstByteOfWifiIp = 192;  // for searching wifi ip


		/// <summary>
		/// This prefix is added to the beginning of each datagram.
		/// Only messages with this prefix are handeled.
		/// </summary>
		const string datagramPrefix = "UdpBroadcastClient ";
		int prefixByteSize;

		UdpClient sendClient;
		UdpClient receiveClient;
		ConcurrentQueue<Message> groupMessages;

		volatile bool noNetwork = true;
		volatile bool noNetworkEventPending = false;
		bool listening = false;
		bool disposed = false;


		/// <summary>
		/// Starts receiving broadcast messages and invoking MessageReceived event.
		/// When there the network goes down, you need to call this method again to
		/// start receicing messages.
		/// </summary>
		public void StartListeningBroadcast() {
			if (disposed) {
				throw new ObjectDisposedException("Network.UdpBroadcastClient");
			}
			if (noNetwork && !TryBindToNetwork()) {
				throw new NoNetworkException("No network found for udp group broadcasts.");
			}

			Assert.IsNotNull(sendClient);
			Assert.IsNotNull(receiveClient);

			if (listening) {
				return;
			}

			receiveClient.EnableBroadcast = true;
			listening = true;
			receiveClient.BeginReceive(OnReceive, null);
		}

		public void StopListeningBroadcast() {
			if (disposed) {
				return;
			}
			if (noNetwork) {
				return;
			}

			Assert.IsNotNull(sendClient);
			Assert.IsNotNull(receiveClient);

			receiveClient.EnableBroadcast = false;
			listening = false;
		}

		public void Send(string message) {
			if (disposed) {
				throw new ObjectDisposedException("Network.UdpBroadcastClient");
			}
			if (noNetwork && !TryBindToNetwork()) {
				throw new NoNetworkException("No network found for udp group broadcasts.");
			}

			byte[] data = Encoding.UTF8.GetBytes(datagramPrefix + message);

			try {
				sendClient.Send(data, data.Length, receivingEP);
			} catch (ObjectDisposedException) {
				throw;
			} catch (SocketException) {
				noNetwork = true;
				if (!TryBindToNetwork()) {
					noNetworkEventPending = true;
					throw new NoNetworkException("No network found for udp group broadcasts.");
				}
			} catch (Exception exception) {
				Debug.LogException(exception);
				return;
			}

#if NETWORK_LOG
			UnityEngine.Debug.Log("UdpBroadcastClient sent broadcast.");
#endif
		}

		public static IPAddress GetWifiIP() {
#if (UNITY_ANDROID) && !UNITY_EDITOR
			IPAddress wifiAddress = GetIpFromWifiManager();
			if (wifiAddress == null) {
				wifiAddress = SearchWifiThroughHostEntry();
				if (wifiAddress == null) {
					wifiAddress = SearchWifiThroughInterfaces();
				}
			}
			

			return wifiAddress;
#else
			IPAddress wifiAddress = SearchWifiThroughInterfaces();
			if (wifiAddress == null) {
				wifiAddress = SearchWifiThroughHostEntry();
			}

			return wifiAddress;
#endif
		}

		protected override void Awake() {
			base.Awake();

			groupMessages = new ConcurrentQueue<Message>();

			prefixByteSize = Encoding.UTF8.GetByteCount(datagramPrefix);
			receivingEP = new IPEndPoint(IPAddress.Parse(broadcastAddr), receivingPort);
		}

		void Start() {
			TryBindToNetwork();
		}

		void Update() {
			if (!disposed) {
				if (listening) {
					Message message;
					while (groupMessages.TryDequeue(out message)) {
						MessageReceived.Invoke(message);
					}

					if (noNetworkEventPending) {
						NoNetwork.Invoke();
					}
				}
			}
		}

		void OnReceive(IAsyncResult ar) {
			try {
				if (disposed) {
					return;
				}
				if (!listening) {
					return;
				}

				IPEndPoint endPoint = null;
				byte[] data;
				try {
					data = receiveClient.EndReceive(ar, ref endPoint);
				} catch (ObjectDisposedException) {
					return;
				} catch (SocketException) {
					noNetwork = true;
					noNetworkEventPending = true;
					return;
				} catch (Exception exception) {
					Debug.LogException(exception);
					receiveClient.BeginReceive(OnReceive, null);
					return;
				}

				if (endPoint.Address.Equals(((IPEndPoint)receiveClient.Client.LocalEndPoint).Address)) {
					receiveClient.BeginReceive(OnReceive, null);
					return;
				}

				bool invalidPrefix = false;
				string prefix = null;
				try {
					prefix = Encoding.UTF8.GetString(data, 0, prefixByteSize);
				} catch (ArgumentException exception) when (
					exception.GetType() == typeof(ArgumentException)
					|| exception.GetType() == typeof(ArgumentOutOfRangeException)
				) {
					invalidPrefix = true;
				}
				if (invalidPrefix || prefix != datagramPrefix) {
					Debug.LogWarning("Datagram with foreign preffix received.");
					return;
				}

				Message message;
				message.source = endPoint;
				message.data = Encoding.UTF8.GetString(data, prefixByteSize,
					data.Length - prefixByteSize);

				groupMessages.Enqueue(message);

#if NETWORK_LOG
				UnityEngine.Debug.Log("UdpBroadcastClient received broadcast.");
#endif

				receiveClient.BeginReceive(OnReceive, null);

			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		bool TryBindToNetwork() {
			Assert.IsTrue(noNetwork, "Why do you try to bind if it's already bound?");
			Assert.IsFalse(disposed, "UdpBroadcastClient disposed.");

			IPAddress localIp = GetWifiIP();
			if (localIp == null) {
				return false;
			}

			sendClient?.Close();
			receiveClient?.Close();

			sendClient = new UdpClient(new IPEndPoint(localIp, sendingPort));
			receiveClient = new UdpClient(new IPEndPoint(localIp, receivingPort));
			sendClient.EnableBroadcast = false;
			noNetwork = false;

			return true;
		}

#if (UNITY_ANDROID) && !UNITY_EDITOR
		static IPAddress GetIpFromWifiManager() {
			string wifiIpStr;
			try {
				var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				var wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
				var wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
				int wifiIpInt = wifiInfo.Call<int>("getIpAddress");
				if (wifiIpInt == 0) {
					return null;
				}

				var formatter = new AndroidJavaClass("android.text.format.Formatter");
				wifiIpStr = formatter.CallStatic<string>("formatIpAddress", wifiIpInt);
			} catch (Exception exception) {
				Debug.LogError("Unable to get wifi ip from Java: " + exception.Message);
				return null;
			}

			try {
				return IPAddress.Parse(wifiIpStr);
			} catch (FormatException exception) {
				Debug.LogError("Got bad wifi ip address from java: " + exception.Message);
			}

			return null;
		}
#endif

		static IPAddress SearchWifiThroughInterfaces() {
#if (UNITY_ANDROID) && !UNITY_EDITOR
			try {
				var networkInterfaceC = new AndroidJavaClass("java.net.NetworkInterface");
				var networkInterfaces =
					networkInterfaceC.CallStatic<AndroidJavaObject>("getNetworkInterfaces");
				while (networkInterfaces.Call<bool>("hasMoreElements")) {
					var curNetworkInterface =
						networkInterfaces.Call<AndroidJavaObject>("nextElement");
					if (curNetworkInterface.Call<bool>("isLoopback")) {
						continue;
					}

					var inetAddresses =
						curNetworkInterface.Call<AndroidJavaObject>("getInetAddresses");
					while (inetAddresses.Call<bool>("hasMoreElements")) {
						var curInetAddress =
							inetAddresses.Call<AndroidJavaObject>("nextElement");
						Assert.IsFalse(curInetAddress.Call<bool>("isLoopbackAddress"),
							"Detected loopback address on non-loopback interface.");

						sbyte[] ipAddressS = curInetAddress.Call<sbyte[]>("getAddress");
						Assert.IsNotNull(ipAddressS);
						if (ipAddressS.Length == 16) {
							continue;
						}
						Assert.IsTrue(ipAddressS.Length == 4);

						byte[] ipAddressU = new byte[ipAddressS.Length];
						Buffer.BlockCopy(ipAddressS, 0, ipAddressU, 0, ipAddressS.Length);
						if (ipAddressU[0] == firstByteOfWifiIp) {
							try {
								return new IPAddress(ipAddressU);
							} catch (Exception exception) {
								Debug.LogException(exception);
							}
						}

					}
				}
			} catch (Exception exception) {
				Debug.LogError("Unable to get wifi ip from Java: " + exception.Message);
				return null;
			}

			return null;

#else
			IPAddress ipv6Candidate = null;
			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces()) {
				if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
					item.OperationalStatus == OperationalStatus.Up) {

					foreach (UnicastIPAddressInformation ip in
						item.GetIPProperties().UnicastAddresses) {

						if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6) {
							ipv6Candidate = ip.Address;
						} else if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
							return ip.Address;
						}
					}
				}
			}

			return ipv6Candidate;
#endif
		}

		static IPAddress SearchWifiThroughHostEntry() {
			IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork &&
					ip.GetAddressBytes()[0] == firstByteOfWifiIp) {

					return ip;
				}
			}

			return null;
		}

		void Close() {
			disposed = true;
			if (noNetwork) {
				return;
			}

			sendClient.Close();
			receiveClient.Close();

#if NETWORK_LOG
			UnityEngine.Debug.Log("UdpBroadcastClient closed.");
#endif
		}

		void OnDestroy() {
			Close();
		}
	}
}
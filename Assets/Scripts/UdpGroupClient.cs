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



namespace Network {

	public struct Message {
		public IPEndPoint source;
		public string data;
	}


	public class UdpGroupClient : Singleton<UdpGroupClient> {

		public class MessageEvent : UnityEvent<Message> { }
		public MessageEvent groupMessageReceived;

		const string groupAddr = "224.0.23.187";
		const int groupPort = 875;
		IPEndPoint groupEP;  // to not always parse groupAddr

		/// <summary>
		/// This prefix is added to the beginning of each datagram.
		/// Only messages with this prefix are handeled.
		/// </summary>
		const string datagramPrefix = "UdpGroupClient ";
		int prefixByteSize;

		UdpClient groupClient;
		ConcurrentQueue<Message> groupMessages;

		bool disposed = false;


		public void StartListeningBroadcast() {
			if (disposed) {
				throw new ObjectDisposedException("Network.UdpGroupClient");
			}

			Assert.IsNotNull(groupClient);

			groupClient.JoinMulticastGroup(IPAddress.Parse(groupAddr));
			groupClient.BeginReceive(OnGroupReceive, null);
		}

		public void StopListeningBroadcast() {
			if (disposed) {
				return;
			}

			Assert.IsNotNull(groupClient);
			groupClient.DropMulticastGroup(IPAddress.Parse(groupAddr));
		}

		public void SendBroadcast(string message) {
			if (disposed) {
				throw new ObjectDisposedException("Network.UdpGroupClient");
			}

			byte[] data = Encoding.UTF8.GetBytes(datagramPrefix + message);

			try {
				groupClient.Send(data, data.Length, groupEP);
			} catch (ObjectDisposedException) {
				throw;
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		public void Close() {
			disposed = true;
			groupClient.Close();
		}

		public static IPAddress GetLocalIP() {
			foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces()) {
				if (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
					item.OperationalStatus == OperationalStatus.Up) {

					foreach (UnicastIPAddressInformation ip in
						item.GetIPProperties().UnicastAddresses) {

						if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
							return ip.Address;
						}
					}
				}
			}

			return null;
		}

		protected override void Awake() {
			base.Awake();
			Assert.IsNull(groupClient);

			groupClient = new UdpClient(new IPEndPoint(GetLocalIP(), groupPort));
			groupMessageReceived = new MessageEvent();
			groupMessages = new ConcurrentQueue<Message>();

			groupEP = new IPEndPoint(IPAddress.Parse(groupAddr), groupPort);
			prefixByteSize = Encoding.UTF8.GetByteCount(datagramPrefix);
		}

		void Update() {
			if (!disposed) {
				Message message;
				while (groupMessages.TryDequeue(out message)) {
					groupMessageReceived.Invoke(message);
				}
			}
		}

		void OnGroupReceive(IAsyncResult ar) {
			try {
				if (disposed) {
					return;
				}

				IPEndPoint endPoint = null;
				byte[] data;
				try {
					data = groupClient.EndReceive(ar, ref endPoint);
				} catch (ObjectDisposedException) {
					return;
				} catch (Exception exception) {
					Debug.LogException(exception);
					groupClient.BeginReceive(OnGroupReceive, null);
					return;
				}

				if (((IPEndPoint)groupClient.Client.LocalEndPoint).Address ==
					endPoint.Address) {
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

			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		void OnDestroy() {
			Close();
		}
	}
}
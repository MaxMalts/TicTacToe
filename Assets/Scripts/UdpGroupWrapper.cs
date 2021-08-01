using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



namespace Network {

	public struct Message {
		public IPEndPoint source;
		public string data;
	}


	public class UdpGroupWrapper : Singleton<UdpGroupWrapper> {

		public class MessageEvent : UnityEvent<Message> {}
		public MessageEvent groupMessageReceived;

		const string groupAddr = "224.0.23.187";
		const int groupPort = 875;
		IPEndPoint groupEP;  // to not always parse groupAddr

		/// <summary>
		/// This prefix is added to the beginning of each datagram.
		/// Only messages with this prefix are handeled.
		/// </summary>
		const string datagramPrefix = "TicTacToe";
		int prefixByteSize;

		UdpClient groupClient;
		ConcurrentQueue<Message> groupMessages;

		public void StartListeningBroadcast() {
			Assert.IsNotNull(groupClient);

			groupClient.JoinMulticastGroup(IPAddress.Parse(groupAddr));
			groupClient.BeginReceive(OnGroupReceive, null);
		}

		void OnGroupReceive(IAsyncResult ar) {
			try {
				IPEndPoint endPoint = null;
				byte[] data;
				try {
					data = groupClient.EndReceive(ar, ref endPoint);
				} catch (Exception exception) {
					Debug.LogException(exception);
					return;
				} finally {
					groupClient.BeginReceive(OnGroupReceive, null);
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

		public void StopListeningBroadcast() {
			Assert.IsNotNull(groupClient);
			groupClient.DropMulticastGroup(IPAddress.Parse(groupAddr));
		}

		public void SendBroadcast(string message) {
			byte[] data = Encoding.UTF8.GetBytes(datagramPrefix + message);

			try {
				groupClient.Send(data, data.Length, groupEP);
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}

		protected override void Awake() {
			base.Awake();
			Assert.IsNull(groupClient);

			groupMessageReceived = new MessageEvent();
			groupClient = new UdpClient(groupPort);
			groupMessages = new ConcurrentQueue<Message>();

			groupEP = new IPEndPoint(IPAddress.Parse(groupAddr), groupPort);
			prefixByteSize = Encoding.UTF8.GetByteCount(datagramPrefix);

			// temp
			StartListeningBroadcast();
			//byte[] datagram = Encoding.UTF8.GetBytes("TicTacToTest");
			groupClient.Send(datagram, datagram.Length, new IPEndPoint(IPAddress.Parse(groupAddr), groupPort));
			//SendBroadcast("test");
			// temp
		}

		void Update() {
			Message message;
			while (groupMessages.TryDequeue(out message)) {
				groupMessageReceived.Invoke(message);
			}
		}
	}
}
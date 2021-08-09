using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;



namespace Network {
	
	/// <summary>
	/// Wrapper over the NetworkStream to remove framing.
	/// Each send is received by single receive.<br/>
	/// Useful for e.g. tcp/ip communication,
	/// you need to both send and receive packages
	/// with this wrapper for correct work.
	/// </summary>
	public class NetworkStreamWrapper {

		NetworkStream stream;
		object packageReadLock;

		readonly byte[] packagePrefix = Encoding.UTF8.GetBytes("NetworkStreamWrapper ");


		public NetworkStreamWrapper(NetworkStream stream) {
			this.stream = stream;
		}

		public void Write(byte[] data, int? offset = null, int? size = null) {
			if (offset == null) {
				offset = 0;
			}
			if (size == null) {
				size = data.Length - offset;
			}

			try {
				byte[] sizeb = BitConverter.GetBytes(size.Value);
				Debug.Assert(sizeb.Length == sizeof(int));

				byte[] headerBuf = new byte[packagePrefix.Length + sizeof(int)];
				try {
					packagePrefix.CopyTo(headerBuf, 0);
					sizeb.CopyTo(headerBuf, packagePrefix.Length);
				} catch (ArgumentException exception) {
					Debug.Assert(false, "headerBuf size mismatch.", exception.Message);
				}

				stream.Write(headerBuf, 0, headerBuf.Length);
				stream.Write(data, offset.Value, size.Value);

			} catch (InvalidOperationException exception) {
				Debug.Assert(false, "Problem with offset or size values", exception.Message);
			}
		}

		public byte[] Read() {
			lock (packageReadLock) {
				byte[] prefixBuf = new byte[packagePrefix.Length];
				ReadFixedSize(prefixBuf, 0, prefixBuf.Length);
				if (prefixBuf != packagePrefix) {
					throw new IOException("Invalid package prefix. " +
						"Probably you sent it not via NetworkStreamWrapper.");
				}

				byte[] sizeBuf = new byte[sizeof(int)];
				ReadFixedSize(sizeBuf, 0, sizeBuf.Length);
				int packageSize = BitConverter.ToInt32(sizeBuf, 0);
				Debug.Assert(packageSize >= 0);

				byte[] res = new byte[packageSize];
				ReadFixedSize(res, 0, packageSize);

				return res;
			}
		}

		public async Task<byte[]> ReadAsync() {
			return await Task<byte[]>.Run(() => {
				return Read();
			});
		}

		void ReadFixedSize(byte[] data, int offset, int size) {
			Debug.Assert(data != null);
			Debug.Assert(offset >= 0);
			Debug.Assert(size >= 0 && size < data.Length);

			int bytesRead = 0;
			do {
				bytesRead += stream.Read(data, bytesRead, size - bytesRead);
			} while (bytesRead < size);
			Debug.Assert(bytesRead == size);
		}
	}
}
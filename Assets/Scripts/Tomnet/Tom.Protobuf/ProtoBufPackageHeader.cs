using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;
using Tom;

namespace Tom
{
	public class ProtoBufPackageHeader
	{
		public int msgLen; // 消息长度

		public int msgType;
		public int typeNameLen;
		public string typeName;
		private readonly static int s_size;
		/// <summary>
		/// 包头的字节大小
		/// </summary>
		public static int SizeOf { get { return s_size; } }

		public ProtoBufPackageHeader()
		{
			msgLen = 0;
		}

		private int ReturnHeaderLen()
		{
			return 9;

		}

		public int Size()
		{
			int size = Marshal.SizeOf(msgLen);
			return size;
		}

		public void WriteTo(System.IO.Stream stream)
		{
			byte[] buf = new byte[SizeOf];
			WriteTo(buf, 0);
			stream.Write(buf, 0, buf.Length);
		}

		public void WriteTo(byte[] buf, int index)
		{
			Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(this.msgLen)), 0, buf, 0, 4);
			Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(this.msgType)), 0, buf, 4, 4);
		}

		public void ReadLen(byte[] buf, int offset)
		{
			this.msgLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
			//this.msgType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 4));
		}

		public void ReadHeader(byte[] buf, int offset)
		{
			this.msgType = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0)); // 4
			this.typeNameLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 4)); // 4
			this.typeName = Encoding.UTF8.GetString(buf, 8, this.typeNameLen); // 
			
			byte[] msg = new byte[this.msgLen - 4 - 4 - this.typeNameLen];
			Array.Copy(buf, 8 + this.typeNameLen, msg, 0, this.msgLen - 4 - 4 - this.typeNameLen);
			PacketParser parser = new PacketParser();

			//var typ = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == this.typeName);
			LoginOk res = parser.Deserialize<LoginOk>(msg);
			var res1 = parser.Deserialize(msg, "LoginOk");
			Console.WriteLine(this.typeName);
		}


		private static void CheckBuf(byte[] buf, int index)
		{
			if (buf == null)
				throw new ArgumentNullException("buf");
			if (index < 0 || index + SizeOf > buf.Length)
				throw new ArgumentOutOfRangeException("index");
		}
	}
}

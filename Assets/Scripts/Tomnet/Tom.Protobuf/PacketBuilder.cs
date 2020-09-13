

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Net;
namespace Tom
{
    public class PacketBuilder
    {

        public byte[] ProtobufPacket(Google.Protobuf.IMessage message)
        {
            int writeIndex = 0;

            byte[] bodyBytes = message.ToByteArray();
            byte[] messageNameBytes = Encoding.UTF8.GetBytes(message.Descriptor.FullName);
            Int32 payloadsize = 4 + 4 + messageNameBytes.Length + bodyBytes.Length;
            byte[] packet = new byte[payloadsize + 4];

            WriteInt32(ref packet, ref writeIndex, payloadsize);  // 数据包总长度
            WriteInt32(ref packet, ref writeIndex, 8);            // 数据类型
            WriteInt32(ref packet, ref writeIndex, messageNameBytes.Length);      // 消息名字长度
            WriteBytes(ref packet, ref writeIndex, messageNameBytes, messageNameBytes.Length);                      // 消息名字符串
            WriteBytes(ref packet, ref writeIndex, bodyBytes, bodyBytes.Length);         // 消息信息， protobuf
            return packet;
        }



        private void WriteUInt16(ref byte[] packet, ref int writeIndex, ushort num)
        {
            byte[] tmp = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num));
            Array.Copy(tmp, 0, packet, writeIndex, 2);
            writeIndex += 2;
        }

        private void WriteInt32(ref byte[] packet, ref int writeIndex, int num)
        {
            byte[] tmp = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num));
            Array.Copy(tmp, 0, packet, writeIndex, 4);
            writeIndex += 4;
        }

        private void WriteBytes(ref byte[] packet, ref int writeIndex, byte[] data, int len)
        {
            Array.Copy(data, 0, packet, writeIndex, len);
            writeIndex += len;
        }
    }
}

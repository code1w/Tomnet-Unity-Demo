/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 * 
 *  Google.Protobuf Encode(Decode)
 * 
 *  创建 2020-09-18
 *  修改 2020-09-21
 */



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Google.Protobuf;
using TomNet.Common;
using System.Data;

namespace TomNet.Protocol
{
    public class ProtobufMsgCodec : IMsgCodec
    {
       
        public ProtobufMsgCodec()
        {
        }


        private byte[] Serialize(Google.Protobuf.IMessage msg)
        {
            using (MemoryStream sndms = new MemoryStream())
            {
                msg.WriteTo(sndms);
                return sndms.ToArray();
            }
        }

        private T DeserializePbMsg<T>(ByteBuffer data) where T : IMessage, new()
        {
            T msg = new T();
            T copy = (T)msg.Descriptor.Parser.ParseFrom(data.Begin(), data.PrependableBytes(), data.Readable());
            return copy;
        }

        private IMessage DeserializePbMsg(string messageName, ByteBuffer data)
        {
            Type type = GetMsgType(messageName);
            IMessage imsg = (Google.Protobuf.IMessage)Activator.CreateInstance(type);
            IMessage msg = imsg.Descriptor.Parser.ParseFrom(data.Begin(), data.PrependableBytes(), data.Readable());
            return msg;
        }

        public override object OnGenerateMsg(IMsgHeader header, ByteBuffer data)
        {
            DefaultMsgHeader dheader = header as DefaultMsgHeader;
            var msg = DeserializePbMsg(dheader.MsgType, data);
            return msg;
        }

        public override ByteBuffer GenerateBinaryMessage(object message)
        {
            ByteBuffer buffer = new ByteBuffer();
            IMessage pbmsg = (IMessage)message;
            byte[] payload = pbmsg.ToByteArray();

            int len = sizeof(int) + sizeof(int) + pbmsg.Descriptor.FullName.Length + payload.Length;
            buffer.AppendInt32(len);
            buffer.AppendInt32(88);
            buffer.AppendInt32(pbmsg.Descriptor.FullName.Length);
            byte[] NameBytes = Encoding.UTF8.GetBytes(pbmsg.Descriptor.FullName);
            buffer.WriteBytes(NameBytes);
            buffer.WriteBytes(payload);
            return buffer;
        }
    }
}

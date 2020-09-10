
using System.Collections;
using System;
using System.Text;
using System.Net;
using Google.Protobuf;

using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Reflection;
using System.Reflection;
using System.Linq;

namespace Tom {
    public class PacketParser {

        public void Parse( byte[] data ) {
            int readIndex = 0;
            int payloadSize = ReadInt32( data, ref readIndex);
            int msgType = ReadInt32(data, ref readIndex);
            int nameLen = ReadInt32( data, ref readIndex );                         // 读取消息名长度
             string messageName = ReadString( data, ref readIndex, nameLen);        // 读取消息名字符串， -1 ，不读取字符串结尾处的'\0'
            int objDataLen = data.Length - readIndex - 4;
            byte[] objData = ReadBytes( data, ref readIndex, objDataLen);               // 读取 protobuf 对象数据
            int checkCode = ReadInt32( data, ref readIndex );                           // 读取校验值
        }

        public byte[] Serialize(Google.Protobuf.IMessage msg)
        {
            using (MemoryStream sndms = new MemoryStream())
            {
                msg.WriteTo(sndms);
                return sndms.ToArray();
            }
        }


        public T Deserialize<T>(byte[] data) where T : Google.Protobuf.IMessage, new()
        {
            T msg = new T();
            T copy = (T)msg.Descriptor.Parser.ParseFrom(data);
            return copy;
        }


        public Google.Protobuf.IMessage Deserialize(byte[] data, string messageName)
        {
            var typ = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == messageName);
            Google.Protobuf.IMessage msg = (Google.Protobuf.IMessage)Activator.CreateInstance(typ);
            var copy = msg.Descriptor.Parser.ParseFrom(data);
            return copy;
        }

        private byte ReadByte( byte[] data, ref int readIndex) {
            byte []array = new byte[1];
            Array.Copy( data, readIndex, array, 0, 1 );
            readIndex += 1;
            return array[0];
        }

        private byte[] ReadBytes( byte[] data, ref int readIndex, int len ) 
        {
            byte[]array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            readIndex += len;
            return array;
        }


        private Int16 ReadUInt16( byte[] data, int readIndex) 
        {
            byte[] array = new byte[2];
            Array.Copy( data, readIndex, array, 0, 2 );
            Int16 result = IPAddress.NetworkToHostOrder(BitConverter.ToInt16( array, 0 ));
            readIndex += 2;
            return result;
        }

        private int ReadInt32( byte[] data, ref int readIndex) 
        {
            byte[] array = new byte[4];
            Array.Copy( data, readIndex, array, 0, 4 );
            int result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32( array, 0 ));
            readIndex += 4;
            return result;
        }

        private string ReadString( byte[] data, ref int readIndex, int len ) 
        {
            byte[] array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            string result = Encoding.UTF8.GetString( array );
            readIndex += len;
            return result;
        }
    }
}

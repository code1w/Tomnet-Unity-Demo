
using System.Collections;
using System;
using System.Text;
using System.Net;
<<<<<<< HEAD
using Google.Protobuf;

=======
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c


using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Reflection;
using System.Reflection;
using System.Linq;

namespace Tom {
    public class PacketParser {

        public void Parse( byte[] data ) {
<<<<<<< HEAD
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
=======

            Int32 readIndex = 0;
            Int32 payloadSize = ReadInt32( data, ref readIndex);
            Int32 msgType = ReadInt32(data, ref readIndex);
            Int32 nameLen = ReadInt32( data, ref readIndex );                         // 读取消息名长度
             string messageName = ReadString( data, ref readIndex, nameLen);        // 读取消息名字符串， -1 ，不读取字符串结尾处的'\0'
            int objDataLen = data.Length - readIndex - 4;
            byte[] objData = ReadBytes( data, ref readIndex, objDataLen);               // 读取 protobuf 对象数据
            int checkCode = ReadInt32( data, ref readIndex );                           // 读取校验值
            //object obj = DataCenter.ProtobufUtility.Deserialize( objData, messageName);   // 反序列化 Protobuf 对象
            //DataCenter.packetProcesser.PacketDispatch( obj, messageName);                 // 将 protobuf 消息对象传给 PacketProcesser 进行处理和派发
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c

            /*
            var typs = Assembly.GetExecutingAssembly().GetTypes();// (t => t.FullName == messageName);
            var descriptor = (MessageDescriptor)typ.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            var msg = descriptor.Parser.ParseFrom(data);
            return msg;
            */
        }


<<<<<<< HEAD
        private byte ReadByte( byte[] data, ref int readIndex) {
=======
        private byte ReadByte( byte[] data, ref Int32 readIndex) {
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c
            byte []array = new byte[1];
            Array.Copy( data, readIndex, array, 0, 1 );
            readIndex += 1;
            return array[0];
        }


<<<<<<< HEAD
        private byte[] ReadBytes( byte[] data, ref int readIndex, int len ) {
=======
        private byte[] ReadBytes( byte[] data, ref Int32 readIndex, Int32 len ) {
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c
            byte[]array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            readIndex += len;
            return array;
        }


<<<<<<< HEAD
        private Int16 ReadUInt16( byte[] data, int readIndex) {
=======
        private Int16 ReadUInt16( byte[] data, Int32 readIndex) {
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c
            byte[] array = new byte[2];
            Array.Copy( data, readIndex, array, 0, 2 );
            Int16 result = IPAddress.NetworkToHostOrder(BitConverter.ToInt16( array, 0 ));
            readIndex += 2;
            return result;
        }

<<<<<<< HEAD
        private int ReadInt32( byte[] data, ref int readIndex) {
            byte[] array = new byte[4];
            Array.Copy( data, readIndex, array, 0, 4 );
            int result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32( array, 0 ));
=======
        private Int32 ReadInt32( byte[] data, ref Int32 readIndex) {
            byte[] array = new byte[4];
            Array.Copy( data, readIndex, array, 0, 4 );
            Int32 result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32( array, 0 ));
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c
            readIndex += 4;
            return result;
        }


<<<<<<< HEAD
        private string ReadString( byte[] data, ref int readIndex, int len ) {
=======
        private string ReadString( byte[] data, ref Int32 readIndex, Int32 len ) {
>>>>>>> 7848d5004388ba039247ab43f8121ebbfcd4871c
            byte[] array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            string result = Encoding.UTF8.GetString( array );
            readIndex += len;
            return result;
        }
    }
}

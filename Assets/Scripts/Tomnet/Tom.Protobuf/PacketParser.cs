
using System.Collections;
using System;
using System.Text;
using System.Net;

namespace GamePacketData {
    public class PacketParser {

        public void Parse( byte[] data ) {

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

        }


        private byte ReadByte( byte[] data, ref Int32 readIndex) {
            byte []array = new byte[1];
            Array.Copy( data, readIndex, array, 0, 1 );
            readIndex += 1;
            return array[0];
        }


        private byte[] ReadBytes( byte[] data, ref Int32 readIndex, Int32 len ) {
            byte[]array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            readIndex += len;
            return array;
        }


        private Int16 ReadUInt16( byte[] data, Int32 readIndex) {
            byte[] array = new byte[2];
            Array.Copy( data, readIndex, array, 0, 2 );
            Int16 result = IPAddress.NetworkToHostOrder(BitConverter.ToInt16( array, 0 ));
            readIndex += 2;
            return result;
        }

        private Int32 ReadInt32( byte[] data, ref Int32 readIndex) {
            byte[] array = new byte[4];
            Array.Copy( data, readIndex, array, 0, 4 );
            Int32 result = IPAddress.NetworkToHostOrder(BitConverter.ToInt32( array, 0 ));
            readIndex += 4;
            return result;
        }


        private string ReadString( byte[] data, ref Int32 readIndex, Int32 len ) {
            byte[] array = new byte[len];
            Array.Copy( data, readIndex, array, 0, len );
            string result = Encoding.UTF8.GetString( array );
            readIndex += len;
            return result;
        }
    }
}

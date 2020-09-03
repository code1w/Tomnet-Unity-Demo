/*
================================================================================
FileName    : ProtobufUtility
Description : Serialize and Deserialize Protobuf class
Date        : 2014-05-05
Author      : Linkrules
================================================================================
*/
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;

    public class ProtobufUtility {

        private Dictionary<string,System.Type> mProtobufType = new Dictionary<string, System.Type>();


        public ProtobufUtility() {
            InitProtobufTypes( this.GetType().Assembly );
        }


        /// <summary>
        /// 将 Protobuf 消息类打包成二进制数据流
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public byte[] Serialize( object data ) {
            byte[] buffer = null;
            using ( MemoryStream m = new MemoryStream() ) {
                Serializer.Serialize( m, data );
                m.Position = 0;
                int len = (int)m.Length;
                buffer = new byte[len];
                m.Read( buffer, 0, len );
            }
            return buffer;
        }


        /// <summary>
        /// 解析 Protobuf 二进制数据流，返回 object (需要根据不同的消息类型回调以注册的处理函数)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="messageName"></param>
        /// <returns></returns>
        public object Deserialize( byte[] data, string messageName ) {
            System.Type type = GetTypeByName( messageName );
            using ( MemoryStream m = new MemoryStream( data ) ) {
                return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize( m, null, type );
            }
        }


        /// <summary>
        /// 遍历所有的 protobuf 消息类，将类型及类名存入字典
        /// </summary>
        /// <param name="assembly"></param>
        private void InitProtobufTypes( System.Reflection.Assembly assembly ) {
            foreach ( System.Type t in assembly.GetTypes() ) {
                ProtoBuf.ProtoContractAttribute[] pc = (ProtoBuf.ProtoContractAttribute[])t.GetCustomAttributes( typeof( ProtoBuf.ProtoContractAttribute ), false );
                if ( pc.Length > 0 ) {
                    mProtobufType.Add( t.Name, t );
                }
            }
        }


        /// <summary>
        /// 通过 protobuf 消息名，获取消息类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public System.Type GetTypeByName( string name ) {
            return mProtobufType[name];
        }
    /*
     * 
using System;
using System.IO;
using System.Collections.Concurrent;
using System.Text;

namespace ProtoBufDemo
{
class Program
{
static void Main(string[] args)
{
    Demopackage.rep rep = new Demopackage.rep();
    rep.Repnum = 1;
    rep.Repmsg = "Hello";
    rep.Repvalue = Math.PI;
    //中间变量
    ConcurrentQueue cqb = new ConcurrentQueue();
    //对象序列化
    byte[] bytes = GetBytesFromProtoObject(rep);
    //对象反序列化
    var recv = GetProtobufObjectFromBytes<Demopackage.rep>(bytes);
    Console.WriteLine(string.Format("num:{0},\nmsg:{1},\nvalue:{2}",recv.Repnum,recv.Repmsg,recv.Repvalue));
    Console.ReadKey();
}
///

/// 对象序列化
///

/// 需要序列化的对象
/// 序列化后的buffer
private static byte[] GetBytesFromProtoObject(Google.Protobuf.IMessage msg)
{
using (MemoryStream sndms = new MemoryStream())
{
Google.Protobuf.CodedOutputStream cos = new Google.Protobuf.CodedOutputStream(sndms);
cos.WriteMessage(msg);
cos.Flush();
return sndms.ToArray();
}
}
///

/// 对象反序列化
///

/// 序列化的对象类型
/// 序列化的对象buffer
/// 对象
public static T GetProtobufObjectFromBytes(byte[] bytes) where T : Google.Protobuf.IMessage, new()
{
Google.Protobuf.CodedInputStream cis = new Google.Protobuf.CodedInputStream(bytes);
T msg = new T();
cis.ReadMessage(msg);
return msg;
}
}
}
     */


}

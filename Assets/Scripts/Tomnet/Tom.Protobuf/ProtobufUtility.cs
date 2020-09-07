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
using Google.Protobuf.Reflection;
using System.Reflection;
using System.Linq;

namespace Tom
{
    public class ProtobufUtility
    {
        //private Dictionary<string,System.Type> mProtobufType = new Dictionary<string, System.Type>();
        private Dictionary<string, MessageDescriptor> mProtobufType = new Dictionary<string, MessageDescriptor>();

        public ProtobufUtility()
        {
            //InitProtobufTypes( this.GetType().Assembly );
        }

        /*
            public byte[] Serialize( object data ) {
                byte[] buffer = null;
                using ( MemoryStream m = new MemoryStream() ) {
                    //Serializer.Serialize( m, data );
                    m.Position = 0;
                    int len = (int)m.Length;
                    buffer = new byte[len];
                    m.Read( buffer, 0, len );
                }
                return buffer;
            }
        */
        public byte[] Serialize(Google.Protobuf.IMessage msg)
        {
            using (MemoryStream sndms = new MemoryStream())
            {
                Google.Protobuf.CodedOutputStream cos = new Google.Protobuf.CodedOutputStream(sndms);
                cos.WriteMessage(msg);
                cos.Flush();
                //return sndms.ToArray();
                return msg.ToByteArray();

            }
        }
        public Google.Protobuf.IMessage Deserialize(byte[] data, string messageName)
        {
            var typ = Assembly.GetExecutingAssembly().GetTypes().First(t => t.Name == messageName);
            var descriptor = (MessageDescriptor)typ.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(data, 0, data.Length);
                ms.Seek(0, SeekOrigin.Begin);
                var msg = descriptor.Parser.ParseFrom(ms); // parse the byte array to Person
                return msg;
            }
        }

        public string MessageName(Google.Protobuf.IMessage msg)
        {
            var descriptor = msg.Descriptor;
            return descriptor.FullName;
        }

        private void InitProtobufTypes(System.Reflection.Assembly assembly)
        {
            foreach (System.Type t in assembly.GetTypes())
            {
                var descriptor = (MessageDescriptor)t.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                if (descriptor != null)
                {
                    mProtobufType.Add(t.Name, descriptor);
                }
            }
        }

        public MessageDescriptor GetTypeByName(string name)
        {
            return mProtobufType[name];
        }

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

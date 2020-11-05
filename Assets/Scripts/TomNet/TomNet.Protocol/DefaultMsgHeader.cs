/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-15
 *  修改 2020-09-18
 */

// 系统默认的消息头处理
// 消息头协议 len[4字节] + opt[4字节] + payload
// len值不包含len本身占用的4字节 len = 4[opt] + payload长度

using System;
using System.Text;
using TomNet.Common;
using TomNet.Core;
using System.IO;
namespace TomNet.Protocol
{
    public class DefaultMsgHeader : IMsgHeader
    {
        private int pketlen = -1;
        private int pketopt = -1;

        private int msgtypelen = -1;
        private string msgtype = "";

        private bool binary = true;
        private bool compressed = false;
        private bool encrypted = false;
        private bool bigsized = false;

        public int PacketLen
        {
            get
            {
                return pketlen;
            }
            set
            {
                pketlen = value;
            }
        }

        public int PacketOpt
        {
            get
            {
                return pketopt;
            }
            set
            {
                pketopt = value;
            }
        }

        public string MsgType
        {
            get
            {
                return msgtype;
            }
            set
            {
                msgtype = value;
            }
        }

        public bool Encrypted
        {
            get
            {
                return encrypted;
            }
            set
            {
                encrypted = value;
            }
        }

        public bool Compressed
        {
            get
            {
                return compressed;
            }
            set
            {
                compressed = value;
            }
        }


        public bool Binary
        {
            get
            {
                return binary;
            }
            set
            {
                binary = value;
            }
        }

        public bool BigSized
        {
            get
            {
                return bigsized;
            }
            set
            {
                bigsized = value;
            }
        }

        public DefaultMsgHeader(bool encrypted = false, bool compressed = false)
        {
            this.compressed = compressed;
            this.encrypted = encrypted;
            this.binary = true;
            this.pketlen = 0;
            this.pketopt = 0;
        }

        public DefaultMsgHeader()
        {
            this.compressed = false;
            this.binary = true;
            this.encrypted = false;
            this.pketlen = 0;
            this.pketopt = 0;
        }

        /// <summary>
        /// 序列化到buffer
        /// </summary>
        /// <param name="buffer"></param>
        public void ToBinary(ByteBuffer buffer)
        {
            buffer.AppendInt32(pketlen);
            buffer.AppendInt32(pketopt);
        }


        private string ReadMsgTypeFromByteBuffer(ByteBuffer buf, int len)
        {
            byte[] array = buf.ReadBytes(len);
            string result = Encoding.UTF8.GetString(array);
            return result;
        }

        public void FromBinary(ByteBuffer buffer)
        {
            this.pketlen = buffer.ReadInt32();  // 读长度
            this.pketopt = buffer.ReadInt32();     // 读类型
            this.msgtypelen = buffer.ReadInt32();     // 读类型
            this.msgtype = ReadMsgTypeFromByteBuffer(buffer, this.msgtypelen);

            if ((this.pketopt & 0x40) > 0)
            {
                encrypted = true;
            }

            if ((this.pketopt & 0x20) > 0)
            {
                compressed = true;
            }

            if ((this.pketopt & 0x10) > 0)
            {
                bigsized = true;
            }
        }



        public byte Encode()
        {
            byte b = 0;
            if (binary)
            {
                b = (byte)(b | 0x80);
            }
            if (Encrypted)
            {
                b = (byte)(b | 0x40);
            }
            if (Compressed)
            {
                b = (byte)(b | 0x20);
            }
            if (bigsized)
            {
                b = (byte)(b | 8);
            }
            return b;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("---------------------------------------------\n");
            stringBuilder.Append("Binary:  \t" + binary + "\n");
            stringBuilder.Append("Compressed:\t" + compressed + "\n");
            stringBuilder.Append("Encrypted:\t" + encrypted + "\n");
            stringBuilder.Append("BigSized:\t" + bigsized + "\n");
            stringBuilder.Append("---------------------------------------------\n");
            return stringBuilder.ToString();
        }
    }
}

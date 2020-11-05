/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 * Protobuf protocol 
 * 
 * 创建 2020/09/18
 * 修改 2020/09/18
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Common;
using Google.Protobuf;

namespace TomNet.Protocol
{
    public class ProtobufProtocol : IProtocol
    {
        private IMsgCodec codec = null;
        private IMsgHeader header = null;
        private string payloadtype = "";


        public string PayLoadType
        {
            get
            {
                return payloadtype;
            }
            set
            {
                payloadtype = value;
            }
        }

        public IMsgCodec Codec
        {
            get
            {
                return codec;
            }
            set
            {
                codec = value;
            }
        }

        public IMsgHeader Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
            }
        }

        public ProtobufProtocol()
        {
            this.PayLoadType = ProtocolDefine.GoogleProtobuf;
            this.Codec = new ProtobufMsgCodec();

        }

        public IMsgHeader GenerateHeader(string type)
        {
            IMsgHeader header = new DefaultMsgHeader();
            this.Header = header;
            return header;
        }

        public IMsgHeader GenerateHeader(bool encrypted, bool compressed)
        {
            IMsgHeader header = new DefaultMsgHeader(encrypted, compressed);
            this.Header = header;
            return header;
        }




    }
}

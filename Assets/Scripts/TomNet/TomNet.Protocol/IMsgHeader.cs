/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-10
 *  修改 2020-09-18
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Common;

namespace TomNet.Protocol
{
    public interface IMsgHeader
    {
        int PacketLen
        {
            get;
            set;
        }

        // 高16位 按位存储： 加密 压缩 ...
        // 低16位 消息类型： pb fb json ...
        int PacketOpt
        {
            get;
            set;
        }

        bool Encrypted
        {
            get;
            set;
        }

        bool Compressed
        {
            get;
            set;
        }


        bool Binary
        {
            get;
            set;
        }

        bool BigSized
        {
            get;
            set;
        }

        /// <summary>
        /// packetlen packetopt 写到buffer中
        /// </summary>
        /// <param name="buffer"></param>
        void ToBinary(ByteBuffer buffer);

        /// <summary>
        /// 从buffer中读出 packetlen packetopt
        /// </summary>
        /// <param name="buffer"></param>
        void FromBinary(ByteBuffer buffer);
    }
}

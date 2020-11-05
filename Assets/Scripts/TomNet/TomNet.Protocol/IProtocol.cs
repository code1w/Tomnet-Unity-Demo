/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/18
 */

// 协议组合接口

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Common;

namespace TomNet.Protocol
{
    public interface IProtocol
    {
        string PayLoadType
        {
            get;
        }

        IMsgCodec Codec
        {
            get;
        }

        IMsgHeader Header
        {
            get;
        }

        IMsgHeader GenerateHeader(string type);
        IMsgHeader GenerateHeader(bool encrypted, bool compressed );
    }
}

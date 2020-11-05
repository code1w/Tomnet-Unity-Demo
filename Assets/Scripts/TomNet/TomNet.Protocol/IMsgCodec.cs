/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-18
 *  修改 2020-09-21
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.NetWork;
using TomNet.Common;

namespace TomNet.Protocol
{
    public class IMsgCodec
    {
        private Dictionary<string, Type> typelist = new Dictionary<string, Type>();
        public virtual object OnGenerateMsg(IMsgHeader header, ByteBuffer data) { return null;}
        public virtual ByteBuffer GenerateBinaryMessage(object message) { return null; }
        public void RegisterMsgType(string key, Type type)
        {
            typelist.Add(key, type);
        }

        public Type GetMsgType(string name)
        {
            return typelist[name];
        }

    }
}

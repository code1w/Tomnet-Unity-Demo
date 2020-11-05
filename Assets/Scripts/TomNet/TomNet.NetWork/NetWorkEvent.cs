/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/16
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Core;

namespace TomNet.NetWork
{
    public class NetWorkEvent : BaseEvent
    {
        public static readonly string CONNECT = "connect";
        public static readonly string CONNECTFAILURE = "connectfailure";
        public static readonly string DISCONNECT = "disconnect";
        public static readonly string RECONNECTION_TRY = "reconnectionTry";
        public static readonly string IO_ERROR = "ioError";
        public static readonly string SECURITY_ERROR = "securityError";
        public static readonly string DATA_ERROR = "dataError";
        public NetWorkEvent(string type)
            : base(type, null)
        {
        }

        public NetWorkEvent(string type, Dictionary<string, object> arguments)
            : base(type, arguments)
        {
        }
    }
}

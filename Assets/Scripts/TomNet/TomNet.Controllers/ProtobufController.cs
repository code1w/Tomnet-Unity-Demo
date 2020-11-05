/* 
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-21
 *  修改 2020-09-21
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.NetWork;
using TomNet.Protocol;

namespace TomNet.Controller
{
    public class ProtobufController : IController
    {

        public ProtobufController(INetWorkClient network):base(network)
        {

        }

        public override void HandleMesage(IMsgHeader header, object message)
        {
            DefaultMsgHeader dheader = header as DefaultMsgHeader;

            RequestProtoBufDelegate cb = this.Callbacks[dheader.MsgType];
            if(cb != null)
            {
                cb(network, message);
            }

            Debug.WriteLine("call Protobuf controller handle message");
        }
    }
}

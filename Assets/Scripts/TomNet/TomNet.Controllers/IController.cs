/* 
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  crate:  2020-09-21
 *  modify: 2020-09-21
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.NetWork;
using TomNet.Protocol;

namespace TomNet.Controller
{
    public abstract class IController
    {
        protected INetWorkClient network;
        public Dictionary<string, RequestProtoBufDelegate> Callbacks;
        public string Id
        {
            get;
            set;
        }

        public INetWorkClient Network
        {
            get
            {
                if(network != null)
                {
                    return network;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if(network == null)
                {
                    network = value;
                }
            }
        }

        public IController(INetWorkClient network)
        {
            this.network = network;
            Callbacks = new Dictionary<string, RequestProtoBufDelegate>();
        }

        public void RegisterMsgCallback(string key , RequestProtoBufDelegate cb)
        {
            Callbacks.Add(key, cb);
        }
        public abstract void HandleMesage(IMsgHeader header, object message);
    }
}

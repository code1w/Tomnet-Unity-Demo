/* 
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-10
 *  修改 2020-09-21
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Core;
using TomNet.Protocol;
using TomNet.Sockets;
using TomNet.Controller;

namespace TomNet.NetWork
{
	public interface INetWorkClient
    {
		string ConnectionMode
		{
			get;
		}

		bool Connected
		{
			get;
		}

		IIOHandler IoHandler
		{
			get;
			set;
		}

		IProtocol Protocol
        {
			get;
			set;
        }

        IController Controller
        {
			get;
			set;
        }

        int CompressionThreshold
		{
			get;
			set;
		}

		int MaxMessageSize
		{
			get;
			set;
		}

		ISocket Socket
		{
			get;
		}

		bool IsReconnecting
		{
			get;
			set;
		}

		int ReconnectionSeconds
		{
			get;
			set;
		}

		EventDispatcher Dispatcher
		{
			get;
			set;
		}

		string ConnectionHost
		{
			get;
		}

		int ConnectionPort
		{
			get;
		}

		bool IsBinProtocol
		{
			get;
		}

		void Initialize();
		void Destroy();
		void Connect(string host, int port);
		void Send(byte[] data);
		void Send(object message);

		void Disconnect();
		void Disconnect(string reason);
		void StopReconnection();
		void KillConnection();
		void AddEventListener(string eventType, EventListenerDelegate listener);
		void RegisterMsgCallback(string type, RequestProtoBufDelegate cb);
		void RegisterMsgCallback(Type type, RequestProtoBufDelegate cb);

	}
}


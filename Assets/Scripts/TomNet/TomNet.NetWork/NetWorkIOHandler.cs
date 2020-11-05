/* 
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin  <qunshuok@gmail.com>
 *
 *  2020-09-18
 *  
 */


using System;
using TomNet.NetWork;
using TomNet.Common;
using TomNet.Protocol;
using TomNet.Exceptions;
using System.Diagnostics;
using Google.Protobuf.Reflection;
using TomNet.Core;
namespace TomNet.NetWork
{
	public class NetWorkIOHandler : IIOHandler
	{
		private IProtocol protocol = null;
		private INetWorkClient network = null;


        public NetWorkIOHandler(IProtocol protocol)
        {
			this.protocol = protocol;

		}

		public NetWorkIOHandler(INetWorkClient network)
        {
			this.network = network;
			this.protocol = network.Protocol;
        }


        /// <summary>
        /// �յ�һ�����������ݰ�
        /// </summary>
        /// <param name="data"></param>
        public void OnDataRead(ByteBuffer data)
		{
			if (data.Readable() <= 0)
			{
				throw new TomNetError("Unexpected empty packet data: no readable bytes available!");
			}
			HandleNetWorkPacket(data);
		}

		private void HandleNetWorkPacket(ByteBuffer data)
		{
			try
			{
				IMsgHeader header = protocol.GenerateHeader(protocol.PayLoadType);
				header.FromBinary(data);

				// TODO ����
				// TODO ��ѹ

				var message = CenerateMsg(protocol.Header, data);
				HandleMesage(header, message);
			}
            catch (Exception ex)
			{
				Debug.WriteLine("net io handler handle packet error !");

			}
			
		}
		
		private object CenerateMsg(IMsgHeader header, ByteBuffer data)
        {
			var mesage = protocol.Codec.OnGenerateMsg(protocol.Header, data);
			return mesage;
		}

		private void HandleMesage(IMsgHeader header, object message)
        {
			network.Controller.HandleMesage(header, message);
		}

		public void OnDataWrite(ByteBuffer message)
        {
			TcpNetWorkClinet net = network as TcpNetWorkClinet;
			IMsgHeader header = protocol.GenerateHeader(false, false);
			net.ThreadManager.EnqueueSend(WriteBinaryData, header, message, false);
			
		}

        private void WriteBinaryData(IMsgHeader header, ByteBuffer binData, bool udp = false)
        {
            if (header.Compressed)
            {
                // TODO
            }
            if (header.Encrypted)
            {
                // TODO
            }

            else if (network.Socket.IsConnected)
            {
                WriteTCP(binData);
            }
        }

		private void WriteTCP(ByteBuffer writeBuffer)
		{
			network.Socket.Write(writeBuffer);
		}
	}
}

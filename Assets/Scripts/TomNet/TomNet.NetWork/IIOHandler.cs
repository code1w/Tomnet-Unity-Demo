/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/17
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Protocol;
using TomNet.Common;

namespace TomNet.NetWork
{
    public interface IIOHandler
    {
		void OnDataRead(ByteBuffer message);
		void OnDataWrite(ByteBuffer message);
	}
}

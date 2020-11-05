/*
 * This file is part of the TomNet package.
 *
 * (a)  <qunshuok@gmail.com>
 *
 *  2020/09/10
 */


using System.Net.Sockets;

namespace TomNet.Sockets
{
    public delegate void OnErrorDelegate(string error, SocketError se);
}

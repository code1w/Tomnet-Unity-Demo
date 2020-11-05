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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.NetWork;

namespace TomNet.Controller
{
    public delegate void RequestProtoBufDelegate(INetWorkClient connect, object msg);
}

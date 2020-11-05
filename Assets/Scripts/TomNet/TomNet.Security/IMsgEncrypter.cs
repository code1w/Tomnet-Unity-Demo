/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-18
 *  修改 2020-09-18
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomNet.Common;
namespace TomNet.Security
{
    public interface IMsgEncrypter
    {
        void Encrypt(ByteBuffer data);
        void Decrypt(ByteBuffer data);
    }
}

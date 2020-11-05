/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  创建 2020-09-18
 *  修改 2020-09-18
 */

using TomNet.Common;

namespace TomNet.Security
{
    public class CryptoKey
    {
        private ByteBuffer iv;
        private ByteBuffer key;
        public ByteBuffer IV => iv;
        public ByteBuffer Key => key;
        public CryptoKey(ByteBuffer iv, ByteBuffer key)
        {
            this.iv = iv;
            this.key = key;
        }
    }
}

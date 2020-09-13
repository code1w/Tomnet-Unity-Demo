using System;
using System.Collections;
using System.Collections.Generic;

namespace WebSocketSharp
{
    internal class PayloadData : IEnumerable<byte>, IEnumerable
    {
        private ushort _code;

        private bool _codeSet;

        private byte[] _data;

        private long _extDataLength;

        private long _length;

        private string _reason;

        private bool _reasonSet;

        public static readonly PayloadData Empty;

        public static readonly ulong MaxLength;

        internal ushort Code
        {
            get
            {
                if (!_codeSet)
                {
                    _code = (ushort)((_length > 1) ? _data.SubArray(0, 2).ToUInt16(ByteOrder.Big) : 1005);
                    _codeSet = true;
                }
                return _code;
            }
        }

        internal long ExtensionDataLength
        {
            get
            {
                return _extDataLength;
            }
            set
            {
                _extDataLength = value;
            }
        }

        internal bool HasReservedCode => _length > 1 && Code.IsReserved();

        internal string Reason
        {
            get
            {
                if (!_reasonSet)
                {
                    _reason = ((_length > 2) ? _data.SubArray(2L, _length - 2).UTF8Decode() : string.Empty);
                    _reasonSet = true;
                }
                return _reason;
            }
        }

        public byte[] ApplicationData => (_extDataLength > 0) ? _data.SubArray(_extDataLength, _length - _extDataLength) : _data;

        public byte[] ExtensionData => (_extDataLength > 0) ? _data.SubArray(0L, _extDataLength) : WebSocket.EmptyBytes;

        public ulong Length => (ulong)_length;

        static PayloadData()
        {
            Empty = new PayloadData();
            MaxLength = 9223372036854775807uL;
        }

        internal PayloadData()
        {
            _code = 1005;
            _reason = string.Empty;
            _data = WebSocket.EmptyBytes;
            _codeSet = true;
            _reasonSet = true;
        }

        internal PayloadData(byte[] data)
            : this(data, data.LongLength)
        {
        }

        internal PayloadData(byte[] data, long length)
        {
            _data = data;
            _length = length;
        }

        internal PayloadData(ushort code, string reason)
        {
            _code = code;
            _reason = (reason ?? string.Empty);
            _data = code.Append(reason);
            _length = _data.LongLength;
            _codeSet = true;
            _reasonSet = true;
        }

        internal void Mask(byte[] key)
        {
            for (long num = 0L; num < _length; num++)
            {
                _data[num] = (byte)(_data[num] ^ key[num % 4]);
            }
        }

        public IEnumerator<byte> GetEnumerator()
        {
            byte[] data = _data;
            for (int i = 0; i < data.Length; i++)
            {
                yield return data[i];
            }
        }

        public byte[] ToArray()
        {
            return _data;
        }

        public override string ToString()
        {
            return BitConverter.ToString(_data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

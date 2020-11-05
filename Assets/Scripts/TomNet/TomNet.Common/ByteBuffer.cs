/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/10
 */

/*
 * Modified from muduo project http://github.com/chenshuo/muduo
 * @see https://github.com/chenshuo/muduo/blob/master/muduo/net/Buffer.h and https://github.com/chenshuo/muduo/blob/master/muduo/net/Buffer.cc
 * */

using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;


namespace TomNet.Common
{
	public class ByteBuffer
	{
		private static readonly int BUFSIZE = 1024;
		private byte[] buffer_;
		private int write_index_ = 0;
		private int read_index_ = 0;
		private int capacity_ = 0;
		private int reserved_prepend_size_ = 8;

		public ByteBuffer()
		{
			buffer_ = new byte[BUFSIZE];
			capacity_ = BUFSIZE;
			write_index_ = reserved_prepend_size_;
			read_index_ = reserved_prepend_size_;
			
		}

		/// <summary>
		/// ���ػ������пɶ����ֽ���.
		/// </summary>
		/// <returns></returns>
		
		public int Readable()
        {
			return write_index_ - read_index_;
        }

		public byte[] Begin()
        {
			return buffer_;
        }

		public int Capacity()
        {
			return capacity_;
		}

		public void Swap(ref ByteBuffer rhs)
        {
			capacity_ = rhs.capacity_;
			read_index_ = rhs.read_index_;
			write_index_ = rhs.write_index_;
			reserved_prepend_size_ = rhs.reserved_prepend_size_;
			Buffer.BlockCopy(rhs.buffer_, 0, buffer_, 0, rhs.capacity_);
		}

		public int  WritableBytes()
		{
	        return capacity_ - write_index_;
		}

		public int PrependableBytes()
		{
			return read_index_;
		}

		public void EnsureWriteableBytes(int len)
        {
			if(WritableBytes() < len)
            {
				Grow(len);
            }

        }

		public void Grow(int len)
        {
			if (WritableBytes() + PrependableBytes() < len + reserved_prepend_size_)
			{
				int n = capacity_ * 2 + len;
				int m = Readable();
				byte[] d = new byte[n];
				Buffer.BlockCopy(buffer_, read_index_, d, reserved_prepend_size_, m);
				write_index_ = m + reserved_prepend_size_;
				read_index_ = reserved_prepend_size_;
				capacity_ = n;
				buffer_ = d;
			}
            else
            {
				int readable = Readable();
				Buffer.BlockCopy(buffer_, read_index_, buffer_, reserved_prepend_size_, readable);
				read_index_ = reserved_prepend_size_;
				write_index_ = read_index_ + readable;
            }

        }

		public void Truncate(int n)
		{
			if (n == 0)
			{
				read_index_ = reserved_prepend_size_;
				write_index_ = reserved_prepend_size_;
			}
			else if (write_index_ > read_index_ + n)
			{
				write_index_ = read_index_ + n;
			}
		}

		/// <summary>
		/// ������������������ݺ�,�������������ݶ�����,
		/// ���Ե��ô˺����ع���������ָ��λ��
		/// </summary>
		/// <param name="size"></param>
		public void Rollback(int size)
        {
			if(size > 0)
            {
                if (read_index_ - reserved_prepend_size_ >= size)
                {
                    read_index_ = read_index_ - size;
                }
            }
			else
            {
				read_index_ = reserved_prepend_size_;
			}
        }

		public void Reset()
		{
			Truncate(0);
		}

		public void Skip(int len)
        {
			if(len <= Readable())
            {
				read_index_ += len;
            }
            else
            {
				Reset();
			}
        }

		public void Write(byte[] data, int ofs, int len)
		{
			EnsureWriteableBytes(len);
			Buffer.BlockCopy(data, ofs, buffer_, write_index_, len);
			write_index_ += len;
		}

		public void WriteBytes(byte[] data)
		{
			Write(data, 0, data.Length);
		}

		public void AppendByte(byte b)
		{
			AppendBytes(new byte[1]
			{
				b
			});
		}

		public void AppendBytes(byte[] data)
		{
			Write(data, 0, data.Length);
		}

		public void AppendBool(bool b)
		{
			AppendBytes(new byte[1]
			{
				(byte)(b ? 1 : 0)
			});
		}

		public void AppendInt64(long  x )
        {
			long i64 = IPAddress.HostToNetworkOrder(x);
			byte[] bytes = BitConverter.GetBytes(i64);
			AppendBytes(bytes);
		}

		public void AppendInt32(int x)
		{
			int i32 = IPAddress.HostToNetworkOrder(x);
			byte[] bytes = BitConverter.GetBytes(i32);
			AppendBytes(bytes);
		}

		public void AppendInt16(short x)
		{
			short i16 = IPAddress.HostToNetworkOrder(x);
			byte[] bytes = BitConverter.GetBytes(i16);
			AppendBytes(bytes);
		}

		private void AppendString(string str)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			int len = bytes.Length;
			EnsureWriteableBytes(sizeof(int) + len);
			AppendInt32(len);
			Buffer.BlockCopy(bytes, 0, buffer_, write_index_, len);
		}

		public long PeekInt64()
        {
			byte[] buf = PeekBytes(sizeof(long));
			long i64 = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buf, 0));
			return i64;
		}

		public int PeekInt32()
        {
			byte[] buf = PeekBytes(sizeof(int));
			int i32 = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
			return i32;
		}

		public short PeekInt16()
        {
			byte[] buf = PeekBytes(sizeof(short));
			short i16 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buf, 0));
			return i16;
		}

		public byte PeekInt8()
        {
			byte[] buf = PeekBytes(sizeof(byte));
			return buf[0];
		}

		public byte[] PeekBytes(int len ,int offset = 0)
		{
			if(Readable() >= len)
            {
				byte[] array = new byte[len];
				Buffer.BlockCopy(buffer_, read_index_ + offset, array, 0, len);
				return array;
            }

			return null;
		}

		public byte[] ReadBytes(int len, int offset = 0)
        {
			if(len > 0 && Readable() >= len )
            {
				byte[] array = new byte[len];
				Buffer.BlockCopy(buffer_, read_index_ + offset, array, 0, len);
				Skip(len);
				return array;
            }
			return null;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public byte[] Data()
        {
			if(Readable()>0)
            {
                byte[] array = new byte[Readable()];
                Buffer.BlockCopy(buffer_, read_index_, array, 0, Readable());
				return array;
            }
            return null;
        }

        public string PeekString()
		{
			int len = PeekInt32();
			byte[] buf = PeekBytes(len , sizeof(int));
			string str = Encoding.UTF8.GetString(buf);
			return str;
		}

		public long ReadInt64()
        {
			long result = PeekInt64();
			Skip(sizeof(long));
			return result;
        }

		public int ReadInt32()
		{
			int result = PeekInt32();
			Skip(sizeof(int));
			return result;
		}

		public short ReadInt16()
		{
			short result = PeekInt16();
			Skip(sizeof(short));
			return result;
		}

		public byte ReadInt8()
        {
			byte result = PeekInt8();
			Skip(sizeof(byte));
			return result;
		}

		private string ReadString()
		{
			string result = PeekString();
			Skip(sizeof(int)+ result.Length);
			return result;
		}
	}
}

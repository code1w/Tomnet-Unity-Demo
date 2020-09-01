using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace WebSocketSharp.Net
{
	internal class ChunkStream
	{
		private int _chunkRead;

		private int _chunkSize;

		private List<Chunk> _chunks;

		private bool _gotIt;

		private WebHeaderCollection _headers;

		private StringBuilder _saved;

		private bool _sawCr;

		private InputChunkState _state;

		private int _trailerState;

		internal WebHeaderCollection Headers => _headers;

		public int ChunkLeft => _chunkSize - _chunkRead;

		public bool WantMore => _state != InputChunkState.End;

		public ChunkStream(WebHeaderCollection headers)
		{
			_headers = headers;
			_chunkSize = -1;
			_chunks = new List<Chunk>();
			_saved = new StringBuilder();
		}

		public ChunkStream(byte[] buffer, int offset, int count, WebHeaderCollection headers)
			: this(headers)
		{
			Write(buffer, offset, count);
		}

		private int read(byte[] buffer, int offset, int count)
		{
			int num = 0;
			int count2 = _chunks.Count;
			for (int i = 0; i < count2; i++)
			{
				Chunk chunk = _chunks[i];
				if (chunk == null)
				{
					continue;
				}
				if (chunk.ReadLeft == 0)
				{
					_chunks[i] = null;
					continue;
				}
				num += chunk.Read(buffer, offset + num, count - num);
				if (num == count)
				{
					break;
				}
			}
			return num;
		}

		private static string removeChunkExtension(string value)
		{
			int num = value.IndexOf(';');
			return (num > -1) ? value.Substring(0, num) : value;
		}

		private InputChunkState seekCrLf(byte[] buffer, ref int offset, int length)
		{
			if (!_sawCr)
			{
				if (buffer[offset++] != 13)
				{
					throwProtocolViolation("CR is expected.");
				}
				_sawCr = true;
				if (offset == length)
				{
					return InputChunkState.DataEnded;
				}
			}
			if (buffer[offset++] != 10)
			{
				throwProtocolViolation("LF is expected.");
			}
			return InputChunkState.None;
		}

		private InputChunkState setChunkSize(byte[] buffer, ref int offset, int length)
		{
			byte b = 0;
			while (offset < length)
			{
				b = buffer[offset++];
				if (_sawCr)
				{
					if (b != 10)
					{
						throwProtocolViolation("LF is expected.");
					}
					break;
				}
				switch (b)
				{
				case 13:
					_sawCr = true;
					continue;
				case 10:
					throwProtocolViolation("LF is unexpected.");
					break;
				}
				if (b == 32)
				{
					_gotIt = true;
				}
				if (!_gotIt)
				{
					_saved.Append((char)b);
				}
				if (_saved.Length > 20)
				{
					throwProtocolViolation("The chunk size is too long.");
				}
			}
			if (!_sawCr || b != 10)
			{
				return InputChunkState.None;
			}
			_chunkRead = 0;
			try
			{
				_chunkSize = int.Parse(removeChunkExtension(_saved.ToString()), NumberStyles.HexNumber);
			}
			catch
			{
				throwProtocolViolation("The chunk size cannot be parsed.");
			}
			if (_chunkSize == 0)
			{
				_trailerState = 2;
				return InputChunkState.Trailer;
			}
			return InputChunkState.Data;
		}

		private InputChunkState setTrailer(byte[] buffer, ref int offset, int length)
		{
			if (_trailerState == 2 && buffer[offset] == 13 && _saved.Length == 0)
			{
				offset++;
				if (offset < length && buffer[offset] == 10)
				{
					offset++;
					return InputChunkState.End;
				}
				offset--;
			}
			while (offset < length && _trailerState < 4)
			{
				byte b = buffer[offset++];
				_saved.Append((char)b);
				if (_saved.Length > 4196)
				{
					throwProtocolViolation("The trailer is too long.");
				}
				if (_trailerState == 1 || _trailerState == 3)
				{
					if (b != 10)
					{
						throwProtocolViolation("LF is expected.");
					}
					_trailerState++;
					continue;
				}
				switch (b)
				{
				case 13:
					_trailerState++;
					continue;
				case 10:
					throwProtocolViolation("LF is unexpected.");
					break;
				}
				_trailerState = 0;
			}
			if (_trailerState < 4)
			{
				return InputChunkState.Trailer;
			}
			_saved.Length -= 2;
			StringReader stringReader = new StringReader(_saved.ToString());
			string text;
			while ((text = stringReader.ReadLine()) != null && text.Length > 0)
			{
				_headers.Add(text);
			}
			return InputChunkState.End;
		}

		private static void throwProtocolViolation(string message)
		{
			throw new WebException(message, null, WebExceptionStatus.ServerProtocolViolation, null);
		}

		private void write(byte[] buffer, ref int offset, int length)
		{
			if (_state == InputChunkState.End)
			{
				throwProtocolViolation("The chunks were ended.");
			}
			if (_state == InputChunkState.None)
			{
				_state = setChunkSize(buffer, ref offset, length);
				if (_state == InputChunkState.None)
				{
					return;
				}
				_saved.Length = 0;
				_sawCr = false;
				_gotIt = false;
			}
			if (_state == InputChunkState.Data && offset < length)
			{
				_state = writeData(buffer, ref offset, length);
				if (_state == InputChunkState.Data)
				{
					return;
				}
			}
			if (_state == InputChunkState.DataEnded && offset < length)
			{
				_state = seekCrLf(buffer, ref offset, length);
				if (_state == InputChunkState.DataEnded)
				{
					return;
				}
				_sawCr = false;
			}
			if (_state == InputChunkState.Trailer && offset < length)
			{
				_state = setTrailer(buffer, ref offset, length);
				if (_state == InputChunkState.Trailer)
				{
					return;
				}
				_saved.Length = 0;
			}
			if (offset < length)
			{
				write(buffer, ref offset, length);
			}
		}

		private InputChunkState writeData(byte[] buffer, ref int offset, int length)
		{
			int num = length - offset;
			int num2 = _chunkSize - _chunkRead;
			if (num > num2)
			{
				num = num2;
			}
			byte[] array = new byte[num];
			Buffer.BlockCopy(buffer, offset, array, 0, num);
			_chunks.Add(new Chunk(array));
			offset += num;
			_chunkRead += num;
			return (_chunkRead != _chunkSize) ? InputChunkState.Data : InputChunkState.DataEnded;
		}

		internal void ResetBuffer()
		{
			_chunkRead = 0;
			_chunkSize = -1;
			_chunks.Clear();
		}

		internal int WriteAndReadBack(byte[] buffer, int offset, int writeCount, int readCount)
		{
			Write(buffer, offset, writeCount);
			return Read(buffer, offset, readCount);
		}

		public int Read(byte[] buffer, int offset, int count)
		{
			if (count <= 0)
			{
				return 0;
			}
			return read(buffer, offset, count);
		}

		public void Write(byte[] buffer, int offset, int count)
		{
			if (count > 0)
			{
				write(buffer, ref offset, offset + count);
			}
		}
	}
}

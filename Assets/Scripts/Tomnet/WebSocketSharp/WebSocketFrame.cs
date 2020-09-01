using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebSocketSharp
{
	internal class WebSocketFrame : IEnumerable<byte>, IEnumerable
	{
		private byte[] _extPayloadLength;

		private Fin _fin;

		private Mask _mask;

		private byte[] _maskingKey;

		private Opcode _opcode;

		private PayloadData _payloadData;

		private byte _payloadLength;

		private Rsv _rsv1;

		private Rsv _rsv2;

		private Rsv _rsv3;

		internal static readonly byte[] EmptyPingBytes;

		internal int ExtendedPayloadLengthCount => (_payloadLength >= 126) ? ((_payloadLength == 126) ? 2 : 8) : 0;

		internal ulong FullPayloadLength => (_payloadLength < 126) ? _payloadLength : ((_payloadLength == 126) ? _extPayloadLength.ToUInt16(ByteOrder.Big) : _extPayloadLength.ToUInt64(ByteOrder.Big));

		public byte[] ExtendedPayloadLength => _extPayloadLength;

		public Fin Fin => _fin;

		public bool IsBinary => _opcode == Opcode.Binary;

		public bool IsClose => _opcode == Opcode.Close;

		public bool IsCompressed => _rsv1 == Rsv.On;

		public bool IsContinuation => _opcode == Opcode.Cont;

		public bool IsControl => (int)_opcode >= 8;

		public bool IsData => _opcode == Opcode.Text || _opcode == Opcode.Binary;

		public bool IsFinal => _fin == Fin.Final;

		public bool IsFragment => _fin == Fin.More || _opcode == Opcode.Cont;

		public bool IsMasked => _mask == Mask.On;

		public bool IsPing => _opcode == Opcode.Ping;

		public bool IsPong => _opcode == Opcode.Pong;

		public bool IsText => _opcode == Opcode.Text;

		public ulong Length => (ulong)(2L + (long)(_extPayloadLength.Length + _maskingKey.Length)) + _payloadData.Length;

		public Mask Mask => _mask;

		public byte[] MaskingKey => _maskingKey;

		public Opcode Opcode => _opcode;

		public PayloadData PayloadData => _payloadData;

		public byte PayloadLength => _payloadLength;

		public Rsv Rsv1 => _rsv1;

		public Rsv Rsv2 => _rsv2;

		public Rsv Rsv3 => _rsv3;

		static WebSocketFrame()
		{
			EmptyPingBytes = CreatePingFrame(mask: false).ToArray();
		}

		private WebSocketFrame()
		{
		}

		internal WebSocketFrame(Opcode opcode, PayloadData payloadData, bool mask)
			: this(Fin.Final, opcode, payloadData, compressed: false, mask)
		{
		}

		internal WebSocketFrame(Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask)
			: this(fin, opcode, new PayloadData(data), compressed, mask)
		{
		}

		internal WebSocketFrame(Fin fin, Opcode opcode, PayloadData payloadData, bool compressed, bool mask)
		{
			_fin = fin;
			_rsv1 = ((opcode.IsData() && compressed) ? Rsv.On : Rsv.Off);
			_rsv2 = Rsv.Off;
			_rsv3 = Rsv.Off;
			_opcode = opcode;
			ulong length = payloadData.Length;
			if (length < 126)
			{
				_payloadLength = (byte)length;
				_extPayloadLength = WebSocket.EmptyBytes;
			}
			else if (length < 65536)
			{
				_payloadLength = 126;
				_extPayloadLength = ((ushort)length).InternalToByteArray(ByteOrder.Big);
			}
			else
			{
				_payloadLength = 127;
				_extPayloadLength = length.InternalToByteArray(ByteOrder.Big);
			}
			if (mask)
			{
				_mask = Mask.On;
				_maskingKey = createMaskingKey();
				payloadData.Mask(_maskingKey);
			}
			else
			{
				_mask = Mask.Off;
				_maskingKey = WebSocket.EmptyBytes;
			}
			_payloadData = payloadData;
		}

		private static byte[] createMaskingKey()
		{
			byte[] array = new byte[4];
			WebSocket.RandomNumber.GetBytes(array);
			return array;
		}

		private static string dump(WebSocketFrame frame)
		{
			ulong length = frame.Length;
			long num = (long)(length / 4uL);
			int num2 = (int)(length % 4uL);
			int num3;
			string arg5;
			if (num < 10000)
			{
				num3 = 4;
				arg5 = "{0,4}";
			}
			else if (num < 65536)
			{
				num3 = 4;
				arg5 = "{0,4:X}";
			}
			else if (num < 4294967296L)
			{
				num3 = 8;
				arg5 = "{0,8:X}";
			}
			else
			{
				num3 = 16;
				arg5 = "{0,16:X}";
			}
			string arg6 = $"{{0,{num3}}}";
			string format = string.Format("\n{0} 01234567 89ABCDEF 01234567 89ABCDEF\n{0}+--------+--------+--------+--------+\\n", arg6);
			string lineFmt = $"{arg5}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n";
			string format2 = $"{arg6}+--------+--------+--------+--------+";
			StringBuilder output = new StringBuilder(64);
			Func<Action<string, string, string, string>> func = delegate
			{
				long lineCnt = 0L;
				return delegate(string arg1, string arg2, string arg3, string arg4)
				{
					output.AppendFormat(lineFmt, ++lineCnt, arg1, arg2, arg3, arg4);
				};
			};
			Action<string, string, string, string> action = func();
			output.AppendFormat(format, string.Empty);
			byte[] array = frame.ToArray();
			for (long num4 = 0L; num4 <= num; num4++)
			{
				long num5 = num4 * 4;
				if (num4 < num)
				{
					action(Convert.ToString(array[num5], 2).PadLeft(8, '0'), Convert.ToString(array[num5 + 1], 2).PadLeft(8, '0'), Convert.ToString(array[num5 + 2], 2).PadLeft(8, '0'), Convert.ToString(array[num5 + 3], 2).PadLeft(8, '0'));
				}
				else if (num2 > 0)
				{
					action(Convert.ToString(array[num5], 2).PadLeft(8, '0'), (num2 >= 2) ? Convert.ToString(array[num5 + 1], 2).PadLeft(8, '0') : string.Empty, (num2 == 3) ? Convert.ToString(array[num5 + 2], 2).PadLeft(8, '0') : string.Empty, string.Empty);
				}
			}
			output.AppendFormat(format2, string.Empty);
			return output.ToString();
		}

		private static string print(WebSocketFrame frame)
		{
			byte payloadLength = frame._payloadLength;
			string text = (payloadLength > 125) ? frame.FullPayloadLength.ToString() : string.Empty;
			string text2 = BitConverter.ToString(frame._maskingKey);
			string text3 = (payloadLength == 0) ? string.Empty : ((payloadLength > 125) ? "---" : ((frame.IsText && !frame.IsFragment && !frame.IsMasked && !frame.IsCompressed) ? frame._payloadData.ApplicationData.UTF8Decode() : frame._payloadData.ToString()));
			string format = "\n                    FIN: {0}\n                   RSV1: {1}\n                   RSV2: {2}\n                   RSV3: {3}\n                 Opcode: {4}\n                   MASK: {5}\n         Payload Length: {6}\nExtended Payload Length: {7}\n            Masking Key: {8}\n           Payload Data: {9}";
			return string.Format(format, frame._fin, frame._rsv1, frame._rsv2, frame._rsv3, frame._opcode, frame._mask, payloadLength, text, text2, text3);
		}

		private static WebSocketFrame processHeader(byte[] header)
		{
			if (header.Length != 2)
			{
				throw new WebSocketException("The header of a frame cannot be read from the stream.");
			}
			Fin fin = ((header[0] & 0x80) == 128) ? Fin.Final : Fin.More;
			Rsv rsv = ((header[0] & 0x40) == 64) ? Rsv.On : Rsv.Off;
			Rsv rsv2 = ((header[0] & 0x20) == 32) ? Rsv.On : Rsv.Off;
			Rsv rsv3 = ((header[0] & 0x10) == 16) ? Rsv.On : Rsv.Off;
			byte opcode = (byte)(header[0] & 0xF);
			Mask mask = ((header[1] & 0x80) == 128) ? Mask.On : Mask.Off;
			byte b = (byte)(header[1] & 0x7F);
			string text = (!opcode.IsSupported()) ? "An unsupported opcode." : ((!opcode.IsData() && rsv == Rsv.On) ? "A non data frame is compressed." : ((opcode.IsControl() && fin == Fin.More) ? "A control frame is fragmented." : ((opcode.IsControl() && b > 125) ? "A control frame has a long payload length." : null)));
			if (text != null)
			{
				throw new WebSocketException(CloseStatusCode.ProtocolError, text);
			}
			WebSocketFrame webSocketFrame = new WebSocketFrame();
			webSocketFrame._fin = fin;
			webSocketFrame._rsv1 = rsv;
			webSocketFrame._rsv2 = rsv2;
			webSocketFrame._rsv3 = rsv3;
			webSocketFrame._opcode = (Opcode)opcode;
			webSocketFrame._mask = mask;
			webSocketFrame._payloadLength = b;
			return webSocketFrame;
		}

		private static WebSocketFrame readExtendedPayloadLength(Stream stream, WebSocketFrame frame)
		{
			int extendedPayloadLengthCount = frame.ExtendedPayloadLengthCount;
			if (extendedPayloadLengthCount == 0)
			{
				frame._extPayloadLength = WebSocket.EmptyBytes;
				return frame;
			}
			byte[] array = stream.ReadBytes(extendedPayloadLengthCount);
			if (array.Length != extendedPayloadLengthCount)
			{
				throw new WebSocketException("The extended payload length of a frame cannot be read from the stream.");
			}
			frame._extPayloadLength = array;
			return frame;
		}

		private static void readExtendedPayloadLengthAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			int len = frame.ExtendedPayloadLengthCount;
			if (len == 0)
			{
				frame._extPayloadLength = WebSocket.EmptyBytes;
				completed(frame);
				return;
			}
			stream.ReadBytesAsync(len, delegate(byte[] bytes)
			{
				if (bytes.Length != len)
				{
					throw new WebSocketException("The extended payload length of a frame cannot be read from the stream.");
				}
				frame._extPayloadLength = bytes;
				completed(frame);
			}, error);
		}

		private static WebSocketFrame readHeader(Stream stream)
		{
			return processHeader(stream.ReadBytes(2));
		}

		private static void readHeaderAsync(Stream stream, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			stream.ReadBytesAsync(2, delegate(byte[] bytes)
			{
				completed(processHeader(bytes));
			}, error);
		}

		private static WebSocketFrame readMaskingKey(Stream stream, WebSocketFrame frame)
		{
			int num = frame.IsMasked ? 4 : 0;
			if (num == 0)
			{
				frame._maskingKey = WebSocket.EmptyBytes;
				return frame;
			}
			byte[] array = stream.ReadBytes(num);
			if (array.Length != num)
			{
				throw new WebSocketException("The masking key of a frame cannot be read from the stream.");
			}
			frame._maskingKey = array;
			return frame;
		}

		private static void readMaskingKeyAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			int len = frame.IsMasked ? 4 : 0;
			if (len == 0)
			{
				frame._maskingKey = WebSocket.EmptyBytes;
				completed(frame);
				return;
			}
			stream.ReadBytesAsync(len, delegate(byte[] bytes)
			{
				if (bytes.Length != len)
				{
					throw new WebSocketException("The masking key of a frame cannot be read from the stream.");
				}
				frame._maskingKey = bytes;
				completed(frame);
			}, error);
		}

		private static WebSocketFrame readPayloadData(Stream stream, WebSocketFrame frame)
		{
			ulong fullPayloadLength = frame.FullPayloadLength;
			if (fullPayloadLength == 0)
			{
				frame._payloadData = PayloadData.Empty;
				return frame;
			}
			if (fullPayloadLength > PayloadData.MaxLength)
			{
				throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");
			}
			long num = (long)fullPayloadLength;
			byte[] array = (frame._payloadLength < 127) ? stream.ReadBytes((int)fullPayloadLength) : stream.ReadBytes(num, 1024);
			if (array.LongLength != num)
			{
				throw new WebSocketException("The payload data of a frame cannot be read from the stream.");
			}
			frame._payloadData = new PayloadData(array, num);
			return frame;
		}

		private static void readPayloadDataAsync(Stream stream, WebSocketFrame frame, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			ulong fullPayloadLength = frame.FullPayloadLength;
			if (fullPayloadLength == 0)
			{
				frame._payloadData = PayloadData.Empty;
				completed(frame);
				return;
			}
			if (fullPayloadLength > PayloadData.MaxLength)
			{
				throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");
			}
			long llen = (long)fullPayloadLength;
			Action<byte[]> completed2 = delegate(byte[] bytes)
			{
				if (bytes.LongLength != llen)
				{
					throw new WebSocketException("The payload data of a frame cannot be read from the stream.");
				}
				frame._payloadData = new PayloadData(bytes, llen);
				completed(frame);
			};
			if (frame._payloadLength < 127)
			{
				stream.ReadBytesAsync((int)fullPayloadLength, completed2, error);
			}
			else
			{
				stream.ReadBytesAsync(llen, 1024, completed2, error);
			}
		}

		internal static WebSocketFrame CreateCloseFrame(PayloadData payloadData, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Close, payloadData, compressed: false, mask);
		}

		internal static WebSocketFrame CreatePingFrame(bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Ping, PayloadData.Empty, compressed: false, mask);
		}

		internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(data), compressed: false, mask);
		}

		internal static WebSocketFrame CreatePongFrame(PayloadData payloadData, bool mask)
		{
			return new WebSocketFrame(Fin.Final, Opcode.Pong, payloadData, compressed: false, mask);
		}

		internal static WebSocketFrame ReadFrame(Stream stream, bool unmask)
		{
			WebSocketFrame webSocketFrame = readHeader(stream);
			readExtendedPayloadLength(stream, webSocketFrame);
			readMaskingKey(stream, webSocketFrame);
			readPayloadData(stream, webSocketFrame);
			if (unmask)
			{
				webSocketFrame.Unmask();
			}
			return webSocketFrame;
		}

		internal static void ReadFrameAsync(Stream stream, bool unmask, Action<WebSocketFrame> completed, Action<Exception> error)
		{
			readHeaderAsync(stream, delegate(WebSocketFrame frame)
			{
				readExtendedPayloadLengthAsync(stream, frame, delegate(WebSocketFrame frame1)
				{
					readMaskingKeyAsync(stream, frame1, delegate(WebSocketFrame frame2)
					{
						readPayloadDataAsync(stream, frame2, delegate(WebSocketFrame frame3)
						{
							if (unmask)
							{
								frame3.Unmask();
							}
							completed(frame3);
						}, error);
					}, error);
				}, error);
			}, error);
		}

		internal void Unmask()
		{
			if (_mask != 0)
			{
				_mask = Mask.Off;
				_payloadData.Mask(_maskingKey);
				_maskingKey = WebSocket.EmptyBytes;
			}
		}

		public IEnumerator<byte> GetEnumerator()
		{
			byte[] array = ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
		}

		public void Print(bool dumped)
		{
			Console.WriteLine(dumped ? dump(this) : print(this));
		}

		public string PrintToString(bool dumped)
		{
			return dumped ? dump(this) : print(this);
		}

		public byte[] ToArray()
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				int fin = (int)_fin;
				fin = (fin << 1) + (int)_rsv1;
				fin = (fin << 1) + (int)_rsv2;
				fin = (fin << 1) + (int)_rsv3;
				fin = (fin << 4) + (int)_opcode;
				fin = (fin << 1) + (int)_mask;
				fin = (fin << 7) + _payloadLength;
				memoryStream.Write(((ushort)fin).InternalToByteArray(ByteOrder.Big), 0, 2);
				if (_payloadLength > 125)
				{
					memoryStream.Write(_extPayloadLength, 0, (_payloadLength == 126) ? 2 : 8);
				}
				if (_mask == Mask.On)
				{
					memoryStream.Write(_maskingKey, 0, 4);
				}
				if (_payloadLength > 0)
				{
					byte[] array = _payloadData.ToArray();
					if (_payloadLength < 127)
					{
						memoryStream.Write(array, 0, array.Length);
					}
					else
					{
						memoryStream.WriteBytes(array, 1024);
					}
				}
				memoryStream.Close();
				return memoryStream.ToArray();
			}
		}

		public override string ToString()
		{
			return BitConverter.ToString(ToArray());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

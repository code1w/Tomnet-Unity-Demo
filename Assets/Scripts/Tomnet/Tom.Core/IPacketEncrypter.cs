using Tom.Util;

namespace Tom.Core
{
	public interface IPacketEncrypter
	{
		void Encrypt(ByteArray data);

		void Decrypt(ByteArray data);
	}
}

namespace Deja.Crypto.BcPgp
{
	/// <summary>
	/// Public key could not be found
	/// </summary>
	public class PublicKeyNotFoundException : CryptoException
	{
		public PublicKeyNotFoundException(string message)
			: base(message)
		{
		}
	}
}

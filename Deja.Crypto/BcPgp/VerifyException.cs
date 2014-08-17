namespace Deja.Crypto.BcPgp
{
	/// <summary>
	/// Unable to verify signature
	/// </summary>
	public class VerifyException : CryptoException
	{
		public VerifyException(string message)
			: base(message)
		{
		}
	}
}

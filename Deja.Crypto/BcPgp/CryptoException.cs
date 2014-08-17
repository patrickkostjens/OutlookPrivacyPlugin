using System;

namespace Deja.Crypto.BcPgp
{
	/// <summary>
	/// Generic exception during crypto process.
	/// </summary>
	public class CryptoException : Exception
	{
		public CryptoException(string message)
			: base(message)
		{
		}
	}
}

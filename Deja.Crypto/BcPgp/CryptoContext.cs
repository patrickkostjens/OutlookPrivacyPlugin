﻿using System;
using System.Collections.Generic;
using System.IO;

using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Deja.Crypto.BcPgp
{
    public class CryptoContext
    {
	    private const string PublicFilename = "pubring.gpg";
	    private const string PrivateFilename = "secring.gpg";

	    public CryptoContext()
		{
			IsEncrypted = false;
			IsSigned = false;
			SignatureValidated = false;
			IsCompressed = false;
			FailedIntegrityCheck = true;

			Password = null;
			OnePassSignature = null;
			Signature = null;

			var gpgLocations = new List<string>();

			// If GNUPGHOME is set, add to list
			var gpgHome = Environment.GetEnvironmentVariable("GNUPGHOME");
			if (gpgHome != null)
				gpgLocations.Add(gpgHome);

			// If registry key is set, add to list
			using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\GNU\GnuPG"))
			{
				if (key != null)
				{
					gpgHome = key.GetValue("HomeDir", null) as string;

					if (gpgHome != null)
						gpgLocations.Add(gpgHome);
				}
			}

			// Add default location to list
			gpgHome = Environment.GetEnvironmentVariable("APPDATA");
			gpgHome = Path.Combine(gpgHome, "gnupg");
			gpgLocations.Add(gpgHome);

			// Try all possible locations
			foreach(var home in gpgLocations)
			{
				if (File.Exists(Path.Combine(home, PrivateFilename)))
				{
					PublicKeyRingFile = Path.Combine(gpgHome, PublicFilename);
					PrivateKeyRingFile = Path.Combine(gpgHome, PrivateFilename);
					return;
				}

				// Portable gnupg will use a subfolder named 'home'
				if (File.Exists(Path.Combine(home, "home", PrivateFilename)))
				{
					PublicKeyRingFile = Path.Combine(gpgHome, "home", PublicFilename);
					PrivateKeyRingFile = Path.Combine(gpgHome, "home", PrivateFilename);
					return;
				}
			}

			// failed to find keyrings!
			throw new ApplicationException("Error, failed to locate keyrings! Please specify location using GNUPGHOME environmental variable.");
		}

		public CryptoContext(char[] password) : this()
		{
			Password = password;
		}

		public CryptoContext(char[] password, string publicKeyRing, string secretKeyRing) : this(password)
		{
			PublicKeyRingFile = publicKeyRing;
			PrivateKeyRingFile = secretKeyRing;
		}

		public CryptoContext(CryptoContext context)
		{
			if (context == null)
				throw new Exception("Error, crypto context is null.");

			IsEncrypted = false;
			IsSigned = false;
			SignatureValidated = false;
			IsCompressed = false;
			OnePassSignature = null;
			Signature = null;
			SignedBy = null;

			Password = context.Password;
			PublicKeyRingFile = context.PublicKeyRingFile;
			PrivateKeyRingFile = context.PrivateKeyRingFile;
		}

		public char[] Password { get; set; }
        public string PublicKeyRingFile { get; set; }
        public string PrivateKeyRingFile { get; set; }

		public bool FailedIntegrityCheck { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsSigned { get; set; }
        public bool SignatureValidated { get; set; }
		public PgpPublicKey SignedBy{ get; set; }
		public string SignedByUserId
		{
			get
			{
				if (SignedBy == null)
					return "Missing Key";

				string lastId = null;

				foreach (string id in SignedBy.GetUserIds())
				{
					lastId = id;
					if (id.Contains("@"))
						return id;
				}

				return lastId;
			}
		}
		public string SignedByKeyId
		{
			get
			{
				if (SignedBy == null)
				{
					if (OnePassSignature != null)
					{
						return OnePassSignature.KeyId.ToString("X");
					}
					return "Unknown KeyId";
				}

				return SignedBy.KeyId.ToString("X");
			}
		}

        public PgpOnePassSignature OnePassSignature { get; set; }
        public PgpSignature Signature { get; set; }
        public PgpSecretKey SecretKey { get; set; }
    }
}

// end

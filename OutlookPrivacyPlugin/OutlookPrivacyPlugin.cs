using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OutlookPrivacyPlugin.Models;
using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;

using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Exception = System.Exception;
using anmar.SharpMimeTools;

using Deja.Crypto.BcPgp;
using NLog;

namespace OutlookPrivacyPlugin
{
    public partial class OutlookPrivacyPlugin
    {
		#region VSTO generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InternalStartup()
		{
			Startup += OutlookGnuPG_Startup;
			Shutdown += OutlookGnuPG_Shutdown;
		}

		#endregion

	    private static OutlookPrivacyPlugin _singletonInstance;

	    public static OutlookPrivacyPlugin Instance
	    {
		    get { return _singletonInstance; }
	    }

		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();


		private Properties.Settings _settings;
		private GnuPGCommandBar _gpgCommandBar;
		private bool _autoDecrypt;
		private Outlook.Explorers _explorers;
		private Outlook.Inspectors _inspectors;        // Outlook inspectors collection
		private Encoding _encoding = Encoding.UTF8;
		// This dictionary holds our Wrapped Inspectors, Explorers, MailItems
		private Dictionary<Guid, object> _wrappedObjects;

		char[] Passphrase { get; set; }

		// PGP Headers
		// http://www.ietf.org/rfc/rfc4880.txt page 54
		const string PgpSignedHeader = "BEGIN PGP SIGNED MESSAGE";
		const string PgpEncryptedHeader = "BEGIN PGP MESSAGE";
		const string PgpHeaderPattern = "BEGIN PGP( SIGNED)? MESSAGE";
	    private const string MailHeaderVersion = "Outlook Privacy Plugin";
	    private const string EndPgpMessageGuard = "-----END PGP MESSAGE-----";
	    private const string EncryptionExtension = @"\.(pgp\.asc|gpg\.asc|pgp|gpg|asc)$";
	    private const string PgpSignatureMime = "application/pgp-signature";
	    private const string PgpEncryptedMime = "application/pgp-encrypted";

	    private void OutlookGnuPG_Startup(object sender, EventArgs e)
	    {
			_singletonInstance = this;

			_settings = new Properties.Settings();

			_wrappedObjects = new Dictionary<Guid, object>();

			// Initialize command bar.
			// Must be saved/closed in Explorer MyClose event.
			// See http://social.msdn.microsoft.com/Forums/en-US/vsto/thread/df53276b-6b44-448f-be86-7dd46c3786c7/
			AddGnuPGCommandBar(this.Application.ActiveExplorer());

			// Register an event for ItemSend
			Application.ItemSend += Application_ItemSend;

			// Initialize the outlook explorers
			_explorers = Application.Explorers;
			_explorers.NewExplorer += OutlookGnuPG_NewExplorer;
			for (var i = _explorers.Count; i >= 1; i--)
			{
				WrapExplorer(_explorers[i]);
			}

			// Initialize the outlook inspectors
			_inspectors = Application.Inspectors;
			_inspectors.NewInspector += OutlookGnuPG_NewInspector;
		}

		/// <summary>
		/// Shutdown the Add-In.
		/// Note: some closing statements must happen before this event, see OutlookGnuPG_ExplorerClose().
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OutlookGnuPG_Shutdown(object sender, EventArgs e)
		{
			// Unhook event handler
			_inspectors.NewInspector -= OutlookGnuPG_NewInspector;
			_explorers.NewExplorer -= OutlookGnuPG_NewExplorer;

			_wrappedObjects.Clear();
			_wrappedObjects = null;
			_inspectors = null;
			_explorers = null;
		}

		private GnuPGRibbon _ribbon;

		protected override object RequestService(Guid serviceGuid)
		{
			if (serviceGuid == typeof(Office.IRibbonExtensibility).GUID)
			{
				return _ribbon ?? (_ribbon = new GnuPGRibbon());
			}

			return base.RequestService(serviceGuid);
		}

		#region Explorer Logic
		/// <summary>
		/// The NewExplorer event fires whenever a new explorer is created. We use
		/// this event to toggle the visibility of the commandbar.
		/// </summary>
		/// <param name="explorer">the new created Explorer</param>
		void OutlookGnuPG_NewExplorer(Outlook.Explorer explorer)
		{
			WrapExplorer(explorer);
		}

		/// <summary>
		/// Wrap Explorer object to managed Explorer events.
		/// </summary>
		/// <param name="explorer">the outlook explorer to manage</param>
		private void WrapExplorer(Outlook.Explorer explorer)
		{
			if (_wrappedObjects.ContainsValue(explorer))
				return;

			var wrappedExplorer = new ExplorerWrapper(explorer);
			wrappedExplorer.Dispose += ExplorerWrapper_Dispose;
			wrappedExplorer.ViewSwitch += wrappedExplorer_ViewSwitch;
			wrappedExplorer.SelectionChange += wrappedExplorer_SelectionChange;
			wrappedExplorer.Close += wrappedExplorer_Close;
			_wrappedObjects[wrappedExplorer.Id] = explorer;

			AddGnuPGCommandBar(explorer);
		}

		/// <summary>
		/// WrapEvent to dispose the wrappedExplorer
		/// </summary>
		/// <param name="id">the UID of the wrappedExplorer</param>
		/// <param name="o">the wrapped Explorer object</param>
		private void ExplorerWrapper_Dispose(Guid id, object o)
		{
			var wrappedExplorer = o as ExplorerWrapper;
			wrappedExplorer.Dispose -= ExplorerWrapper_Dispose;
			wrappedExplorer.ViewSwitch -= wrappedExplorer_ViewSwitch;
			wrappedExplorer.SelectionChange -= wrappedExplorer_SelectionChange;
			wrappedExplorer.Close -= wrappedExplorer_Close;
			_wrappedObjects.Remove(id);
		}

		/// <summary>
		/// WrapEvent fired for MyClose event.
		/// </summary>
		/// <param name="explorer">the explorer for which a close event fired.</param>
		void wrappedExplorer_Close(Outlook.Explorer explorer)
		{
			if (_gpgCommandBar != null && explorer == _gpgCommandBar.Explorer)
				_gpgCommandBar.SavePosition(_settings);
		}

		/// <summary>
		/// WrapEvent fired for ViewSwitch event.
		/// </summary>
		/// <param name="explorer">the explorer for which a switchview event fired.</param>
		void wrappedExplorer_ViewSwitch(Outlook.Explorer explorer)
		{
			if (_gpgCommandBar == null)
				return;
			_gpgCommandBar.CommandBar.Visible = explorer.CurrentFolder.DefaultMessageClass == "IPM.Note";
		}

	    /// <summary>
		/// WrapEvent fired for SelectionChange event.
		/// </summary>
		/// <param name="explorer">the explorer for which a selectionchange event fired.</param>
		void wrappedExplorer_SelectionChange(Outlook.Explorer explorer)
		{
			var selection = explorer.Selection;
			if (selection.Count != 1)
				return;

			var mailItem = selection[1] as Outlook.MailItem;
			if (mailItem == null || mailItem.Body == null)
				return;

			if (mailItem.BodyFormat == Outlook.OlBodyFormat.olFormatPlain)
			{
				var match = Regex.Match(mailItem.Body, PgpHeaderPattern);

				_gpgCommandBar.GetButton("Verify").Enabled = (match.Value == PgpSignedHeader);
			}
			else
			{
				_gpgCommandBar.GetButton("Verify").Enabled = false;
			}
		}
		#endregion

		#region Inspector Logic
		/// <summary>
		/// The NewInspector event fires whenever a new inspector is displayed. We use
		/// this event to initialize button to mail item inspectors.
		/// The inspector logic handles the registration and execution of mailItem
		/// events (Open, MyClose and Write) to initialize, maintain and save the
		/// ribbon button states per mailItem.
		/// </summary>
		/// <param name="inspector">the new created Inspector</param>
		private void OutlookGnuPG_NewInspector(Outlook.Inspector inspector)
		{
			var mailItem = inspector.CurrentItem as Outlook.MailItem;
			if (mailItem != null)
			{
				WrapMailItem(inspector);
			}
		}

		/// <summary>
		/// Wrap mailItem object to managed mailItem events.
		/// </summary>
		/// <param name="explorer">the outlook explorer to manage</param>
		private void WrapMailItem(Outlook.Inspector inspector)
		{
			if (_wrappedObjects.ContainsValue(inspector))
				return;

			var wrappedMailItem = new MailItemInspector(inspector);
			wrappedMailItem.Dispose += MailItemInspector_Dispose;
			wrappedMailItem.MyClose += mailItem_Close;
			wrappedMailItem.Open += mailItem_Open;
			wrappedMailItem.Save += mailItem_Save;
			_wrappedObjects[wrappedMailItem.Id] = inspector;
		}

		/// <summary>
		/// WrapEvent to dispose the wrappedMailItem
		/// </summary>
		/// <param name="id">the UID of the wrappedMailItem</param>
		/// <param name="o">the wrapped mailItem object</param>
		private void MailItemInspector_Dispose(Guid id, object o)
		{
			var wrappedMailItem = o as MailItemInspector;
			wrappedMailItem.Dispose -= MailItemInspector_Dispose;
			wrappedMailItem.MyClose -= mailItem_Close;
			wrappedMailItem.Open -= mailItem_Open;
			wrappedMailItem.Save -= mailItem_Save;
			_wrappedObjects.Remove(id);
		}

		/// <summary>
		/// WrapperEvent fired when a mailItem is opened.
		/// This handler is designed to initialize the state of the compose button
		/// states (Sign/Encrypt) with recorded values, if present, or with default
		/// settings values.
		/// </summary>
		/// <param name="mailItem">the opened mailItem</param>
		/// <param name="cancel">False when the event occurs. If the event procedure sets this argument to True,
		/// the open operation is not completed and the inspector is not displayed.</param>
		void mailItem_Open(Outlook.MailItem mailItem, ref bool cancel)
		{
			if (mailItem == null)
				return;

			SetProperty(mailItem, "GnuPGSetting.Sign", false);
			SetProperty(mailItem, "GnuPGSetting.Encrypt", false);

			// New mail (Compose)
			if (!mailItem.Sent)
			{
				_ribbon.SignButton.Checked = _settings.AutoSign;
				_ribbon.EncryptButton.Checked = _settings.AutoEncrypt;

				if (mailItem.Body.Contains("\n** Message decrypted. Valid signature"))
				{
					_ribbon.SignButton.Checked = true;
					_ribbon.EncryptButton.Checked = true;
				}
				else if (mailItem.Body.Contains("\n** Message decrypted."))
				{
					_ribbon.EncryptButton.Checked = true;
				}

				SetProperty(mailItem, "GnuPGSetting.Sign", _ribbon.SignButton.Checked);
				SetProperty(mailItem, "GnuPGSetting.Encrypt", _ribbon.EncryptButton.Checked);

				_ribbon.InvalidateButtons();

				if (_ribbon.EncryptButton.Checked || _ribbon.SignButton.Checked)
					mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
			}
			else
			// Read mail
			{
				// Default: disable read-buttons
				_ribbon.VerifyButton.Enabled = false;

				// Look for PGP headers
				Match match = null;
				if (mailItem.Body != null)
					match = Regex.Match(mailItem.Body, PgpHeaderPattern);

				if (match != null && (_autoDecrypt || _settings.AutoDecrypt) && match.Value == PgpEncryptedHeader)
				{
					if (mailItem.BodyFormat != Outlook.OlBodyFormat.olFormatPlain)
					{
						var body = new StringBuilder(mailItem.Body);

						RemovePgpHeader(body);

						mailItem.Body = body.ToString();
					}

					_autoDecrypt = false;
					DecryptEmail(mailItem);
					// Update match again, in case decryption failed/cancelled.
					match = Regex.Match(mailItem.Body, PgpHeaderPattern);

					SetProperty(mailItem, "GnuPGSetting.Decrypted", true);
				}
				else if (match != null && _settings.AutoVerify && match.Value == PgpSignedHeader)
				{
					if (mailItem.BodyFormat != Outlook.OlBodyFormat.olFormatPlain)
					{
						var body = new StringBuilder(mailItem.Body);
						mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;

						RemovePgpHeader(body);

						mailItem.Body = body.ToString();
					}

					VerifyEmail(mailItem);
				}
				else
				{
					HandleMailWithoutPgpBody(mailItem);
				}

				if (match != null)
				{
					_ribbon.VerifyButton.Enabled = (match.Value == PgpSignedHeader);
				}
			}

			_ribbon.InvalidateButtons();
		}

	    private MailModel HandleMailWithoutPgpBody(Outlook.MailItem mailItem)
	    {
		    var sigHash = "sha1";
		    Outlook.Attachment sigMime = null;

		    dynamic contentType;
		    try
		    {
			    contentType =
				    mailItem.PropertyAccessor.GetProperty(
					    "http://schemas.microsoft.com/mapi/string/{00020386-0000-0000-C000-000000000046}/content-type/0x0000001F");
		    }
		    catch (Exception)
		    {
			    return null;
		    }
		    

		    Logger.Trace("MIME: Message content-type: " + (string) contentType);

		    if (((string) contentType).Contains(PgpSignatureMime))
		    {
			    // PGP MIM Signed message it looks like
			    //multipart/signed; micalg=pgp-sha1; protocol="application/pgp-signature"; boundary="Iq9CNK2GBN9g0PCsVJK4WdkEAR0887CbX"; charset="iso-8859-1"

			    Logger.Trace("MIME: Found " + PgpSignatureMime + ": " + contentType);

			    var sigMatch = Regex.Match((string) contentType, @"micalg=pgp-([^; ]*)");
			    sigHash = sigMatch.Groups[1].Value;

			    Logger.Trace("MIME: sigHash: " + sigHash);
		    }

		    var encryptedMime = FindPgpMime(mailItem, out sigMime);

		    if (encryptedMime == null && sigMime == null)
		    {
			    return null;
		    }
		    Logger.Trace("MIME: Calling HandlePgpMime");
		    var result = HandlePgpMime(mailItem, encryptedMime, sigMime, sigHash);

		    if (encryptedMime != null)
			    SetProperty(mailItem, "GnuPGSetting.Decrypted", true);
		    return result;
	    }

	    private static Outlook.Attachment FindPgpMime(Outlook.MailItem mailItem, out Outlook.Attachment sigMime)
	    {
		    var foundPgpMime = false;
		    Outlook.Attachment encryptedMime = null;
		    sigMime = null;
		    foreach (Outlook.Attachment attachment in mailItem.Attachments)
		    {
			    var mimeEncoding = attachment.PropertyAccessor.GetProperty("http://schemas.microsoft.com/mapi/proptag/0x370E001F");

			    Logger.Trace("MIME: Attachment type: " + mimeEncoding);

			    if (mimeEncoding == PgpEncryptedMime)
			    {
				    Logger.Trace("MIME: Found" + PgpEncryptedMime);
				    foundPgpMime = true;
			    }
			    else if (mimeEncoding == PgpSignatureMime)
			    {
				    Logger.Trace("MIME: Found" + PgpSignatureMime);
				    sigMime = attachment;
			    }
			    else if (foundPgpMime && encryptedMime == null && mimeEncoding == "application/octet-stream")
			    {
				    // Should be first attachment *after* PGP_MIME version identification

				    Logger.Trace("MIME: Found octet-stream following pgp-encrypted.");
				    encryptedMime = attachment;
			    }
		    }
		    return encryptedMime;
	    }

	    private static void RemovePgpHeader(StringBuilder body)
	    {
		    var indexes = new Stack<int>();
		    for (var cnt = 0; cnt < body.Length; cnt++)
		    {
			    if (body[cnt] > 127)
				    indexes.Push(cnt);
		    }

		    while (true)
		    {
			    if (indexes.Count == 0)
				    break;

			    int index = indexes.Pop();
			    body.Remove(index, 1);
		    }
	    }

	    public static void SetProperty(Outlook.MailItem mailItem, string name, object value)
		{
			var schema = "http://schemas.microsoft.com/mapi/string/{27EE45DA-1B2C-4E5B-B437-93E9820CC1FA}/" + name;
			
			mailItem.PropertyAccessor.SetProperty(schema, value);
		}

		public static object GetProperty(Outlook.MailItem mailItem, string name)
		{
			var schema = "http://schemas.microsoft.com/mapi/string/{27EE45DA-1B2C-4E5B-B437-93E9820CC1FA}/" + name;

			return mailItem.PropertyAccessor.GetProperty(schema);
			//return null;
		}

		/// <summary>
		/// Add "- " prefix as needed
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		string PgpClearDashEscapeAndQuoteEncode(string msg)
		{
			var writer = new StringWriter();
			using (var reader = new StringReader(msg))
			{
				while(true)
				{
					var line = EncodeQuotedPrintable(reader.ReadLine());
					if(line == null)
						break;

					if (line.Length > 0 && line[0] == '-')
						writer.Write("- ");

					writer.WriteLine(line);
				}
			}

			return writer.ToString();
		}

		Encoding GetEncodingFromMail(Outlook.MailItem mailItem)
		{
			var contentType = mailItem.PropertyAccessor.GetProperty("http://schemas.microsoft.com/mapi/string/{00020386-0000-0000-C000-000000000046}/content-type/0x0000001F");

			var match = Regex.Match(contentType, "charset=\"([^\"]+)\"");
			if (!match.Success)
				return Encoding.UTF8;

			return Encoding.GetEncoding(match.Groups[1].Value);
		}

		public static string EncodeQuotedPrintable(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			var builder = new StringBuilder();

			char[] bytes = value.ToCharArray();
			foreach (var v in bytes)
			{
				// The following are not required to be encoded:
				// - Tab (ASCII 9)
				// - Space (ASCII 32)
				// - Characters 33 to 126, except for the equal sign (61).

				if (v == '\n' || v == '\r')
					builder.Append(v);

				else if ((v == 9) || ((v >= 32) && (v <= 60)) || ((v >= 62) && (v <= 126)))
					builder.Append(v);

				else
				{
					builder.Append('=');
					builder.Append(((int)v).ToString("X2"));
				}
			}

			var lastChar = builder[builder.Length - 1];
			if (char.IsWhiteSpace(lastChar))
			{
				builder.Remove(builder.Length - 1, 1);
				builder.Append('=');
				builder.Append(((int)lastChar).ToString("X2"));
			}

			return builder.ToString();
		}

		MailModel HandlePgpMime(Outlook.MailItem mailItem, Outlook.Attachment encryptedMime, Outlook.Attachment sigMime, string sigHash = "sha1")
		{
			Logger.Trace("> HandlePgpMime");
			CryptoContext context = null;

			var cleartext = mailItem.Body;
			// 1. Decrypt attachement

			if (encryptedMime != null)
			{
				if (DecryptMime(mailItem, encryptedMime, ref context, ref cleartext))
				{
					return null;
				}
			}

			// 2. Verify signature
			string message = null;
			if (sigMime != null)
			{
				context = new CryptoContext(Passphrase);
				message = VerifySignature(mailItem, sigMime, sigHash, ref context);
			}

			if (context == null)
				return null;

			// Extract files from MIME data

			MailModel mailModel = null;
			var msg = new SharpMessage(cleartext);
			string body = mailItem.Body;

			var DecryptAndVerifyHeaderMessage = "** ";

			if (context.IsEncrypted)
				DecryptAndVerifyHeaderMessage += "Message decrypted. ";

			if (context.FailedIntegrityCheck)
				DecryptAndVerifyHeaderMessage += "Failed integrity check! ";

			if (context.IsSigned)
			{
				DecryptAndVerifyHeaderMessage += context.SignatureValidated ? "Valid" : "Invalid";
				DecryptAndVerifyHeaderMessage += " signature from \"" + context.SignedByUserId +
					"\" with KeyId " + context.SignedByKeyId + ".";
			}
			else
				DecryptAndVerifyHeaderMessage += "Message was unsigned.";

			DecryptAndVerifyHeaderMessage += "\n\n";

			if (mailItem.BodyFormat == Outlook.OlBodyFormat.olFormatPlain)
			{
				mailModel = new PlainMailModel
				{
					Body = DecryptAndVerifyHeaderMessage + msg.Body
				};
			}
			else if (mailItem.BodyFormat == Outlook.OlBodyFormat.olFormatHTML)
			{
				if (!msg.Body.TrimStart().ToLower().StartsWith("<html"))
				{
					body = DecryptAndVerifyHeaderMessage + msg.Body;
					body = System.Net.WebUtility.HtmlEncode(body);
					body = body.Replace("\n", "<br />");

					mailModel = new HtmlMailModel
					{
						Body = "<html><head></head><body>" + body + "</body></html>"
					};
				}
				else
				{
					// Find <body> tag and insert our message.

					var matches = Regex.Match(msg.Body, @"(<body[^<]*>)", RegexOptions.IgnoreCase);
					if (matches.Success)
					{
						var bodyTag = matches.Groups[1].Value;

						// Insert decryption message.
						mailModel = new HtmlMailModel
						{
							Body = msg.Body.Replace(
								bodyTag,
								bodyTag + DecryptAndVerifyHeaderMessage.Replace("\n", "<br />"))
						};
					}
					else
					{
						mailModel = new HtmlMailModel
						{
							Body = msg.Body
						};
					}
				}
			}
			else
			{
				// May cause mail item not to open correctly

				mailModel = new PlainMailModel
				{
					Body = msg.Body
				};
			}

			foreach (SharpAttachment mimeAttachment in msg.Attachments)
			{
				mimeAttachment.Stream.Position = 0;
				var fileName = mimeAttachment.Name;

				var tempFile = Path.Combine(Path.GetTempPath(), fileName);

				using (var fout = File.OpenWrite(tempFile))
				{
					mimeAttachment.Stream.CopyTo(fout);
				}

				if (fileName == "signature.asc")
				{
					var detachedsig = File.ReadAllText(tempFile);
					var clearsig = CreateClearSignatureFromDetachedSignature(mailItem, sigHash, detachedsig);
					var crypto = new PgpCrypto(context);
					message = VerifyClearSignature(ref context, crypto, clearsig);
				}

				mailModel.Attachments.Add(new Attachment
				{
					TempFile = tempFile, 
					AttachmentType = Outlook.OlAttachmentType.olByValue, 
					FileName = fileName
				});
			}
			mailModel.Body = message + mailModel.Body;
			return mailModel;
		}

	    private bool DecryptMime(Outlook.MailItem mailItem, Outlook.Attachment encryptedMime, ref CryptoContext context, ref string cleartext)
	    {
		    Logger.Trace("Decrypting cypher text.");

		    var tempfile = Path.GetTempFileName();
		    encryptedMime.SaveAsFile(tempfile);
		    var cyphertext = File.ReadAllBytes(tempfile);
		    File.Delete(tempfile);

		    var clearbytes = DecryptAndVerify(mailItem.To, cyphertext, out context);
		    if (clearbytes == null)
		    {
			    return true;
		    }

		    cleartext = _encoding.GetString(clearbytes);
		    return false;
	    }

	    private string VerifySignature(Outlook.MailItem mailItem, Outlook.Attachment sigMime, string sigHash, ref CryptoContext context)
	    {
		    var crypto = new PgpCrypto(context);

			Logger.Trace("Verify detached signature");

			var tempfile = Path.GetTempFileName();
			sigMime.SaveAsFile(tempfile);
			var detachedsig = File.ReadAllText(tempfile);
			File.Delete(tempfile);

			var clearsig = CreateClearSignatureFromDetachedSignature(mailItem, sigHash, detachedsig);
		    return VerifyClearSignature(ref context, crypto, clearsig);
	    }

	    private string VerifyClearSignature(ref CryptoContext context, PgpCrypto crypto, string clearsig)
	    {
		    string message;

		    try
		    {
			    var valid = crypto.VerifyClear(_encoding.GetBytes(clearsig));
			    context = crypto.Context;
			    message = valid ? "** Valid" : "** Invalid";

			    message += " signature from \"" + context.SignedByUserId +
			               "\" with KeyId " + context.SignedByKeyId + ".\n\n";
		    }
		    catch (PublicKeyNotFoundException ex)
		    {
			    Logger.Debug(ex.ToString());

			    message = "** Unable to verify signature, missing public key.\n\n";
		    }
		    catch (Exception ex)
		    {
			    Logger.Debug(ex.ToString());

			    Passphrase = null;

			    WriteErrorData("VerifyEmail", ex);
			    ShowErrorBox(ex.Message);
			    return null;
		    }

		    return message;
	    }

	    private string CreateClearSignatureFromDetachedSignature(Outlook.MailItem mailItem, string sigHash, string detachedsig)
	    {
			// Build up a clearsignature format for validation
			// the rules for are the same with the addition of two heaer fields.
			// Ultimately we need to get these fields out of email itself.
		    var encoding = GetEncodingFromMail(mailItem);

		    var clearsig = string.Format("-----" + PgpSignedHeader + "-----\r\nHash: {0}\r\n\r\n", sigHash);
		    clearsig += "Content-Type: text/plain; charset=" +
		                encoding.BodyName.ToUpper() +
		                "\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n";

		    try
		    {
				clearsig += PgpClearDashEscapeAndQuoteEncode(
				encoding.GetString(
					(byte[])mailItem.PropertyAccessor.GetProperty(
						"http://schemas.microsoft.com/mapi/string/{4E3A7680-B77A-11D0-9DA5-00C04FD65685}/Internet Charset Body/0x00000102")));
		    }
		    catch (Exception) { }
		    

		    clearsig += "\r\n" + detachedsig;

		    Logger.Trace(clearsig);
		    return clearsig;
	    }

	    private static void PrependMessageToMail(Outlook.MailItem mailItem, string message)
	    {
		    var mailType = mailItem.BodyFormat;
		    if (mailType == Outlook.OlBodyFormat.olFormatPlain)
		    {
			    mailItem.Body = message + mailItem.Body;
		    }
	    }

	    /// <summary>
		/// WrapperEvent fired when a mailItem is closed.
		/// </summary>
		/// <param name="mailItem">the mailItem to close</param>
		/// <param name="Cancel">False when the event occurs. If the event procedure sets this argument to True,
		/// the open operation is not completed and the inspector is not displayed.</param>
		void mailItem_Close(Outlook.MailItem mailItem, ref bool Cancel)
		{
			try
			{
				if (mailItem == null)
					return;

				// New mail (Compose)
				if (mailItem.Sent == false)
				{
					var toSave = false;
					var signProperpty = GetProperty(mailItem, "GnuPGSetting.Sign");
					if (signProperpty == null || (bool)signProperpty != _ribbon.SignButton.Checked)
					{
						toSave = true;
					}

					var encryptProperpty = GetProperty(mailItem, "GnuPGSetting.Decrypted");
					if (encryptProperpty == null || (bool)encryptProperpty != _ribbon.EncryptButton.Checked)
					{
						toSave = true;
					}
					if (toSave)
					{
#if DISABLED
		BoolEventArgs ev = e as BoolEventArgs;
		DialogResult res = MessageBox.Show("Do you want to save changes?",
										   "OutlookGnuPG",
										   MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
		if (res == DialogResult.Yes)
		{
		  // Must call mailItem.Write event handler (mailItem_Save) explicitely as it is not always called
		  // from the mailItem.Save() method. Mail is effectly saved only if a property changed.
		  mailItem_Save(sender, EventArgs.Empty);
		  mailItem.Save();
		}
		if (res == DialogResult.Cancel)
		{
		  ev.Value = true;
		}
#else
						// Invalidate the mailItem to force Outlook to ask to save the mailItem, hence calling
						// the mailItem_Save() handler to record the buttons state.
						// Note: the reason (button state property change) to save the mailItem is not necessairy obvious
						// to the user, certainly if nothing has been updated/changed by the user. If specific notification
						// is required see DISABLED code above. Beware, it might open 2 dialog boxes: the add-in custom and
						// the regular Outlook save confirmation.
						mailItem.Subject = mailItem.Subject;
					}
#endif
				}
				else
				{
					var signProperty = GetProperty(mailItem, "GnuPGSetting.Decrypted");
					if (signProperty != null && (bool)signProperty)
					{
						mailItem.Close(Outlook.OlInspectorClose.olDiscard);
					}
				}
			}
			catch
			{
				// Ignore random COM errors
			}
		}

		/// <summary>
		/// WrapperEvent fired when a mailItem is saved.
		/// This handler is designed to record the compose button state (Sign/Encrypt)
		/// associated to this mailItem.
		/// </summary>
		/// <param name="mailItem">the mailItem to save</param>
		/// <param name="Cancel">False when the event occurs. If the event procedure sets this argument to True,
		/// the open operation is not completed and the inspector is not displayed.</param>
		void mailItem_Save(Outlook.MailItem mailItem, ref bool Cancel)
		{
			if (mailItem == null || mailItem.Sent)
				return;

			// New mail (Compose); Record compose button states.
			SetProperty(mailItem, "GnuPGSetting.Sign", _ribbon.SignButton.Checked);
			SetProperty(mailItem, "GnuPGSetting.Encrypt", _ribbon.EncryptButton.Checked);
		}
		#endregion

		#region CommandBar Logic
		private void AddGnuPGCommandBar(Outlook.Explorer activeExplorer)
		{
			if (_gpgCommandBar != null)
				return;
			try
			{
				_gpgCommandBar = new GnuPGCommandBar(activeExplorer);
				_gpgCommandBar.Remove();
				_gpgCommandBar.Add();
				_gpgCommandBar.GetButton("Verify").Click += VerifyButton_Click;
				_gpgCommandBar.GetButton("Settings").Click += SettingsButton_Click;
				_gpgCommandBar.GetButton("About").Click += AboutButton_Click;
				_gpgCommandBar.RestorePosition(_settings);
			}
			catch (Exception ex)
			{
				WriteErrorData("AddGnuPGCommandBar", ex);
				ShowErrorBox(ex.Message);
			}
		}

		private void VerifyButton_Click(Office.CommandBarButton ctrl, ref bool cancelDefault)
		{
			// Get the selected item in Outlook and determine its type.
			var outlookSelection = Application.ActiveExplorer().Selection;
			if (outlookSelection.Count <= 0)
				return;

			object selectedItem = outlookSelection[1];
			var mailItem = selectedItem as Outlook.MailItem;

			if (mailItem == null)
			{
				ShowErrorBox("OutlookGnuPG can only verify mails.");

				return;
			}

			VerifyEmail(mailItem);
		}

		private void AboutButton_Click(Office.CommandBarButton ctrl, ref bool cancelDefault)
		{
			Globals.OutlookPrivacyPlugin.About();
		}

		private void SettingsButton_Click(Office.CommandBarButton ctrl, ref bool cancelDefault)
		{
			Globals.OutlookPrivacyPlugin.Settings();
		}
		#endregion

		public string GetSMTPAddress(Outlook.MailItem mailItem)
		{
			if (mailItem.SendUsingAccount != null &&
				!string.IsNullOrWhiteSpace(mailItem.SendUsingAccount.SmtpAddress))
				return mailItem.SendUsingAccount.SmtpAddress;

			if (!string.IsNullOrWhiteSpace(mailItem.SenderEmailAddress) &&
				!mailItem.SenderEmailType.ToUpper().Equals("EX"))
				return mailItem.SenderEmailAddress;

			// This can be x509 for exchange accounts
			if (mailItem.SendUsingAccount != null &&
				mailItem.SendUsingAccount.AccountType != 0 && /* Verify not exchange account */
				mailItem.SendUsingAccount.CurrentUser != null &&
				mailItem.SendUsingAccount.CurrentUser.Address != null)
				return mailItem.SendUsingAccount.CurrentUser.Address;

			var oOutlook = new Outlook.Application();
			var oNs = oOutlook.GetNamespace("MAPI");

			if (mailItem.SenderEmailType.ToUpper().Equals("EX"))
			{
				var recipient = oNs.CreateRecipient(mailItem.SenderName);
				var exchangeUser = recipient.AddressEntry.GetExchangeUser();
				return exchangeUser.PrimarySmtpAddress;
			}

			throw new Exception("Error, unable to determine senders address.");
		}

		#region Send Logic
		private void Application_ItemSend(object item, ref bool cancel)
		{
			var mailItem = item as Outlook.MailItem;

			if (mailItem == null)
				return;

			var currentRibbon = _ribbon;
			if (currentRibbon == null)
				return;

			var mail = mailItem.Body;
			var needToEncrypt = currentRibbon.EncryptButton.Checked;
			var needToSign = currentRibbon.SignButton.Checked;

			// Early out when we don't need to sign/encrypt
			if (!needToEncrypt && !needToSign)
				return;

			// Cancel by default
			cancel = true;

			if (mailItem.BodyFormat != Outlook.OlBodyFormat.olFormatPlain)
			{
				ShowErrorBox("OutlookGnuPG can only sign/encrypt plain text mails. Please change the format, or disable signing/encrypting for this mail.");

				// Prevent sending the mail
				return;
			}

			try
			{
				IList<string> recipients = new List<string>();

				if (needToEncrypt)
				{
					if (GetRecipientsForEncryption(mailItem, recipients))
					{
						// Prevent sending the mail
						return;
					}
				}

				var attachments = new List<Attachment>();

				if (needToSign && needToEncrypt)
				{
					mail = SignAndEncryptEmail(mail, GetSMTPAddress(mailItem), recipients);
					if (mail == null)
						return;

					SignAndEncryptAttachments(mailItem, recipients, attachments);
				}
				else if (needToSign)
				{
					mail = SignEmail(mail, GetSMTPAddress(mailItem));
					if (mail == null)
						return;
				}
				else if (needToEncrypt)
				{
					mail = EncryptEmail(mail, recipients);
					if (mail == null)
						return;

					EncryptAttachments(mailItem, recipients, attachments);
				}

				foreach (var attachment in attachments)
				{
					mailItem.Attachments.Add(attachment.TempFile, attachment.AttachmentType, 1, attachment.DisplayName);
				}
			}
			catch (Exception ex)
			{
				Passphrase = null;

				if (ex.Message.ToLower().StartsWith("checksum"))
				{
					ShowErrorBox("Incorrect passphrase possibly entered.");
					return;
				}

				WriteErrorData("Application_ItemSend", ex);
				ShowErrorBox(ex.Message);

				// Cancel sending
				return;
			}

			cancel = false;
			mailItem.Body = mail;
		}

	    private bool GetRecipientsForEncryption(Outlook.MailItem mailItem, IList<string> recipients)
	    {
		    var mailRecipients = new List<string>();
		    foreach (Outlook.Recipient mailRecipient in mailItem.Recipients)
			    mailRecipients.Add(GetAddressCN(mailRecipient.Address));

			// Popup UI to select the encryption targets 
		    var recipientDialog = new Recipient(mailRecipients); // Passing in the first addres, maybe it matches
		    recipientDialog.TopMost = true;
		    var recipientResult = recipientDialog.ShowDialog();

		    if (recipientResult != DialogResult.OK)
		    {
			    // The user closed the recipient dialog, prevent sending the mail
			    return true;
		    }

		    foreach (var r in recipientDialog.SelectedKeys)
			    recipients.Add(r);

		    recipientDialog.Close();

		    if (recipients.Count == 0)
		    {
			    ShowErrorBox("OutlookGnuPG needs a recipient when encrypting. No keys were detected/selected.");

			    // Prevent sending the mail
			    return true;
		    }

		    recipients.Add(GetSMTPAddress(mailItem));
		    return false;
	    }

	    private void SignAndEncryptAttachments(Outlook.MailItem mailItem, IList<string> recipients, List<Attachment> attachments)
	    {
			var mailAttachments = CreateAttachmentListFromMailItem(mailItem);

		    foreach (var attachment in mailAttachments)
		    {
			    var tempAttachment = CreateTempAttachment(attachment);

			    // Encrypt file
			    var cleartext = File.ReadAllBytes(tempAttachment.TempFile);
			    var cyphertext = SignAndEncryptAttachment(cleartext, GetSMTPAddress(mailItem), recipients);
			    File.WriteAllText(tempAttachment.TempFile, cyphertext);

			    tempAttachment.Encrypted = true;
			    attachments.Add(tempAttachment);
		    }
	    }

	    private static Attachment CreateTempAttachment(Outlook.Attachment attachment)
	    {
		    var tempAttachment = new Attachment
		    {
			    TempFile = Path.GetTempPath(),
			    FileName = attachment.FileName,
			    DisplayName = attachment.DisplayName,
			    AttachmentType = attachment.Type
		    };

		    tempAttachment.TempFile = Path.Combine(tempAttachment.TempFile, tempAttachment.FileName);
		    tempAttachment.TempFile = tempAttachment.TempFile + ".pgp";
		    attachment.SaveAsFile(tempAttachment.TempFile);
		    attachment.Delete();
		    return tempAttachment;
	    }

	    private void EncryptAttachments(Outlook.MailItem mailItem, IList<string> recipients, List<Attachment> attachments)
	    {
		    var mailAttachments = CreateAttachmentListFromMailItem(mailItem);

		    foreach (var attachment in mailAttachments)
		    {
				var tempAttachment = CreateTempAttachment(attachment);

			    // Encrypt file
			    var cleartext = File.ReadAllBytes(tempAttachment.TempFile);
			    var cyphertext = EncryptEmail(cleartext, recipients);
			    File.WriteAllText(tempAttachment.TempFile, cyphertext);

			    tempAttachment.Encrypted = true;
			    attachments.Add(tempAttachment);
		    }
	    }

	    private static List<Outlook.Attachment> CreateAttachmentListFromMailItem(Outlook.MailItem mailItem)
	    {
		    return mailItem.Attachments.Cast<Outlook.Attachment>().ToList();
	    }

	    private string SignEmail(string data, string key)
		{
			try
			{
				if (!PromptForPasswordAndKey())
					return null;

				var context = new CryptoContext(Passphrase);
				var crypto = new PgpCrypto(context);
				var headers = new Dictionary<string, string>();
				headers["Version"] = MailHeaderVersion;

				return crypto.SignClear(data, key, _encoding, headers);
			}
			catch (CryptoException ex)
			{
				Passphrase = null;

				WriteErrorData("SignEmail", ex);
				ShowErrorBox(ex.Message);

				return null;
			}
		}

		private string EncryptEmail(string mail, IList<string> recipients)
		{
			return EncryptEmail(_encoding.GetBytes(mail), recipients);
		}

		private string EncryptEmail(byte[] data, IList<string> recipients)
		{
			try
			{
				var context = new CryptoContext();
				var crypto = new PgpCrypto(context);
				var headers = GetEncryptedMailHeaders();

				return crypto.Encrypt(data, recipients, headers);
			}
			catch (Exception e)
			{
				Passphrase = null;

				WriteErrorData("EncryptEmail", e);
				ShowErrorBox(e.Message);

				return null;
			}
		}

	    private Dictionary<string, string> GetEncryptedMailHeaders()
	    {
		    var headers = new Dictionary<string, string>();
		    headers["Version"] = MailHeaderVersion;
		    headers["Charset"] = _encoding.WebName;
		    return headers;
	    }

	    private string SignAndEncryptAttachment(byte[] data, string key, IList<string> recipients)
		{
			try
			{
				if (!PromptForPasswordAndKey())
					return null;

				var context = new CryptoContext(Passphrase);
				var crypto = new PgpCrypto(context);
				var headers = GetEncryptedMailHeaders();

				return crypto.SignAndEncryptBinary(data, key, recipients, headers);
			}
			catch (Exception ex)
			{
				Passphrase = null;

				ShowErrorBox(ex.Message);

				throw;
			}
		}

		private string SignAndEncryptEmail(string data, string key, IList<string> recipients)
		{
			return SignAndEncryptEmail(_encoding.GetBytes(data), key, recipients);
		}

		private string SignAndEncryptEmail(byte[] data, string key, IList<string> recipients)
		{
			try
			{
				if (!PromptForPasswordAndKey())
					return null;

				var context = new CryptoContext(Passphrase);
				var crypto = new PgpCrypto(context);
				var headers = GetEncryptedMailHeaders();

				return crypto.SignAndEncryptText(data, key, recipients, headers);
			}
			catch (Exception ex)
			{
				Passphrase = null;

				WriteErrorData("SignAndEncryptEmail", ex);
				ShowErrorBox(ex.Message);

				throw;
			}
		}
		#endregion

		#region Receive Logic
		internal void VerifyEmail(Outlook.MailItem mailItem)
		{
			var mail = mailItem.Body;

			if (Regex.IsMatch(mailItem.Body, PgpSignedHeader) == false)
			{
				ShowErrorBox("Outlook Privacy cannot help here; mail is not signed");

				return;
			}

			var context = new CryptoContext(Passphrase);
			var crypto = new PgpCrypto(context);

			try
			{
				var valid = crypto.Verify(_encoding.GetBytes(mail));
				context = crypto.Context;
				var message = valid ? "** Valid" : "** Invalid";

				message += " signature from \"" + context.SignedByUserId +
						"\" with KeyId " + context.SignedByKeyId + ".\n\n";

				PrependMessageToMail(mailItem, message);
			}
			catch (PublicKeyNotFoundException ex)
			{
				var message = "** Unable to verify signature, missing public key.\n\n";

				PrependMessageToMail(mailItem, message);
			}
			catch (Exception ex)
			{
				Passphrase = null;

				WriteErrorData("VerifyEmail", ex);
				ShowErrorBox(ex.Message);
			}
		}

		void WriteErrorData(string msg, Exception ex)
		{
			try
			{
				var logFile = Environment.GetEnvironmentVariable("APPDATA");
				logFile = Path.Combine(logFile, "OutlookPrivacyPlugin");

				if (!Directory.Exists(logFile))
					Directory.CreateDirectory(logFile);

				logFile = Path.Combine(logFile, "log.txt");

				File.AppendAllText(logFile, "\n-------- " +
					DateTime.Now +
					" --------\n" +
					msg +
					"\n\n" +
					ex);
			}
			catch
			{
			}
		}

		public MailModel DecryptEmail(Outlook.MailItem mailItem)
		{
			if (mailItem.Body == null || Regex.IsMatch(mailItem.Body, PgpEncryptedHeader) == false)
			{
				return HandleMailWithoutPgpBody(mailItem);
			}

			MailModel mailModel;

			// Sometimes messages could contain multiple message blocks.  In that case just use the 
			// very first one.
			var firstPgpBlock = GetFirstPgpBlock(mailItem);

			var encoding = GetEncoding(firstPgpBlock);

			CryptoContext context;
			var cleardata = DecryptAndVerify(mailItem.To, Encoding.ASCII.GetBytes(firstPgpBlock), out context);
			if (cleardata == null) 
				return null;

			if (mailItem.BodyFormat == Outlook.OlBodyFormat.olFormatHTML)
			{
				// Don't HMTL encode or we will encode emails already in HTML format.
				// Office has a safe html module they use to prevent security issues.
				// Not encoding here should be no worse then reading a standard HTML
				// email.
				var html = _decryptAndVerifyHeaderMessage.Replace("<", "&lt;").Replace(">", "&gt;") + encoding.GetString(cleardata);
				html = html.Replace("\n", "<br/>");
				html = "<html><body>" + html + "</body></html>";
				mailModel = new HtmlMailModel
				{
					Body = html
				};
			}
			else
			{
				var mailText = _decryptAndVerifyHeaderMessage + encoding.GetString(cleardata);
				mailModel = new PlainMailModel
				{
					Body = mailText
				};
			}

			// Decrypt all attachments
			var mailAttachments = CreateAttachmentListFromMailItem(mailItem);

			var attachments = new List<Attachment>();

			foreach (var attachment in mailAttachments)
			{
				var tempAttachment = new Attachment();

				// content id

				if (attachment.FileName.StartsWith("Attachment") && attachment.FileName.EndsWith(".pgp"))
				{
					var property = attachment.PropertyAccessor.GetProperty("http://schemas.microsoft.com/mapi/proptag/0x3712001F");
					tempAttachment.FileName = property.ToString();

					if (tempAttachment.FileName.Contains('@'))
					{
						tempAttachment.FileName = tempAttachment.FileName.Substring(0, tempAttachment.FileName.IndexOf('@'));
					}

					tempAttachment.TempFile = Path.GetTempPath();
					tempAttachment.AttachmentType = attachment.Type;

					tempAttachment.TempFile = Path.Combine(tempAttachment.TempFile, tempAttachment.FileName);

					attachment.SaveAsFile(tempAttachment.TempFile);

					TryDecryptAndAddAttachment(mailItem, tempAttachment, attachments);
				}
					//else if (attachment.FileName == "PGPexch.htm.pgp")
					//{
					//	// This is the HTML email message.

					//	var TempFile = Path.GetTempFileName();
					//	attachment.SaveAsFile(TempFile);

					//	// Decrypt file
					//	var cyphertext = File.ReadAllBytes(TempFile);
					//	File.Delete(TempFile);

					//	try
					//	{
					//		var plaintext = DecryptAndVerify(mailItem.To, cyphertext);

					//		mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;
					//		mailItem.HTMLBody = _encoding.GetString(plaintext);
					//	}
					//	catch
					//	{
					//		// Odd!
					//	}
					//}
				else
				{
					tempAttachment.FileName = Regex.Replace(attachment.FileName, EncryptionExtension, "");
					tempAttachment.DisplayName = Regex.Replace(attachment.DisplayName, EncryptionExtension, ""); ;
					tempAttachment.TempFile = Path.GetTempPath();
					tempAttachment.AttachmentType = attachment.Type;

					tempAttachment.TempFile = Path.Combine(tempAttachment.TempFile, tempAttachment.FileName);

					attachment.SaveAsFile(tempAttachment.TempFile);

					TryDecryptAndAddAttachment(mailItem, tempAttachment, attachments);
				}
			}

			mailModel.Attachments = attachments;
			return mailModel;
		}

	    private void TryDecryptAndAddAttachment(Outlook.MailItem mailItem, Attachment tempAttachment, List<Attachment> attachments)
	    {
		    var cyphertext = File.ReadAllBytes(tempAttachment.TempFile);
		    File.Delete(tempAttachment.TempFile);

		    try
		    {
			    CryptoContext context;
			    var plaintext = DecryptAndVerify(mailItem.To, cyphertext, out context);

			    File.WriteAllBytes(tempAttachment.TempFile, plaintext);

			    attachments.Add(tempAttachment);
		    }
		    catch
		    {
			    // Assume attachment wasn't encrypted
		    }
	    }

	    private static string GetFirstPgpBlock(Outlook.MailItem mailItem)
	    {
		    var firstPgpBlock = mailItem.Body;
		    var endMessagePosition = firstPgpBlock.IndexOf(EndPgpMessageGuard) + EndPgpMessageGuard.Length;
		    if (endMessagePosition != -1)
		    {
			    firstPgpBlock = firstPgpBlock.Substring(0, endMessagePosition);
		    }
		    return firstPgpBlock;
	    }

	    private static Encoding GetEncoding(string firstPgpBlock)
	    {
		    string charset = null;
		    try
		    {
			    charset = Regex.Match(firstPgpBlock, @"Charset:\s+([^\s\r\n]+)").Groups[1].Value;
		    }
		    catch
		    {
		    }

		    // Set default encoding if charset was missing from 
		    // message.
		    if (string.IsNullOrWhiteSpace(charset))
		    {
			    charset = "UTF-8";
		    }

		    var encoding = Encoding.GetEncoding(charset);
		    return encoding;
	    }

	    #endregion

		bool PromptForPasswordAndKey()
		{
			if (Passphrase != null)
				return true;

			// Popup UI to select the passphrase and private key.
			var passphraseDialog = new Passphrase(_settings.DefaultKey, "Key");
			passphraseDialog.TopMost = true;
			var passphraseResult = passphraseDialog.ShowDialog();
			if (passphraseResult != DialogResult.OK)
			{
				// The user closed the passphrase dialog, prevent sending the mail
				return false;
			}

			Passphrase = passphraseDialog.EnteredPassphrase.ToCharArray();
			passphraseDialog.Close();

			return true;
		}

		string _decryptAndVerifyHeaderMessage = "";

		byte[] DecryptAndVerify(string to, byte[] data, out CryptoContext outContext)
		{
			_decryptAndVerifyHeaderMessage = "";
			outContext = null;

			if (!PromptForPasswordAndKey())
				return null;

			var context = new CryptoContext(Passphrase);
			var crypto = new PgpCrypto(context);

			try
			{
				var cleartext = crypto.DecryptAndVerify(data, _settings.IgnoreIntegrityCheck);
				context = crypto.Context;

				// NOT USED YET.
				
				//DecryptAndVerifyHeaderMessage = "** ";

				//if (Context.IsEncrypted)
				//	DecryptAndVerifyHeaderMessage += "Message decrypted. ";

				//if (Context.IsSigned && Context.SignatureValidated)
				//{
				//	DecryptAndVerifyHeaderMessage += "Valid signature from \"" + Context.SignedByUserId +
				//		"\" with KeyId " + Context.SignedByKeyId;
				//}
				//else if (Context.IsSigned)
				//{
				//	DecryptAndVerifyHeaderMessage += "Invalid signature from \"" + Context.SignedByUserId +
				//		"\" with KeyId " + Context.SignedByKeyId + ".";
				//}
				//else
				//	DecryptAndVerifyHeaderMessage += "Message was unsigned.";

				//DecryptAndVerifyHeaderMessage += "\n\n";

				outContext = context;
				return cleartext;
			}
			catch (Exception e)
			{
				Passphrase = null;
				WriteErrorData("DecryptAndVerify", e);

				ShowErrorBox(e.Message.ToLower().StartsWith("checksum") ? "Incorrect passphrase possibly entered." : e.Message);
			}

			return null;
		}

	    private static void ShowErrorBox(string message)
	    {
		    MessageBox.Show(
			    message,
			    "Outlook Privacy Error",
			    MessageBoxButtons.OK,
			    MessageBoxIcon.Error);
	    }

	    #region General Logic
		internal void About()
		{
			var aboutBox = new About();
			aboutBox.TopMost = true;
			aboutBox.ShowDialog();
		}

		internal void Settings()
		{
			var settingsBox = new Settings(_settings);
			settingsBox.TopMost = true;
			var result = settingsBox.ShowDialog();

			if (result != DialogResult.OK)
				return;

			_settings.Encrypt2Self = settingsBox.Encrypt2Self;
			_settings.AutoDecrypt = settingsBox.AutoDecrypt;
			_settings.AutoVerify = settingsBox.AutoVerify;
			_settings.AutoEncrypt = settingsBox.AutoEncrypt;
			_settings.AutoSign = settingsBox.AutoSign;
			_settings.DefaultKey = settingsBox.DefaultKey;
			_settings.DefaultDomain = settingsBox.DefaultDomain;
			_settings.Default2PlainFormat = settingsBox.Default2PlainFormat;
			_settings.IgnoreIntegrityCheck = settingsBox.IgnoreIntegrityCheck;
			_settings.Save();
		}

		#endregion

		#region Key Management

		public IList<GnuKey> GetKeysForEncryption()
		{
			var crypto = new PgpCrypto(new CryptoContext());
			var keys = new List<GnuKey>();

			foreach (var key in crypto.GetPublicKeyUserIdsForEncryption())
			{
				AddGnuKeyIfUsable(key, keys);
			}

			return keys;
		}

	    public IList<GnuKey> GetKeysForSigning()
		{
			var crypto = new PgpCrypto(new CryptoContext());
			var keys = new List<GnuKey>();

			foreach (var key in crypto.GetPublicKeyUserIdsForSign())
			{
				AddGnuKeyIfUsable(key, keys);
			}

			return keys;
		}

		private static void AddGnuKeyIfUsable(string key, List<GnuKey> keys)
		{
			var match = Regex.Match(key, @"<(.*)>");
			if (!match.Success)
				return;

			var k = new GnuKey
			{
				Key = match.Groups[1].Value,
				KeyDisplay = key
			};

			keys.Add(k);
		}

		string GetAddressCN(string AddressX400)
		{
			char[] delimiters = { '/' };
			var splitAddress = AddressX400.Split(delimiters);
			foreach (var addressPart in splitAddress)
			{
				if (addressPart.StartsWith("cn=", true, null) && !Regex.IsMatch(addressPart, "ecipient", RegexOptions.IgnoreCase))
				{
					var address = Regex.Replace(addressPart, "cn=", string.Empty, RegexOptions.IgnoreCase);
					if (string.IsNullOrEmpty(_settings.DefaultDomain))
					{
						return address;
					}
					address += "@" + _settings.DefaultDomain;
					address = address.Replace("@@", "@");
					return address;
				}
			}
			return AddressX400;
		}

		#endregion
	}
}

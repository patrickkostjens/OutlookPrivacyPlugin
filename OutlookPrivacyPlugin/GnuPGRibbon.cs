// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any
// later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Office = Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;

using Deja.Crypto.BcPgp;
using System.Text.RegularExpressions;

using NLog;

namespace OutlookPrivacyPlugin
{
	public class ButtonStateData
	{
		public bool SignButton = false;
		public bool EncryptButton = false;

		public ButtonStateData(bool sign, bool encrypt)
		{
			SignButton = sign;
			EncryptButton = encrypt;
		}
	}

	[ComVisible(true)]
	public class GnuPGRibbon : Office.IRibbonExtensibility
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private Office.IRibbonUI ribbon;

		public GnuPGToggleButton SignButton;
		public GnuPGToggleButton EncryptButton;
		public GnuPGToggleButton VerifyButton;
		public GnuPGToggleButton AttachPublicKeyButton;

		public Dictionary<string, ButtonStateData> ButtonState = new Dictionary<string, ButtonStateData>();

		private Dictionary<string, GnuPGToggleButton> Buttons = new Dictionary<string, GnuPGToggleButton>();

		public GnuPGRibbon()
		{
			SignButton = new GnuPGToggleButton("signButton");
			EncryptButton = new GnuPGToggleButton("encryptButton");
			VerifyButton = new GnuPGToggleButton("verifyButton");
			AttachPublicKeyButton = new GnuPGToggleButton("attachPublicKeyButton");

			Buttons.Add(SignButton.Id, SignButton);
			Buttons.Add(EncryptButton.Id, EncryptButton);
			Buttons.Add(VerifyButton.Id, VerifyButton);
			Buttons.Add(AttachPublicKeyButton.Id, AttachPublicKeyButton);
		}

		#region IRibbonExtensibility Members

		public string GetCustomUI(string ribbonID)
		{
			String ui = null;
			// Examine the ribbonID to see if the current item
			// is a Mail inspector.
			if (ribbonID == "Microsoft.Outlook.Mail.Read")
			{
				// Retrieve the customized Ribbon XML.
				ui = GetResourceText("OutlookPrivacyPlugin.GnuPGRibbonRead.xml");
			}
			if (ribbonID == "Microsoft.Outlook.Mail.Compose")
			{
				// Retrieve the customized Ribbon XML.
				ui = GetResourceText("OutlookPrivacyPlugin.GnuPGRibbonCompose.xml");
			}
			return ui;
		}

		#endregion

		internal void UpdateButtons(Properties.Settings settings)
		{
			// Compose Mail
			EncryptButton.Checked = settings.AutoEncrypt;
			SignButton.Checked = settings.AutoSign;
			AttachPublicKeyButton.Checked = false;

			// Read Mail
			VerifyButton.Checked = settings.AutoVerify;
		}

		internal void InvalidateButtons()
		{
			if (ribbon == null)
				return;

			ribbon.InvalidateControl(SignButton.Id);
			ribbon.InvalidateControl(EncryptButton.Id);
			ribbon.InvalidateControl(VerifyButton.Id);
			ribbon.InvalidateControl(AttachPublicKeyButton.Id);
		}

		#region Ribbon Callbacks

		public void OnLoad(Office.IRibbonUI ribbonUI)
		{
			this.ribbon = ribbonUI;
		}

		public void OnEncryptButton(Office.IRibbonControl control, bool isPressed)
		{
			logger.Trace("OnEncryptButton("+control.Id+", "+isPressed+")");

			Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;
			if (mailItem == null)
				logger.Trace("OnEncryptButton: mailItem == null");

			if (isPressed == true)
			{
				if (mailItem != null)
				{
					var settings = new Properties.Settings();
					if (settings.Default2PlainFormat)
					{
						string body = mailItem.Body;
						mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
						mailItem.Body = body;
					}
				}
			}

			OutlookPrivacyPlugin.SetProperty(mailItem, "GnuPGSetting.Encrypt", isPressed);
			EncryptButton.Checked = isPressed;
			ribbon.InvalidateControl(EncryptButton.Id);
		}

		public void OnSignButton(Office.IRibbonControl control, bool isPressed)
		{
			Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;

			if (isPressed == true)
			{
				if (mailItem != null)
				{
					var settings = new Properties.Settings();
					if (settings.Default2PlainFormat)
					{
						string body = mailItem.Body;
						mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
						mailItem.Body = body;
					}
				}
			}

			OutlookPrivacyPlugin.SetProperty(mailItem, "GnuPGSetting.Sign", isPressed);
			SignButton.Checked = isPressed;
			ribbon.InvalidateControl(SignButton.Id);
		}

		public void OnVerifyButton(Office.IRibbonControl control)
		{
			Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;
			if (mailItem != null)
				Globals.OutlookPrivacyPlugin.VerifyEmail(mailItem);
		}

		public void OnAttachPublicKeyButton(Office.IRibbonControl control)
		{
			Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;
			if (mailItem == null)
			{
				// TODO - Generate a log message
				MessageBox.Show("Error, mailItem was null.");
				return;
			}
			
			var smtp = Globals.OutlookPrivacyPlugin.GetSMTPAddress(mailItem);
			
			var crypto = new Deja.Crypto.BcPgp.PgpCrypto(new Deja.Crypto.BcPgp.CryptoContext());
			var publicKey = crypto.PublicKey(smtp, new Dictionary<string, string>());

			if(publicKey == null)
			{
				// TODO - Generate log message
				MessageBox.Show("Error, unable to find public key to attach.");
				return;
			}

			var tempFile = Path.Combine(Path.GetTempPath(), "public_key.gpg");
			File.WriteAllText(tempFile, publicKey);

			try
			{
				mailItem.Attachments.Add(
					tempFile, 
					Outlook.OlAttachmentType.olByValue, 
					1, 
					"public_key.gpg");
				
				mailItem.Save();
			}
			finally
			{
				File.Delete(tempFile);
			}
		}

		public void OnSettingsButtonRead(Office.IRibbonControl control)
		{
			Globals.OutlookPrivacyPlugin.Settings();
		}

		public void OnSettingsButtonNew(Office.IRibbonControl control)
		{
			Globals.OutlookPrivacyPlugin.Settings();

			// Force an update of button state:
			ribbon.InvalidateControl(SignButton.Id);
			ribbon.InvalidateControl(EncryptButton.Id);
		}

		public void OnAboutButton(Office.IRibbonControl control)
		{
			Globals.OutlookPrivacyPlugin.About();
		}

		public stdole.IPictureDisp
		  GetCustomImage(Office.IRibbonControl control)
		{
			stdole.IPictureDisp pictureDisp = null;
			switch (control.Id)
			{
				case "settingsButtonNew":
				case "settingsButtonRead":
					pictureDisp = ImageConverter.Convert(global::OutlookPrivacyPlugin.Properties.Resources.database_gear);
					break;
				case "aboutButtonNew":
				case "aboutButtonRead":
					pictureDisp = ImageConverter.Convert(global::OutlookPrivacyPlugin.Properties.Resources.Logo);
					break;
				case "attachPublicKeyButton":
					pictureDisp = ImageConverter.Convert(global::OutlookPrivacyPlugin.Properties.Resources.attach);
					break;
				default:
					if ((control.Id == EncryptButton.Id))
						pictureDisp = ImageConverter.Convert(global::OutlookPrivacyPlugin.Properties.Resources.lock_edit);
					if ((control.Id == SignButton.Id) || (control.Id == VerifyButton.Id))
						pictureDisp = ImageConverter.Convert(global::OutlookPrivacyPlugin.Properties.Resources.link_edit);
					break;
			}
			return pictureDisp;
		}

		public bool GetPressed(Office.IRibbonControl control)
		{
			logger.Trace("GetPressed("+control.Id+")");

			if (control.Id == SignButton.Id)
			{
				Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;
				return (bool)OutlookPrivacyPlugin.GetProperty(mailItem, "GnuPGSetting.Sign");
			}
			else if (control.Id == EncryptButton.Id)
			{
				Outlook.MailItem mailItem = ((Outlook.Inspector)control.Context).CurrentItem as Outlook.MailItem;
				return (bool)OutlookPrivacyPlugin.GetProperty(mailItem, "GnuPGSetting.Encrypt");
			}
			else
				logger.Trace("GetPressed: Button did not match encrypt or sign!");

			if (Buttons.ContainsKey(control.Id))
				return Buttons[control.Id].Checked;

			return false;
		}

		public bool GetEnabled(Office.IRibbonControl control)
		{
			if (Buttons.ContainsKey(control.Id))
				return Buttons[control.Id].Enabled;
			return false;
		}

		#endregion

		#region Helpers

		private static string GetResourceText(string resourceName)
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			string[] resourceNames = asm.GetManifestResourceNames();
			for (int i = 0; i < resourceNames.Length; ++i)
			{
				if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
				{
					using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
					{
						if (resourceReader != null)
						{
							return resourceReader.ReadToEnd();
						}
					}
				}
			}
			return null;
		}

		#endregion
	}

	internal class ImageConverter : System.Windows.Forms.AxHost
	{
		private ImageConverter()
			: base(null)
		{
		}
		public static stdole.IPictureDisp Convert(System.Drawing.Image image)
		{
			return (stdole.IPictureDisp)AxHost.GetIPictureDispFromPicture(image);
		}
	}

	public class GnuPGToggleButton
	{
		private bool m_Checked = false;
		public bool Checked
		{
			get { return m_Checked; }
			set { m_Checked = value; }
		}

		private bool m_Enabled = true;
		public bool Enabled
		{
			get { return m_Enabled; }
			set { m_Enabled = value; }
		}

		private string m_ControlId = "unknown";
		public string Id
		{
			get { return m_ControlId; }
			set { m_ControlId = value; }
		}

		public GnuPGToggleButton(string controlId)
		{
			Id = controlId;
		}
	}
}

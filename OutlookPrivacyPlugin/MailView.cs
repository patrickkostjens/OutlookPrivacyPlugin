using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Deja.Crypto.BcPgp;
using Office = Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace OutlookPrivacyPlugin
{
	partial class MailView : IMailView
	{
		#region Form Region Factory

		[Microsoft.Office.Tools.Outlook.FormRegionMessageClass(
			Microsoft.Office.Tools.Outlook.FormRegionMessageClassAttribute.Note)]
		[Microsoft.Office.Tools.Outlook.FormRegionName("OutlookPrivacyPlugin.MailView")]
		public partial class MailViewFactory
		{
			// Occurs before the form region is initialized.
			// To prevent the form region from appearing, set e.Cancel to true.
			// Use e.OutlookItem to get a reference to the current Outlook item.
			private void MailViewFactory_FormRegionInitializing(object sender,
				Microsoft.Office.Tools.Outlook.FormRegionInitializingEventArgs e)
			{
			}
		}

		#endregion

		// Occurs before the form region is displayed.
		private void MailView_FormRegionShowing(object sender, System.EventArgs e)
		{
			var pluginInstance = OutlookPrivacyPlugin.Instance;

			var mailItem = (Outlook.MailItem) OutlookItem;

			var decryptedMail = pluginInstance.DecryptEmail(mailItem);

			if (decryptedMail == null)
			{
				OutlookFormRegion.Visible = false;
				return;
			}
			
			decryptedMail.Show(this);
		}

		// Occurs when the form region is closed.
		private void MailView_FormRegionClosed(object sender, System.EventArgs e)
		{
			
		}

		public void ShowHtmlEmail(string body)
		{
			htmlEmailView.Visible = true;
			plainEmailView.Visible = false;
			htmlEmailView.DocumentText = body;
		}

		public void ShowPlainEmail(string body)
		{
			plainEmailView.Visible = true;
			htmlEmailView.Visible = false;
			plainEmailView.Text = body;
		}
	}
}
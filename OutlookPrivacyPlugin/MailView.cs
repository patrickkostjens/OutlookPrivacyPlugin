using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

		public void ShowAttachments(IEnumerable<Attachment> attachments)
		{
			attachmentList.DataSource = attachments;
		}

		private void attachmentList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var currentItems = attachmentList.SelectedItems;
			foreach (Attachment attachment in currentItems)
			{
				System.Diagnostics.Process.Start(attachment.TempFile);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var currentItems = attachmentList.SelectedItems;
			foreach (Attachment attachment in currentItems)
			{
				var saveDialog = new SaveFileDialog
				{
					FileName = attachment.FileName,
					// TODO Fix file type
					Filter = string.Format("{0} | *.{0}|All files (*.*)|*.*", Path.GetExtension(attachment.FileName))
				};

				if (saveDialog.ShowDialog() != DialogResult.OK)
				{
					continue;
				}
				var fileStream = saveDialog.OpenFile();
				File.OpenRead(attachment.TempFile).CopyTo(fileStream);
				fileStream.Close();
			}
		}
	}
}
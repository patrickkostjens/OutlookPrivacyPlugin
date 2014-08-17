using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace OutlookPrivacyPlugin
{
	internal partial class Recipient : Form
	{
		/// <summary>
		/// Use Encryption or SIgning key?
		/// </summary>
		public bool Encryption = true;

		private readonly List<string> _defaultKeys;

		internal IList<string> SelectedKeys
		{
			get
			{
				var recipients = new List<string>();

				for (var i = 0; i < KeyBox.Items.Count; i++)
				{
					var recipient = (GnuKey)KeyBox.Items[i];
					if (KeyBox.GetItemChecked(i))
						recipients.Add(recipient.Key);
				}

				return recipients;
			}
		}

		internal Recipient(List<string> defaultRecipients)
		{
			_defaultKeys = defaultRecipients;
			InitializeComponent();
		}

		private void Passphrase_Load(object sender, EventArgs e)
		{
			// Did we locate all recipients keys?
			var unfoundKeys = true;

			IList<GnuKey> keys = Encryption ? Globals.OutlookPrivacyPlugin.GetKeysForEncryption() : Globals.OutlookPrivacyPlugin.GetKeysForSigning();
			if (keys.Count <= 0)
			{
				// No keys available, no use in showing this dialog at all
				Hide();
				return;
			}

			var datasource = new List<GnuKey>();
			var selectedCount = 0;

			// 1/ Collect selected keys and sort them.
			foreach (var gnuKey in keys)
			{
				if (_defaultKeys.Find(key => gnuKey.Key.StartsWith(key, true, null)) == null)
				{
					continue;
				}
				selectedCount++;
				datasource.Add(gnuKey);
			}

			datasource.Sort(new GnuKeySorter());

			// If we found all the keys we don't need to show dialog
			if (datasource.Count == _defaultKeys.Count)
			{
				unfoundKeys = false;
			}

			// 2/ Collect unselected keys and sort them.
			var datasource2 = new List<GnuKey>();
			foreach (var gnuKey in keys)
			{
				if (_defaultKeys.Find(key => gnuKey.Key.StartsWith(key, true, null)) == null)
					datasource2.Add(gnuKey);
			}

			datasource2.Sort(new GnuKeySorter());

			// Append unselected keys to datasource.
			datasource.AddRange(datasource2);

			// Setup KeyBox
			KeyBox.DataSource = datasource;
			KeyBox.DisplayMember = "KeyDisplay";
			KeyBox.ValueMember = "Key";

			int boxHeight = (keys.Count > 10) ? KeyBox.ItemHeight * 10 : KeyBox.ItemHeight * keys.Count;
			KeyBox.Height = boxHeight + 5;
			Height = boxHeight + 90;

			// Enlarge dialog to fit the longest key
			using (var g = CreateGraphics())
			{
				var maxSize = Width;
				foreach (var key in datasource)
				{
					var textWidth = (int)g.MeasureString(key.KeyDisplay, KeyBox.Font).Width + 50;
					if (textWidth > maxSize)
						maxSize = textWidth;
				}
				Width = maxSize;
				CenterToScreen();
			}

			// Note: Keybox sorted property MUST be False!
			//       unless the custom sort strategy is voided!
			for (var i = 0; i < selectedCount; i++)
				KeyBox.SetItemChecked(i, true);

			// If we found all the keys we don't need to show dialog
			if (!unfoundKeys)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}

	#region GnuKey_Sorter
	internal class GnuKeySorter : IComparer<GnuKey>
	{
		public int Compare(GnuKey x, GnuKey y)
		{
			return x.KeyDisplay.CompareTo(y.KeyDisplay);
		}
	}
	#endregion
}

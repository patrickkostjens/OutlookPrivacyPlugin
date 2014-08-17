﻿using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace OutlookPrivacyPlugin
{
	internal partial class Settings : Form
	{
		internal Settings(Properties.Settings settings)
		{
			InitializeComponent();

			AutoDecrypt = settings.AutoDecrypt;
			AutoVerify = settings.AutoVerify;
			AutoEncrypt = settings.AutoEncrypt;
			AutoSign = settings.AutoSign;
			Encrypt2Self = settings.Encrypt2Self;

			DefaultKey = settings.DefaultKey;
			DefaultDomain = settings.DefaultDomain;

			Default2PlainFormat = settings.Default2PlainFormat;

			IgnoreIntegrityCheck = settings.IgnoreIntegrityCheck;

			// Temporary disable all settings regarding auto-verify/decrypt
			// MainTabControl.TabPages.RemoveByKey(ReadTab.Name);
		}

		internal string DefaultKey { get; set; }

		internal bool IgnoreIntegrityCheck
		{
			get { return IgnoreIntegrityCheckBox.Checked; }
			set { IgnoreIntegrityCheckBox.Checked = value; }
		}

		internal bool Default2PlainFormat
		{
			get { return Default2PlainTextCheckBox.Checked; }
			set { Default2PlainTextCheckBox.Checked = value; }
		}

		internal bool AutoDecrypt
		{
			get { return DecryptCheckBox.Checked; }
			private set { DecryptCheckBox.Checked = value; }
		}

		internal bool AutoVerify
		{
			get { return VerifyCheckBox.Checked; }
			private set { VerifyCheckBox.Checked = value; }
		}

		internal bool AutoEncrypt
		{
			get { return EncryptCheckBox.Checked; }
			private set { EncryptCheckBox.Checked = value; }
		}

		internal bool AutoSign
		{
			get { return SignCheckBox.Checked; }
			private set { SignCheckBox.Checked = value; }
		}

		internal bool Encrypt2Self
		{
			get { return Encrypt2SelfCheckBox.Checked; }
			private set { Encrypt2SelfCheckBox.Checked = value; }
		}

		internal string DefaultDomain
		{
			get { return DefaultDomainTextBox.Text; }
			set { DefaultDomainTextBox.Text = value; }
		}

		private void OkButton_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void DefaultDomainTextBox_TextChanged(object sender, System.EventArgs e)
		{
			DefaultDomain = DefaultDomainTextBox.Text;
		}

		private void Settings_Load(object sender, System.EventArgs e)
		{

		}
	}
}

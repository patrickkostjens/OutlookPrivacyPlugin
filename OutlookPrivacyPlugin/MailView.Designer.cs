namespace OutlookPrivacyPlugin
{
	[System.ComponentModel.ToolboxItemAttribute(false)]
	partial class MailView : Microsoft.Office.Tools.Outlook.FormRegionBase
	{
		public MailView(Microsoft.Office.Interop.Outlook.FormRegion formRegion)
			: base(Globals.Factory, formRegion)
		{
			this.InitializeComponent();
		}

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.plainEmailView = new System.Windows.Forms.Label();
			this.htmlEmailView = new System.Windows.Forms.WebBrowser();
			this.SuspendLayout();
			// 
			// plainEmailView
			// 
			this.plainEmailView.AutoSize = true;
			this.plainEmailView.Location = new System.Drawing.Point(3, 0);
			this.plainEmailView.Name = "plainEmailView";
			this.plainEmailView.Size = new System.Drawing.Size(74, 13);
			this.plainEmailView.TabIndex = 0;
			this.plainEmailView.Text = "plainMailLabel";
			// 
			// htmlEmailView
			// 
			this.htmlEmailView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.htmlEmailView.IsWebBrowserContextMenuEnabled = false;
			this.htmlEmailView.Location = new System.Drawing.Point(0, 0);
			this.htmlEmailView.MinimumSize = new System.Drawing.Size(20, 20);
			this.htmlEmailView.Name = "htmlEmailView";
			this.htmlEmailView.Size = new System.Drawing.Size(322, 197);
			this.htmlEmailView.TabIndex = 1;
			this.htmlEmailView.Visible = false;
			// 
			// MailView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.plainEmailView);
			this.Controls.Add(this.htmlEmailView);
			this.Name = "MailView";
			this.Size = new System.Drawing.Size(322, 197);
			this.FormRegionShowing += new System.EventHandler(this.MailView_FormRegionShowing);
			this.FormRegionClosed += new System.EventHandler(this.MailView_FormRegionClosed);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#region Form Region Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private static void InitializeManifest(Microsoft.Office.Tools.Outlook.FormRegionManifest manifest, Microsoft.Office.Tools.Outlook.Factory factory)
		{
			manifest.FormRegionName = "MailView";
			manifest.FormRegionType = Microsoft.Office.Tools.Outlook.FormRegionType.Adjoining;
			manifest.ShowInspectorCompose = false;

		}

		#endregion

		private System.Windows.Forms.Label plainEmailView;
		private System.Windows.Forms.WebBrowser htmlEmailView;


		public partial class MailViewFactory : Microsoft.Office.Tools.Outlook.IFormRegionFactory
		{
			public event Microsoft.Office.Tools.Outlook.FormRegionInitializingEventHandler FormRegionInitializing;

			private Microsoft.Office.Tools.Outlook.FormRegionManifest _Manifest;

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public MailViewFactory()
			{
				this._Manifest = Globals.Factory.CreateFormRegionManifest();
				MailView.InitializeManifest(this._Manifest, Globals.Factory);
				this.FormRegionInitializing += new Microsoft.Office.Tools.Outlook.FormRegionInitializingEventHandler(this.MailViewFactory_FormRegionInitializing);
			}

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			public Microsoft.Office.Tools.Outlook.FormRegionManifest Manifest
			{
				get
				{
					return this._Manifest;
				}
			}

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			Microsoft.Office.Tools.Outlook.IFormRegion Microsoft.Office.Tools.Outlook.IFormRegionFactory.CreateFormRegion(Microsoft.Office.Interop.Outlook.FormRegion formRegion)
			{
				MailView form = new MailView(formRegion);
				form.Factory = this;
				return form;
			}

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			byte[] Microsoft.Office.Tools.Outlook.IFormRegionFactory.GetFormRegionStorage(object outlookItem, Microsoft.Office.Interop.Outlook.OlFormRegionMode formRegionMode, Microsoft.Office.Interop.Outlook.OlFormRegionSize formRegionSize)
			{
				throw new System.NotSupportedException();
			}

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			bool Microsoft.Office.Tools.Outlook.IFormRegionFactory.IsDisplayedForItem(object outlookItem, Microsoft.Office.Interop.Outlook.OlFormRegionMode formRegionMode, Microsoft.Office.Interop.Outlook.OlFormRegionSize formRegionSize)
			{
				if (this.FormRegionInitializing != null)
				{
					Microsoft.Office.Tools.Outlook.FormRegionInitializingEventArgs cancelArgs = Globals.Factory.CreateFormRegionInitializingEventArgs(outlookItem, formRegionMode, formRegionSize, false);
					this.FormRegionInitializing(this, cancelArgs);
					return !cancelArgs.Cancel;
				}
				else
				{
					return true;
				}
			}

			[System.Diagnostics.DebuggerNonUserCodeAttribute()]
			Microsoft.Office.Tools.Outlook.FormRegionKindConstants Microsoft.Office.Tools.Outlook.IFormRegionFactory.Kind
			{
				get
				{
					return Microsoft.Office.Tools.Outlook.FormRegionKindConstants.WindowsForms;
				}
			}
		}
	}

	partial class WindowFormRegionCollection
	{
		internal MailView MailView
		{
			get
			{
				foreach (var item in this)
				{
					if (item.GetType() == typeof(MailView))
						return (MailView)item;
				}
				return null;
			}
		}
	}
}

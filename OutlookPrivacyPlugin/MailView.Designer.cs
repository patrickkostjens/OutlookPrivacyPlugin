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
			this.components = new System.ComponentModel.Container();
			this.plainEmailView = new System.Windows.Forms.Label();
			this.htmlEmailView = new System.Windows.Forms.WebBrowser();
			this.tableLayout = new System.Windows.Forms.TableLayoutPanel();
			this.viewPanel = new System.Windows.Forms.Panel();
			this.attachmentList = new System.Windows.Forms.ListBox();
			this.attachmentContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.signatureLabel = new System.Windows.Forms.Label();
			this.tableLayout.SuspendLayout();
			this.viewPanel.SuspendLayout();
			this.attachmentContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// plainEmailView
			// 
			this.plainEmailView.AutoSize = true;
			this.plainEmailView.Location = new System.Drawing.Point(0, 0);
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
			this.htmlEmailView.MinimumSize = new System.Drawing.Size(200, 200);
			this.htmlEmailView.Name = "htmlEmailView";
			this.htmlEmailView.Size = new System.Drawing.Size(721, 294);
			this.htmlEmailView.TabIndex = 1;
			this.htmlEmailView.Visible = false;
			// 
			// tableLayout
			// 
			this.tableLayout.ColumnCount = 1;
			this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayout.Controls.Add(this.attachmentList, 0, 0);
			this.tableLayout.Controls.Add(this.viewPanel, 0, 2);
			this.tableLayout.Controls.Add(this.signatureLabel, 0, 1);
			this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayout.Location = new System.Drawing.Point(0, 0);
			this.tableLayout.Name = "tableLayout";
			this.tableLayout.RowCount = 3;
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayout.Size = new System.Drawing.Size(727, 330);
			this.tableLayout.TabIndex = 2;
			// 
			// viewPanel
			// 
			this.viewPanel.Controls.Add(this.plainEmailView);
			this.viewPanel.Controls.Add(this.htmlEmailView);
			this.viewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.viewPanel.Location = new System.Drawing.Point(3, 53);
			this.viewPanel.Name = "viewPanel";
			this.viewPanel.Size = new System.Drawing.Size(721, 294);
			this.viewPanel.TabIndex = 3;
			// 
			// attachmentList
			// 
			this.attachmentList.ContextMenuStrip = this.attachmentContextMenu;
			this.attachmentList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.attachmentList.FormattingEnabled = true;
			this.attachmentList.HorizontalScrollbar = true;
			this.attachmentList.Location = new System.Drawing.Point(3, 3);
			this.attachmentList.MultiColumn = true;
			this.attachmentList.Name = "attachmentList";
			this.attachmentList.Size = new System.Drawing.Size(721, 24);
			this.attachmentList.TabIndex = 4;
			this.attachmentList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.attachmentList_MouseDoubleClick);
			// 
			// attachmentContextMenu
			// 
			this.attachmentContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem});
			this.attachmentContextMenu.Name = "attachmentContextMenu";
			this.attachmentContextMenu.Size = new System.Drawing.Size(99, 26);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(98, 22);
			this.saveToolStripMenuItem.Text = "Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			// 
			// signatureLabel
			// 
			this.signatureLabel.AutoSize = true;
			this.signatureLabel.Location = new System.Drawing.Point(3, 30);
			this.signatureLabel.Name = "signatureLabel";
			this.signatureLabel.Size = new System.Drawing.Size(76, 13);
			this.signatureLabel.TabIndex = 5;
			this.signatureLabel.Text = "signatureLabel";
			// 
			// MailView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.Controls.Add(this.tableLayout);
			this.Name = "MailView";
			this.Size = new System.Drawing.Size(727, 330);
			this.FormRegionShowing += new System.EventHandler(this.MailView_FormRegionShowing);
			this.tableLayout.ResumeLayout(false);
			this.tableLayout.PerformLayout();
			this.viewPanel.ResumeLayout(false);
			this.viewPanel.PerformLayout();
			this.attachmentContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

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
		private System.Windows.Forms.TableLayoutPanel tableLayout;
		private System.Windows.Forms.Panel viewPanel;
		private System.Windows.Forms.ListBox attachmentList;
		private System.Windows.Forms.ContextMenuStrip attachmentContextMenu;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.Label signatureLabel;


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

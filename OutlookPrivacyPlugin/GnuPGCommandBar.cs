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
using System.Linq;

using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;

namespace OutlookPrivacyPlugin
{
	/// <summary>
	/// GnuPG CommandBar wrapper class
	/// </summary>
	internal class GnuPGCommandBar
	{
		/// <summary>
		/// Contants and members.
		/// </summary>
		private const string CmdBarName = "OutlookGnuPG";

		// Button list/dictionary.
		private Dictionary<string, Office.CommandBarButton> _buttons = new Dictionary<string, Office.CommandBarButton>();

		/// <summary>
		/// Public access to some properties.
		/// </summary>
		internal Office.CommandBar CommandBar { get; private set; }

		internal Outlook.Explorer Explorer { get; private set; }

		/// <summary>
		/// The constructor
		/// </summary>
		/// <param name="activeExplorer">The Outlook active explorer containing the CommandBar(s).</param>
		public GnuPGCommandBar(Outlook.Explorer activeExplorer)
		{
			Explorer = activeExplorer;
		}

		/// <summary>
		/// Helper function to find a named CommandBar
		/// </summary>
		/// <param name="name">CommandBar name</param>
		/// <returns>The CommandBar found or null.</returns>
		private Office.CommandBar Find(String name)
		{
			return Explorer.CommandBars.Cast<Office.CommandBar>().FirstOrDefault(bar => bar.Name == name);
		}

		/// <summary>
		/// Remove the GnuPG CommandBar, if any.
		/// </summary>
		/// <param name="explorer"></param>
		internal void Remove()
		{
			var bar = Find(CmdBarName);
			if (bar == null)
				return;
			bar.Delete();
		}

		/// <summary>
		/// Add a new CommandBar
		/// </summary>
		internal void Add()
		{
			if (Explorer == null)
				return;

			CommandBar = Find(CmdBarName);
			if (CommandBar == null)
			{
				var bars = Explorer.CommandBars;
				CommandBar = bars.Add(CmdBarName, Office.MsoBarPosition.msoBarTop, false, true);
			}
			CommandBar.Visible = true;

			foreach (var btn in new[] {"About", "Settings"})
			{
				_buttons.Add(btn, (Office.CommandBarButton) CommandBar.Controls.Add(Office.MsoControlType.msoControlButton,
					Type.Missing, Type.Missing, 1, true));
				_buttons[btn].Style = Office.MsoButtonStyle.msoButtonIconAndCaption;
				_buttons[btn].Caption = btn;
				_buttons[btn].Tag = "GnuPG" + btn;
			}

			// http://www.kebabshopblues.co.uk/2007/01/04/visual-studio-2005-tools-for-office-commandbarbutton-faceid-property/
			_buttons["About"].FaceId = 700;
			_buttons["Settings"].FaceId = 2144;

			_buttons["About"].Picture = ImageConverter.Convert(Properties.Resources.Logo);
			_buttons["Settings"].Picture = ImageConverter.Convert(Properties.Resources.database_gear);
		}


		/// <summary>
		/// Return a given button by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal Office.CommandBarButton GetButton(string name)
		{
			return _buttons.ContainsKey(name) ? _buttons[name] : null;
		}

		/// <summary>
		/// Save the CommandBar position in application property settings.
		/// </summary>
		/// <param name="settings"></param>
		internal void SavePosition(Properties.Settings settings)
		{
			settings.BarLeft = CommandBar.Left;
			settings.BarPosition = (int) CommandBar.Position;
			settings.BarPositionSaved = true;
			settings.BarRowIndex = CommandBar.RowIndex;
			settings.BarTop = CommandBar.Top;
			settings.Save();
		}

		/// <summary>
		/// Set the CommandBar position from application property settings.
		/// </summary>
		/// <param name="settings"></param>
		internal void RestorePosition(Properties.Settings settings)
		{
			// Position the bar
			if (settings.BarPositionSaved)
			{
				CommandBar.Position = (Office.MsoBarPosition) settings.BarPosition;
				CommandBar.RowIndex = settings.BarRowIndex;
				CommandBar.Top = settings.BarTop;
				CommandBar.Left = settings.BarLeft;
			}
			else
			{
				var standardBar = Find("standard");
				if (standardBar != null)
				{
					var oldPos = standardBar.Left;
					CommandBar.RowIndex = standardBar.RowIndex;
					CommandBar.Left = standardBar.Left + standardBar.Width;
					CommandBar.Position = Office.MsoBarPosition.msoBarTop;
					standardBar.Left = oldPos;
				}
				else
				{
					CommandBar.Position = Office.MsoBarPosition.msoBarTop;
				}
			}
		}
	}
}
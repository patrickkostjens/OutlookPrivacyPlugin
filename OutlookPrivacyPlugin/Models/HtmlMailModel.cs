using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookPrivacyPlugin.Models
{
	public class HtmlMailModel : MailModel
	{
		public override void Show(IMailView mailView)
		{
			mailView.ShowHtmlEmail(Body);
		}
	}
}

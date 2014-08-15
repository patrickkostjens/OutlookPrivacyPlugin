using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookPrivacyPlugin.Models
{
	public class PlainMailModel : MailModel
	{
		public override void Show(IMailView mailView)
		{
			mailView.ShowPlainEmail(Body);
		}
	}
}

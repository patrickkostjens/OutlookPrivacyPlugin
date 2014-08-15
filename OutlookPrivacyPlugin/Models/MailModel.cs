using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookPrivacyPlugin.Models
{
	public abstract class MailModel
	{
		public string Body;
		public List<Attachment> Attachments;

		public abstract void Show(IMailView mailView);
	}
}

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

		protected MailModel()
		{
			Attachments = new List<Attachment>();
		}

		public virtual void Show(IMailView mailView)
		{
			mailView.ShowAttachments(Attachments);
		}
	}
}

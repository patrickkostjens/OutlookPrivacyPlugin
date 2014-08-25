using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookPrivacyPlugin
{
	public interface IMailView
	{
		void ShowHtmlEmail(string body);

		void ShowPlainEmail(string body);

		void ShowAttachments(IEnumerable<Attachment> attachments);
	}
}

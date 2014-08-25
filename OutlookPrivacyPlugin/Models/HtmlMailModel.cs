namespace OutlookPrivacyPlugin.Models
{
	public class HtmlMailModel : MailModel
	{
		public override void Show(IMailView mailView)
		{
			base.Show(mailView);
			mailView.ShowHtmlEmail(Body);
		}
	}
}

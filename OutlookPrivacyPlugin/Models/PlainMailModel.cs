namespace OutlookPrivacyPlugin.Models
{
	public class PlainMailModel : MailModel
	{
		public override void Show(IMailView mailView)
		{
			base.Show(mailView);
			mailView.ShowPlainEmail(Body);
		}
	}
}

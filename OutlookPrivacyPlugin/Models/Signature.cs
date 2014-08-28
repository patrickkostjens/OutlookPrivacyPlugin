namespace OutlookPrivacyPlugin.Models
{
	public class Signature
	{
		public bool Valid { get; set; }
		public string UserId { get; set; }
		public string KeyId { get; set; }

		public override string ToString()
		{
			var message = Valid ? "Valid" : "Invalid";
			message += " signature from " + UserId + " with key " + KeyId;
			return message;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;


namespace C2dmSharp
{
	public class C2dmMessage
	{
		public C2dmMessage()
		{
			this.RegistrationId = string.Empty;
			this.CollapseKey = string.Empty;
			this.Data = new NameValueCollection();
			this.DelayWhileIdle = null;
		}

		public string RegistrationId
		{
			get;
			set;
		}

		public string CollapseKey
		{
			get;
			set;
		}

		public NameValueCollection Data
		{
			get;
			set;
		}

		public bool? DelayWhileIdle
		{
			get;
			set;
		}

		public string GetPostData()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("registration_id={0}&collapse_key={1}&", //&auth={2}&",
				HttpUtility.UrlEncode(this.RegistrationId),
				HttpUtility.UrlEncode(this.CollapseKey)
				//HttpUtility.UrlEncode(this.GoogleLoginAuthorizationToken)
				);

			if (this.DelayWhileIdle.HasValue)
				sb.AppendFormat("delay_while_idle={0}&", this.DelayWhileIdle.Value ? "true" : "false");

			foreach (var key in this.Data.AllKeys)
			{
				sb.AppendFormat("data.{0}={1}&",
					HttpUtility.UrlEncode(key),
					HttpUtility.UrlEncode(this.Data[key]));
			}

			//Remove trailing & if necessary
			if (sb.Length > 0 && sb[sb.Length - 1] == '&')
				sb.Remove(sb.Length - 1, 1);

			return sb.ToString();
		}

		public int GetMessageSize()
		{
			//http://groups.google.com/group/android-c2dm/browse_thread/thread/c70575480be4f883?pli=1
			// suggests that the max size of 1024 bytes only includes 
			// only char counts of:  keys, values, and the collapse_data value
			int size = this.CollapseKey.Length;

			foreach (var key in this.Data.AllKeys)
				size += key.Length + this.Data[key].Length;

			return size;
		}
	}
}

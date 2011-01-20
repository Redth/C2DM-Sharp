using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace C2dmSharp
{
	public class C2dmMessageTransport
	{
		private const string C2DM_SEND_URL = "https://android.apis.google.com/c2dm/send";

		public static C2dmMessageTransportResponse Send(C2dmMessage msg, string googleLoginAuthorizationToken, string senderID, string applicationID)
		{
			return send(msg, googleLoginAuthorizationToken, senderID, applicationID);
		}

		static C2dmMessageTransportResponse send(C2dmMessage msg, string googleLoginAuthorizationToken, string senderID, string applicationID)
		{
			C2dmMessageTransportResponse result = new C2dmMessageTransportResponse();
			result.Message = msg;

			var webReq = HttpWebRequest.Create(C2DM_SEND_URL) as HttpWebRequest;

			webReq.Headers["Authorization"] = googleLoginAuthorizationToken;

			using (var webReqStream = new StreamWriter(webReq.GetRequestStream()))
			{
				webReqStream.Write(msg.GetPostData());
				webReqStream.Flush();
			}

			try
			{
				var webResp = webReq.GetResponse() as HttpWebResponse;

				if (webResp != null)
				{					
					result.ResponseStatus = MessageTransportResponseStatus.Ok;

					var wrId = webResp.GetResponseHeader("id");
					var wrErr = webResp.GetResponseHeader("Error");

					if (!string.IsNullOrEmpty(wrId))
						result.MessageId = wrId;

					if (!string.IsNullOrEmpty(wrErr))
					{
						switch (wrErr.ToLower().Trim())
						{
							case "quotaexceeded":
								result.ResponseStatus = MessageTransportResponseStatus.QuotaExceeded;
								break;
							case "devicequotaexceeded":
								result.ResponseStatus = MessageTransportResponseStatus.DeviceQuotaExceeded;
								break;
							case "invalidregistration":
								result.ResponseStatus = MessageTransportResponseStatus.InvalidRegistration;
								break;
							case "notregistered":
								result.ResponseStatus = MessageTransportResponseStatus.NotRegistered;
								break;
							case "messagetoobig":
								result.ResponseStatus = MessageTransportResponseStatus.MessageTooBig;
								break;
							case "missingcollapsekey":
								result.ResponseStatus = MessageTransportResponseStatus.MissingCollapseKey;
								break;
						}

						throw new MessageTransportException(wrErr, result);						
					}				
				}
			}
			catch (WebException webEx)
			{
				var webResp = webEx.Response as HttpWebResponse;

				if (webResp != null)
				{
					if (webResp.StatusCode == HttpStatusCode.Unauthorized)
					{
						//401 bad auth token
						result.ResponseCode = MessageTransportResponseCode.InvalidAuthToken;
						result.ResponseStatus = MessageTransportResponseStatus.Error;
						throw new InvalidAuthenticationTokenTransportException(result);
					}
					else if (webResp.StatusCode == HttpStatusCode.ServiceUnavailable)
					{
						//First try grabbing the retry-after header and parsing it.
						TimeSpan retryAfter = new TimeSpan(0, 0, 120);

						var wrRetryAfter = webResp.GetResponseHeader("Retry-After");

						if (!string.IsNullOrEmpty(wrRetryAfter))
						{
							DateTime wrRetryAfterDate = DateTime.UtcNow;

							if (DateTime.TryParse(wrRetryAfter, out wrRetryAfterDate))
								retryAfter = wrRetryAfterDate - DateTime.UtcNow;
							else
							{
								int wrRetryAfterSeconds = 120;
								if (int.TryParse(wrRetryAfter, out wrRetryAfterSeconds))
									retryAfter = new TimeSpan(0,0, wrRetryAfterSeconds);
							}
						}

						//503 exponential backoff, get retry-after header
						result.ResponseCode = MessageTransportResponseCode.ServiceUnavailable;
						result.ResponseStatus = MessageTransportResponseStatus.Error;

						throw new ServiceUnavailableTransportException(retryAfter, result);
					}
				}
			}

			return result;
		}
	}
}

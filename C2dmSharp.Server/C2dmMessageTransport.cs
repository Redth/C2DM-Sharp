using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security;
using System.Net.Security;

namespace C2dmSharp.Server
{
	internal class C2dmMessageTransport
	{
		internal static event Action<string> UpdateGoogleClientAuthToken;

		static C2dmMessageTransport()
		{
			ServicePointManager.ServerCertificateValidationCallback += certValidateCallback;
		}
		static bool certValidateCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrs)
		{
			return true;
		}

		private const string C2DM_SEND_URL = "https://android.apis.google.com/c2dm/send";

		public static C2dmMessageTransportResponse Send(C2dmMessage msg, string googleLoginAuthorizationToken, string senderID, string applicationID)
		{
			return send(msg, googleLoginAuthorizationToken, senderID, applicationID);
		}

		static C2dmMessageTransportResponse send(C2dmMessage msg, string googleLoginAuthorizationToken, string senderID, string applicationID)
		{
			C2dmMessageTransportResponse result = new C2dmMessageTransportResponse();
			result.Message = msg;

			var postData = msg.GetPostData();

			var webReq = (HttpWebRequest)WebRequest.Create(C2DM_SEND_URL);
			//webReq.ContentLength = postData.Length;
			webReq.Method = "POST";
			webReq.ContentType = "application/x-www-form-urlencoded";
			webReq.UserAgent = "C2DM-Sharp (version: 1.0)";
			webReq.Headers.Add("Authorization: GoogleLogin auth=" + googleLoginAuthorizationToken); 

			using (var webReqStream = new StreamWriter(webReq.GetRequestStream(), Encoding.ASCII))
			{
				webReqStream.Write(postData);
				webReqStream.Close();
			}		
			
			try
			{
				var webResp = webReq.GetResponse() as HttpWebResponse;

				if (webResp != null)
				{					
					result.ResponseStatus = MessageTransportResponseStatus.Ok;

					//Check for an updated auth token and store it here if necessary
					var updateClientAuth = webResp.GetResponseHeader("Update-Client-Auth");
					if (!string.IsNullOrEmpty(updateClientAuth) && C2dmMessageTransport.UpdateGoogleClientAuthToken != null)
						UpdateGoogleClientAuthToken(updateClientAuth);
						
					//Get the response body
					var responseBody = "Error=";
					try { responseBody = (new StreamReader(webResp.GetResponseStream())).ReadToEnd(); }
					catch { }

					//Handle the type of error
					if (responseBody.StartsWith("Error="))
					{
						var wrErr = responseBody.Substring(responseBody.IndexOf("Error=") + 6);
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
							default:
								result.ResponseStatus = MessageTransportResponseStatus.Error;
								break;
						}

						throw new MessageTransportException(wrErr, result);
					}
					else
					{
						//Get the message ID
						if (responseBody.StartsWith("id="))
							result.MessageId = responseBody.Substring(3).Trim();
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

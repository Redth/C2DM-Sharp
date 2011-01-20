using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C2dmSharp.Server
{
	public class C2dmMessageTransportResponse
	{
		public string MessageId
		{
			get;
			set;
		}

		public C2dmMessage Message
		{
			get;
			set;
		}

		public MessageTransportResponseCode ResponseCode
		{
			get;
			set;
		}

		public MessageTransportResponseStatus ResponseStatus
		{
			get;
			set;
		}
	}

	public enum MessageTransportResponseCode
	{
		Ok,
		Error,
		ServiceUnavailable,
		InvalidAuthToken
	}

	public enum MessageTransportResponseStatus
	{
		Ok,
		Error,
		QuotaExceeded,
		DeviceQuotaExceeded,
		InvalidRegistration,
		NotRegistered,
		MessageTooBig,
		MissingCollapseKey,
	}
}

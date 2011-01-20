using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;

namespace C2dmSharp.Client
{
	[Service]
	public class C2dmService : IntentService
	{
		protected override void OnHandleIntent(Android.Content.Intent intent)
		{
			//Handle the c2dm intent, decide which it is
			if (intent.Action == C2dmBroadcastReceiver.GOOGLE_ACTION_C2DM_INTENT_REGISTRATION)
			{
				var registrationId = intent.GetStringExtra("registration_id");
				if (!string.IsNullOrEmpty(registrationId))
				{
					C2dmClient.FireRegistered(registrationId);
					return;
				}

				var unregistered = intent.GetStringExtra("unregistered");
				if (!string.IsNullOrEmpty(unregistered))
				{
					C2dmClient.FireUnregistered();
					return;
				}

				var error = intent.GetStringExtra("error");
				if (!string.IsNullOrEmpty(error))
				{
					C2dmClient.FireError(new C2dmRegistrationError(error, C2dmRegistrationError.GetErrorDescription(error)));
					return;
				}			
			}
			else if (intent.Action == C2dmBroadcastReceiver.GOOGLE_ACTION_C2DM_INTENT_RECEIVE)
				C2dmClient.FireReceiveMessage(intent.Extras);
		}
	}
}

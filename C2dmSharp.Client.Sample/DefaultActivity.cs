using System;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace C2dmSharp.Client.Sample
{
	[Activity(Label = "C2DM-Sharp Sample", MainLauncher = true)]
	public class DefaultActivity : Activity
	{
		Button buttonRegister;
		Button buttonUnregister;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			Android.Util.Log.D("C2DM-Sample", "Hello World!");

			// Set our view from the "main" layout resource
			SetContentView(Resource.layout.main);

			// Get our buttons from the layout resource,
			// and attach an event to it
			buttonRegister = FindViewById<Button>(Resource.id.buttonRegister);
			buttonUnregister = FindViewById<Button>(Resource.id.buttonUnregister);

			Android.Util.Log.D("C2DM-Sample", "Got Buttons...");

			buttonUnregister.Enabled = false;

			buttonRegister.Click += delegate
			{
				try 
				{ 
					C2dmSharp.Client.C2dmClient.Register(this); 
				}
				catch (NoGoogleAccountsOnDeviceRegistrationException ngex)
				{
					MakeToast("No Google Accounts on this Device!  Please add one and try again!");
				}

				buttonRegister.Enabled = false;
			};

			buttonUnregister.Click += delegate
			{
				C2dmSharp.Client.C2dmClient.Unregister(this);
				buttonUnregister.Enabled = false;
			};


			Android.Util.Log.D("C2DM-Sample", "Registered Button Clicks...");

			C2dmSharp.Client.C2dmClient.ReceiveMessage += new Action<Bundle>(C2dmClient_ReceiveMessage);
			C2dmSharp.Client.C2dmClient.Registered += new Action<string>(C2dmClient_Registered);
			C2dmSharp.Client.C2dmClient.RegisterError += new Action<Exception>(C2dmClient_RegisterError);
			C2dmSharp.Client.C2dmClient.Unregistered += new Action(C2dmClient_Unregistered);

			Android.Util.Log.D("C2DM-Sample", "Registered Client Events...");
		}

		void C2dmClient_Unregistered()
		{
			MakeToast("C2DM Unregistered");

			if (buttonRegister != null)
				buttonRegister.Enabled = true;
		}

		void C2dmClient_RegisterError(Exception ex)
		{
			MakeToast(ex.Message);
		}

		void C2dmClient_Registered(string registrationId)
		{
			MakeToast("C2DM Registered (ID: " + registrationId + ")");

			if (buttonUnregister != null)
				buttonUnregister.Enabled = true;
		}

		void C2dmClient_ReceiveMessage(Bundle extras)
		{
			var msg = new StringBuilder();
			msg.AppendLine("C2DM Received Message");

			foreach (var key in extras.KeySet())
				msg.AppendLine("    " + key + "=" + extras.Get(key).ToString());

			MakeToast(msg.ToString());
		}

		void MakeToast(string msg)
		{
			Toast.MakeText(this, msg, ToastLength.Short).Show();
		}
	}
}


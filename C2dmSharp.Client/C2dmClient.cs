using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Accounts;

namespace C2dmSharp.Client
{
	public class C2dmClient
	{
		/// <summary>
		/// Raised whenever a C2DM Message is Received, includes a Bundle object with Key/Value extras data pairs
		/// </summary>
		public static event Action<Bundle> ReceiveMessage;

		/// <summary>
		/// Raised whenever there is an error Registering for C2DM
		/// </summary>
		public static event Action<Exception> RegisterError;

		/// <summary>
		/// Raised whenever the device has successfully registered for C2DM, with the Registration_ID
		/// </summary>
		public static event Action<string> Registered;

		/// <summary>
		/// Raised whenever the device has successfully unregistered from C2DM
		/// </summary>
		public static event Action Unregistered;

		/// <summary>
		/// Register's the Device for C2DM Messages
		/// </summary>
		/// <param name="context">Context</param>
		public static void Register(Context context) //, string emailOfSender)
		{
			var accountManager = context.GetSystemService(Context.AccountService) as AccountManager;

			var accounts = accountManager.GetAccountsByType("com.google");

			if (accounts == null || accounts.Length <= 0)
			{
				if (C2dmClient.RegisterError != null)
					C2dmClient.RegisterError(new NoGoogleAccountsOnDeviceRegistrationException());
				else
					throw new NoGoogleAccountsOnDeviceRegistrationException();
			}

			var email = accounts[0].Name;

			Intent registrationIntent = new Intent("com.google.android.c2dm.intent.REGISTER");
			registrationIntent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
			registrationIntent.PutExtra("sender", email);
			context.StartService(registrationIntent);
		}

		/// <summary>
		/// Unregisters the Device from C2DM Messages
		/// </summary>
		/// <param name="context">Context</param>
		public static void Unregister(Context context)
		{
			Intent unregIntent = new Intent("com.google.android.c2dm.intent.UNREGISTER");
			unregIntent.PutExtra("app", PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
			context.StartService(unregIntent);
		}


		internal static void FireRegistered(string registrationId)
		{
			if (C2dmClient.Registered != null)
				C2dmClient.Registered.BeginInvoke(registrationId, null, null);
		}

		internal static void FireUnregistered()
		{
			if (C2dmClient.Unregistered != null)
				C2dmClient.Unregistered.BeginInvoke(null, null);
		}

		internal static void FireError(Exception ex)
		{
			if (C2dmClient.RegisterError != null)
				C2dmClient.RegisterError.BeginInvoke(ex, null, null);
		}

		internal static void FireReceiveMessage(Bundle extras)
		{
			if (C2dmClient.ReceiveMessage != null)
				C2dmClient.ReceiveMessage.BeginInvoke(extras, null, null);
		}
	}
}
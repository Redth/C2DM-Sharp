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

namespace C2dmSharp.Client
{
	public class C2dmBroadcastReceiver : BroadcastReceiver
	{
		public const string GOOGLE_ACTION_C2DM_INTENT_RECEIVE = "com.google.android.c2dm.permission.RECEIVE";
		public const string GOOGLE_ACTION_C2DM_INTENT_REGISTRATION = "com.google.android.c2dm.intent.REGISTRATION";
		public const string GOOGLE_PERMISSION_C2DM_SEND = "com.google.android.c2dm.permission.SEND";

		public override void OnReceive(Context context, Intent intent)
		{
			var c2dmIntent = new Intent(context, typeof(C2dmService));
			c2dmIntent.PutExtras(intent.Extras);

			context.StartService(c2dmIntent);			
		}
	}
}
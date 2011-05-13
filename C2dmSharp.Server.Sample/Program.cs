using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using C2dmSharp.Server;

namespace C2dmSharp.Server.Sample
{
	class Program
	{
		static C2dmService service;

		static void Main(string[] args)
		{
			var senderID = string.Empty; //Eg: "youremail@gmail.com";
			var password = string.Empty;
			var applicationID = string.Empty;

			Console.WriteLine("C2DM-Sharp Server Sample");
			Console.WriteLine("------------------------");
			Console.WriteLine();

			Console.WriteLine("Enter your Sender ID (The google email you registered your app on Google with)...");
			Console.Write("Sender ID> ");
			while (string.IsNullOrEmpty(senderID = Console.ReadLine()))
				Console.Write("Sender ID> ");

			Console.WriteLine("Enter your Password (The password to login with your google email)...");
			Console.Write("Password> ");
			while (string.IsNullOrEmpty(password = Console.ReadLine()))
				Console.Write("Password> ");

			Console.WriteLine("Enter your Application ID (The Package Name you registered your app on Google with)...");
			Console.Write("Application ID> ");
			while (string.IsNullOrEmpty(applicationID = Console.ReadLine()))
				Console.Write("Application ID> ");
			
			service = new C2dmService(senderID, password, applicationID);

			service.MessageSuccess += (C2dmMessageTransportResponse response) =>
			{
				Console.WriteLine("Message Sent: (id: " + response.MessageId + ")");
			};

			service.MessageFailure += (MessageTransportException ex) =>
			{
				Console.WriteLine("Message Failed: " + ex.Message);
			};

			service.Waiting += (DateTime waitUntil) =>
			{
				Console.WriteLine("Service told to back off until: " + waitUntil.ToString());
			};

			service.Start();

			while (true)
			{
				var extras = new NameValueCollection();
				string registrationId = string.Empty;
				string collapseKey = string.Empty;

				while (string.IsNullOrEmpty(registrationId))
				{
					Console.Write("Device Registration Id> ");
					registrationId = Console.ReadLine();
				}

				while (string.IsNullOrEmpty(collapseKey))
				{
					Console.Write("Collapse Key> ");
					collapseKey = Console.ReadLine();
				}

				Console.WriteLine("Enter key=value pairs to send... (Omit the data. part)");
				Console.WriteLine("Enter a blank line when finished.");

				string extra = string.Empty;

				while (!string.IsNullOrEmpty(extra = Console.ReadLine()))
				{
					var parts = extra.Split("=".ToCharArray(), 2);

					if (parts.Length > 0)
					{
						var key = parts[0];
						var value = "";

						if (parts.Length > 1)
							value = parts[1];

                        // The 'data.' part gets prepended to the key automatically by the library
						extras.Add(key, value);
					}
				}

				//Queue up the message to be sent
				service.QueueMessage(registrationId, extras, collapseKey);

				Console.WriteLine("Type 'exit' or 'quit' to close");
				Console.WriteLine(" or type a blank line to send another message");

				var exit = Console.ReadLine();

				if (exit.StartsWith("exit", StringComparison.InvariantCultureIgnoreCase)
					|| exit.StartsWith("quit", StringComparison.InvariantCultureIgnoreCase))
					break;

			}

			service.Stop();
		}
	}
}

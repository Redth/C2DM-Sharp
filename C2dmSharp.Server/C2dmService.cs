using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Specialized;
using System.Security;
using System.Net;

namespace C2dmSharp
{
	public class C2dmService
	{
		/// <summary>
		/// This is the Email that you used to be registered on google's c2dm whitelist
		/// </summary>
		public string SenderID
		{
			get;
			set;
		}

		/// <summary>
		/// Password for the SenderID
		/// </summary>
		public string Password
		{
			get;
			set;
		}

		/// <summary>
		/// Application ID that you registered with on google's Whitelist
		/// </summary>
		public string ApplicationID
		{
			get;
			set;
		}

		private string googleAuthToken = string.Empty;

		public event Action<MessageTransportException> MessageFailure;
		public event Action<C2dmMessageTransportResponse> MessageSuccess;
		public event Action<DateTime> Waiting;

		public bool running;

		//ConcurrentQueue<Message> messages;
		BlockingCollection<C2dmMessage> messages;
		List<C2dmMessageTransportWorker> workers;

		DateTime waitUntil = DateTime.MinValue;
		TimeSpan lastRetryAfter = new TimeSpan(0, 0, 0);

		public C2dmService()
		{
			var senderID = ConfigurationManager.AppSettings["C2DM.SenderID"] ?? "";
			var password = ConfigurationManager.AppSettings["C2DM.Password"] ?? "";
			var applicationID = ConfigurationManager.AppSettings["C2DM.ApplicationID"] ?? "";

			init(senderID, password, applicationID);
		}

		public C2dmService(string senderID, string password, string applicationID)
		{
			init(senderID, password, applicationID);
		}


		private void init(string senderID, string password, string applicationID)
		{
			this.SenderID = senderID;
			this.Password = password;
			this.SenderID = senderID;
			this.ApplicationID = applicationID;

			running = false;
			//messages = new ConcurrentQueue<Message>();
			messages = new BlockingCollection<C2dmMessage>();
			workers = new List<C2dmMessageTransportWorker>();

			//Get a new auth token
			RefreshGoogleAuthToken();
		}

		public void RefreshGoogleAuthToken()
		{
			string authUrl = "https://www.google.com/accounts/ClientLogin";

			var data = new NameValueCollection();

			data.Add("Email", this.SenderID);
			data.Add("Passwd", this.Password);
			data.Add("accountType", "GOOGLE_OR_HOSTED");
			data.Add("service", "ac2dm");
			data.Add("source", this.ApplicationID);

			var wc = new WebClient();
			
			try { googleAuthToken = Encoding.ASCII.GetString(wc.UploadValues(authUrl, data)); }
			catch (WebException ex)
			{
				var result = "Unknown Error";
				try { result = (new System.IO.StreamReader(ex.Response.GetResponseStream())).ReadToEnd(); }
				catch { }

				throw new GoogleLoginAuthorizationException(result);
			}
		}

		public void SetNumberOfWorkers(int value)
		{
			lock (workers)
			{
				//Remove some if we need to
				while (workers.Count > value)
				{
					workers[0].Stop();
					workers.RemoveAt(0);
				}

				//Add workers if we need to
				while (workers.Count < value)
				{
					var worker = new C2dmMessageTransportWorker();

					worker.Task = Task.Factory.StartNew(messageTransportWork,
					worker.CancelToken,
					TaskCreationOptions.LongRunning).ContinueWith((Task t) =>
					{
						//TODO: Log error

					}, TaskContinuationOptions.OnlyOnFaulted);

					workers.Add(worker);					
				}
			}
		}

		public int NumberOfWorkers
		{
			get { return workers.Count; }
		}

		public bool Running
		{
			get { return running; }
		}


		public void QueueMessage(string registrationId, NameValueCollection data, string collapseKey)
		{
			QueueMessage(registrationId, data, collapseKey, null);
		}

		public void QueueMessage(string registrationId, NameValueCollection data, string collapseKey, bool? delayWhileIdle)
		{
			QueueMessage(new C2dmMessage()
			{
				RegistrationId = registrationId,
				Data = data,
				CollapseKey = collapseKey,
				DelayWhileIdle = delayWhileIdle
			});
		}

		public void QueueMessage(C2dmMessage msg)
		{
			messages.Add(msg);
			//messages.Enqueue(msg);
		}

		public int QueueLength
		{
			get { return messages.Count; }
		}

		public void Start()
		{
			Start(1);
		}

		public void Start(int numberOfWorkers)
		{
			running = true;

			SetNumberOfWorkers(numberOfWorkers);
		}

		public void Stop()
		{
			running = false;

			SetNumberOfWorkers(0);
		}


		private void messageTransportWork(object state)
		{
			var cancelToken = (CancellationToken)state;

			while (!cancelToken.IsCancellationRequested)
			{
				bool sentWaitNotice = false;

				//Check to see if we should be waiting
				while (DateTime.UtcNow < waitUntil && !cancelToken.IsCancellationRequested)
				{
					//Check if we've sent notice that we're waiting this time yet or not
					// and send notice if not
					if (!sentWaitNotice && this.Waiting != null && !cancelToken.IsCancellationRequested)
					{
						sentWaitNotice = true;
						this.Waiting(waitUntil);
					}

					for (int i = 0; i < 10; i++)
					{
						if (!cancelToken.IsCancellationRequested)
							break;

						System.Threading.Thread.Sleep(100);
					}
				}

				C2dmMessage toSend = null;
				//if (messages.TryDequeue(out toSend))
				if (!cancelToken.IsCancellationRequested && (toSend = messages.Take()) != null)
				{
					try
					{
						var result = C2dmMessageTransport.Send(toSend, this.googleAuthToken, this.SenderID, this.ApplicationID);

						if (this.MessageSuccess != null)
							this.MessageSuccess(result);

						//Reset the retry
						if (lastRetryAfter.TotalSeconds > 0)
							lastRetryAfter = new TimeSpan(0, 0, 0);
					}
					catch (ServiceUnavailableTransportException suEx)
					{
						//Backoff the last retry timespan + new one
						lastRetryAfter += suEx.RetryAfter;

						//Set the time to wait until
						waitUntil = DateTime.UtcNow + lastRetryAfter;
					}
					catch (MessageTransportException mtEx)
					{
						if (this.MessageFailure != null)
							this.MessageFailure(mtEx);
					}
					catch (Exception ex)
					{

					}
				}
			}
		}
	}
}

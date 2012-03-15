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

namespace C2dmSharp.Server
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

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="senderID">Email address (google account) used to sign up for Google's C2DM application Whitelist</param>
        /// <param name="password">Password used to login with Email</param>
        /// <param name="applicationID">Application (Package Name) used to sign up for Google's C2DM Whitelist</param>
        public C2dmService(string senderID, string password, string applicationID)
        {
            init(senderID, password, applicationID);
        }


        private void init(string senderID, string password, string applicationID)
        {
            //Listens for headers back from google to update our auth token
            C2dmMessageTransport.UpdateGoogleClientAuthToken += delegate(string authToken)
            {
                this.googleAuthToken = authToken;
            };

            C2dmMessageTransportAsync.MessageResponseReceived += delegate(C2dmMessageTransportResponse resp)
            {
            };

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

        /// <summary>
        /// Explicitly refreshes the Google Auth Token.  Usually not necessary.
        /// </summary>
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

            try
            {
                var authStr = Encoding.ASCII.GetString(wc.UploadValues(authUrl, data));

                //Only care about the Auth= part at the end
                if (authStr.Contains("Auth="))
                    googleAuthToken = authStr.Substring(authStr.IndexOf("Auth=") + 5);
                else
                    throw new GoogleLoginAuthorizationException("Missing Auth Token");
            }
            catch (WebException ex)
            {
                var result = "Unknown Error";
                try { result = (new System.IO.StreamReader(ex.Response.GetResponseStream())).ReadToEnd(); }
                catch { }

                throw new GoogleLoginAuthorizationException(result);
            }
        }

        /// <summary>
        /// Increase or decrease the number of workers to process queued messages.
        /// </summary>
        /// <param name="value">New Value for the number of workers to use</param>
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

        /// <summary>
        /// How many worksers are currently running
        /// </summary>
        public int NumberOfWorkers
        {
            get { return workers.Count; }
        }

        /// <summary>
        /// Is the Service Running or not
        /// </summary>
        public bool Running
        {
            get { return running; }
        }

        /// <summary>
        /// Queues a new C2DM Message to be sent
        /// </summary>
        /// <param name="registrationId">Registration ID of the Device</param>
        /// <param name="data">Key/Value Collection of data or 'extras' to send</param>
        /// <param name="collapseKey">Collapse Key</param>
        public void QueueMessage(string registrationId, NameValueCollection data, string collapseKey)
        {
            QueueMessage(registrationId, data, collapseKey, null);
        }

        /// <summary>
        /// Queues a new C2DM Message to be sent
        /// </summary>
        /// <param name="registrationId">Registration ID of the Device</param>
        /// <param name="data">Key/Value Collection of data or 'extras' to send</param>
        /// <param name="collapseKey">Collapse Key</param>
        /// <param name="delayWhileIdle">If true, C2DM will only be delivered once the device's screen is on</param>
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

        /// <summary>
        /// Queues a new C2DM Message to be sent
        /// </summary>
        /// <param name="msg">Constructed C2dmMessage parameter</param>
        public void QueueMessage(C2dmMessage msg)
        {
            messages.Add(msg);
            //messages.Enqueue(msg);
        }

        /// <summary>
        /// How many messages are left in the queue to be sent
        /// </summary>
        public int QueueLength
        {
            get { return messages.Count; }
        }

        public void Start()
        {
            Start(1);
        }

        /// <summary>
        /// Starts the specified number of workers ready to send queued messages
        /// </summary>
        /// <param name="numberOfWorkers">Number of Workers</param>
        public void Start(int numberOfWorkers)
        {
            running = true;

            SetNumberOfWorkers(numberOfWorkers);
        }

        /// <summary>
        /// Stops all workers and the service, without waiting for queued messages to be sent.
        /// </summary>
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
                try
                {
                    if (cancelToken.IsCancellationRequested || (toSend = messages.Take(cancelToken)) == null) continue;
                }
                catch (OperationCanceledException)
                {
                    continue;
                }

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
                    //Enqueue the message again to resend later
                    QueueMessage(toSend);

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
                    //TODO: Log Error
                }

            }
        }
    }
}

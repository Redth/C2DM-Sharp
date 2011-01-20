using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace C2dmSharp
{
	public class C2dmService
	{
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
			running = false;
			//messages = new ConcurrentQueue<Message>();
			messages = new BlockingCollection<C2dmMessage>();
			workers = new List<C2dmMessageTransportWorker>();
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
						var result = C2dmMessageTransport.Send(toSend);

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

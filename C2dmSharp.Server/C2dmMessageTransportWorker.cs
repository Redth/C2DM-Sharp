using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace C2dmSharp
{
	internal class C2dmMessageTransportWorker
	{
		CancellationTokenSource cancelTokenSource;

		public C2dmMessageTransportWorker()
		{
			this.Id = Guid.NewGuid().ToString();
			this.cancelTokenSource = new CancellationTokenSource();
		}

		public string Id
		{
			get;
			private set;
		}

		public void Stop()
		{
			this.cancelTokenSource.Cancel();
			this.Task.Wait();
		}

		public CancellationToken CancelToken
		{
			get { return cancelTokenSource.Token; }
		}

		public Task Task
		{
			get;
			set;
		}
	}
}

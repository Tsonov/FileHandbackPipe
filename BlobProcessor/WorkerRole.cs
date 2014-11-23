using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;

namespace BlobProcessor
{
    public class WorkerRole : RoleEntryPoint
    {
        private const int minBackoffSeconds = 1;
        private const int maxBackoffSeconds = 120;
        private const int exponent = 2;
        private int backoffWaitSeconds = minBackoffSeconds;

        public override void Run()
        {
            Trace.TraceInformation("BlobProcessor is running");
            while (true)
            {
                if (false)
                {
                    Trace.WriteLine("BlobProcessor has found work to process, running");
                    // TODO: Add queue processing. And stuff.

                    backoffWaitSeconds = minBackoffSeconds;
                    Trace.WriteLine(string.Format("BlobProcessor resetting the back off to {0} seconds", backoffWaitSeconds));
                }
                else
                {
                    Trace.WriteLine(string.Format("BlobProcessor backing off for {0} seconds", backoffWaitSeconds));
                    Thread.Sleep(TimeSpan.FromSeconds(backoffWaitSeconds));
                    backoffWaitSeconds = Math.Min(backoffWaitSeconds * exponent, maxBackoffSeconds);
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            bool result = base.OnStart();

            Trace.TraceInformation("BlobProcessor has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("BlobProcessor is stopping");

            base.OnStop();

            Trace.TraceInformation("BlobProcessor has stopped");
        }
    }
}

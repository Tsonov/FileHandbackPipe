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
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.IO.Compression;
using System.IO;

namespace BlobProcessor
{
    public class WorkerRole : RoleEntryPoint
    {
        private const int MinutesBeforeReleasingMessage = 5;
        private const int MinBackoffSeconds = 1;
        private const int MaxBackoffSeconds = 120;
        private const int Exponent = 2;
        private int backoffWaitSeconds = MinBackoffSeconds;

        private CloudQueueClient queueClient = null;
        private CloudQueueClient QueueClient
        {
            get
            {
                return queueClient;
            }
            set
            {
                if (queueClient != null)
                {
                    throw new InvalidOperationException("Attempt to set the queue client twice");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Provided queue client is null");
                }
                queueClient = value;
            }
        }

        private CloudBlobClient blobClient = null;
        private CloudBlobClient BlobClient
        {
            get
            {
                return blobClient;
            }
            set
            {
                if (blobClient != null)
                {
                    throw new InvalidOperationException("Attempt to set the blob client twice");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value", "Provided blob client is null");
                }
                blobClient = value;
            }
        }


        public override void Run()
        {
            Trace.TraceInformation("BlobProcessor is getting the storage communicator objects");
            var handbackQueue = QueueClient.GetQueueReference(
                CloudConfigurationManager.GetSetting("Storage.HbQueue"));
            var handbackContainer = BlobClient.GetContainerReference(
                CloudConfigurationManager.GetSetting("Storage.HbContainer"));
            var processedContainer = BlobClient.GetContainerReference(
                CloudConfigurationManager.GetSetting("Storage.ProcessedContainer"));
            

            Trace.TraceInformation("BlobProcessor is running");
            while (true)
            {
                var nextMessage = handbackQueue.GetMessage(TimeSpan.FromMinutes(MinutesBeforeReleasingMessage));
                if (nextMessage != null)
                {
                    Trace.WriteLine("BlobProcessor has found work to process, running");

                    string blobName = nextMessage.AsString;
                    // TODO: Better validate name
                    blobName = blobName.Trim();
                    var blob = handbackContainer.GetBlockBlobReference(blobName);
                    if (blob.Exists())
                    {
                        Trace.WriteLine("Blob with name {0} was found, processing", blobName);

                        using (var blobStream = blob.OpenRead())
                        {
                            
                            Trace.WriteLine("Checking blob for viruses");
                            bool isInfected;
                            using (FileScannerClient fileScanner = new FileScannerClient())
                            {
                                isInfected = fileScanner.TestForViruses(blobStream);
                            }

                            if (isInfected)
                            {
                                Trace.TraceError("Blob {0} was infected! Deleting from blob storage and stopping further processing", blobName);
                            }
                            else
                            {
                                Trace.WriteLine("Blob wasn't infected");
                                Trace.WriteLine("Compressing blob");
                                blobStream.Seek(0, SeekOrigin.Begin);
                                // Intermediate stream GZip can write to
                                using (var compressedStream = new MemoryStream())
                                {
                                    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Fastest))
                                    {
                                        blobStream.CopyTo(gzipStream);
                                        gzipStream.Flush();
                                        // Rewind the stream to read from it
                                        compressedStream.Seek(0, SeekOrigin.Begin);

                                        Trace.WriteLine("Successfully compressed blob.");

                                        Trace.WriteLine("Uploading blob to processed blobs");
                                        var compressedBlob = processedContainer.GetBlockBlobReference(blobName);
                                        // Assume overwrite if it's already there.
                                        compressedBlob.UploadFromStream(compressedStream);
                                        Console.WriteLine("Finished processing blob {0}", blobName);
                                    }
                                }
                            }
                        }
                        Trace.WriteLine("Deleting blob {0}", blobName);
                        // Delete the source blob since we are done with it
                        blob.Delete();
                    }
                    else
                    {
                        Trace.TraceError("Blob with name {0} did not exist in the blob storage. Skipping and deleting message.", blobName);
                    }

                    // Delete the message to finish processing
                    handbackQueue.DeleteMessage(nextMessage);

                    backoffWaitSeconds = MinBackoffSeconds;
                    Trace.WriteLine(string.Format("BlobProcessor resetting the back off to {0} seconds", backoffWaitSeconds));
                }
                else
                {
                    Trace.WriteLine(string.Format("BlobProcessor backing off for {0} seconds", backoffWaitSeconds));
                    Thread.Sleep(TimeSpan.FromSeconds(backoffWaitSeconds));
                    backoffWaitSeconds = Math.Min(backoffWaitSeconds * Exponent, MaxBackoffSeconds);
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += RoleEnvironment_Changing;

            Trace.TraceInformation("BlobProcessor is initializing");
            string accountName = CloudConfigurationManager.GetSetting("Storage.AccountName");
            string sas = CloudConfigurationManager.GetSetting("Storage.AccountSAS");
            StorageCredentials credentials = new StorageCredentials(accountName, sas);
            CloudStorageAccount account = new CloudStorageAccount(credentials, useHttps: true);
            QueueClient = account.CreateCloudQueueClient();
            BlobClient = account.CreateCloudBlobClient();

            Trace.TraceInformation("BlobProcessor has been started");
            return base.OnStart();
        }

        void RoleEnvironment_Changing(object sender, RoleEnvironmentChangingEventArgs e)
        {
            // Implements the changes after restarting the role instance (connection details might be updated)
            if ((e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange)))
            {
                e.Cancel = true;
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("BlobProcessor is stopping");

            base.OnStop();

            Trace.TraceInformation("BlobProcessor has stopped");
        }
    
    }
}

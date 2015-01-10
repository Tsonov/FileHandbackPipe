using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using VirusScanner;

namespace BlobProcessor
{
    internal class FileScannerClient : ClientBase<IFileScannerService>
    {
        public FileScannerClient()
            : base("BasicHttpEndpoint_IFileScannerService")
        {

        }

        public bool TestForViruses(System.IO.Stream input)
        {
            return Channel.TestForViruses(input).Result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace VirusScanner
{
    [ServiceContract]
    public interface IFileScannerService
    {
        // TODO: Composite return type with some message + result + threats found
        // TODO: Use messages if more than the file itself is needed.
        [OperationContract]
        Task<bool> TestForViruses(Stream input);
    }
}

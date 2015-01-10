using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirusScanning
{
    /// <summary>
    /// Uses the Microsoft Security Essentials that comes pre-installed in Azure VMs/Cloud services
    /// </summary>
    /// <remarks>
    /// By default, Azure MSE is disabled in cloud service VMs and is an extension to standard VMs.
    /// In order to use this scanner, Azure MSE must be enabled first.
    /// </remarks>
    public class AzureMSEVirusScanner : MicrosoftSEVirusScanner
    {
        private static readonly string ProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        // By default, MpCmdRun.exe is in a different location in Azure. It's not under "Antimalware" as when installed by System Center, but in the main folder itself.
        private static readonly string PathToAzureVirusScanningExe = 
            Path.Combine(ProgramFilesPath, "Microsoft Security Client", "MpCmdRun.exe");

        public AzureMSEVirusScanner()
            : base(PathToAzureVirusScanningExe)
        {

        }
    }
}

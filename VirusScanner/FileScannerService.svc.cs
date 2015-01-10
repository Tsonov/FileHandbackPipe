using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using VirusScanning;

namespace VirusScanner
{
    public class FileScannerService : IFileScannerService
    {
        private const int BufferSize = 65555;

        public async Task<bool> TestForViruses(Stream input)
        {
            using (input)
            {
                // TODO: Most of this should be done only once. Only the file name is dynamic.
                LocalResource fileStorage = RoleEnvironment.GetLocalResource("TempFileStorage");
                string tempFileName = String.Format("{0}.tmp", Guid.NewGuid().ToString());
                string filePath = Path.Combine(fileStorage.RootPath, tempFileName);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string pathToExe = Path.Combine(programFiles, "Microsoft Security Client", "MpCmdRun.exe");

                using (FileStream writer = File.Create(filePath))
                {
                    // Free up the thread while copying
                    await input.CopyToAsync(writer, BufferSize);
                }
                try
                {
                    IFileVirusScanner scanner = new AzureMSEVirusScanner();

                    bool result = scanner.IsInfected(filePath);


                    return result;
                }
                finally
                {
                    // The file scanner should not clean the file, but always good to check before deleting
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VirusScanning
{
    /// <summary>
    /// Scans files for viruses using the Microsoft Security Essentials virus scanner (MpCmdRun.exe).
    /// </summary>
    /// <remarks>
    /// In order for this scanner to work, some version of MSE must be available and enabled.
    /// </remarks>
    public class MicrosoftSEVirusScanner : IFileVirusScanner
    {
        // -Scan starts the scan
        // -ScanType 3 is a custom scan
        // -File <path> to provide the target
        // -DisableRemediation will ignore exclusions but also do no clean-up operations.
        private const string ScannerArgumentListFormat = "-Scan -Scantype 3 -File \"{0}\" -DisableRemediation";
        private readonly string _antimalwareExecutableLocation;
        private readonly int _timeoutSeconds = 5;

        public MicrosoftSEVirusScanner(string pathToAntimalwareExecutable)
        {
            if (pathToAntimalwareExecutable == null)
            {
                throw new ArgumentNullException("pathToAntimalwareExecutable", "The provided folder path for the antimalware exe can't be null");
            }
            if (!File.Exists(pathToAntimalwareExecutable))
            {
                throw new ArgumentException("Invalid path to MpCmdRun.exe, no file found at the provided location " + pathToAntimalwareExecutable);
            }
            this._antimalwareExecutableLocation = pathToAntimalwareExecutable;
        }

        public bool IsInfected(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath", "The provided path for file scanning can't be null");
            }
            Process proc = null;
            try
            {
                // TODO: This should probably be async to avoid blocking the thread while waiting but a wrapper is needed for starting the process...
                proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _antimalwareExecutableLocation,
                        Arguments = String.Format(ScannerArgumentListFormat, filePath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit(_timeoutSeconds * 1000); // Expects milliseconds

                while (!proc.StandardOutput.EndOfStream)
                {
                    string result = proc.StandardOutput.ReadToEnd();
                    //Successful message will contain: "Scan finished. .* found no threats."

                    // TODO: Make the reporting more robust. E.g. find "found X threats" and report that. 
                    if (result.Contains("found no threats"))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                if (!proc.HasExited)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(30000); //The Kill method executes asynchronously. After calling the Kill method, call the WaitForExit method to wait (30 sec to be safe) for the process to exit

                        throw new Exception();
                    }
                    catch (InvalidOperationException)
                    {
                        //Ignore as the process has already exited. 
                    }
                }
                return false;
            }
            finally
            {
                if (proc != null)
                {
                    proc.Dispose();
                }
            }
        }
    }
}

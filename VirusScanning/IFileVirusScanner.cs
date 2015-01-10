using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirusScanning
{
    /// <summary>
    /// Provides the means to scan a file for viruses
    /// </summary>
    public interface IFileVirusScanner
    {
        bool IsInfected(string filePath);
    }
}

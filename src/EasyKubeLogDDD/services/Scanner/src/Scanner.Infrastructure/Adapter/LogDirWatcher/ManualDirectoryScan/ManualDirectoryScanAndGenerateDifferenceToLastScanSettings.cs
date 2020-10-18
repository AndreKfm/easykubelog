namespace Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan
{
    public class ManualDirectoryScanAndGenerateDifferenceToLastScanSettings
    {
        public ManualDirectoryScanAndGenerateDifferenceToLastScanSettings(string directory)
        {
            ScanDirectory = directory;
        }



        public string ScanDirectory { get; private set; }
    }
}
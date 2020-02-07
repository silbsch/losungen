using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;

namespace LosungenUwp
{
    public class Losungen
    {
        private readonly string _fileName;
        public Losungen(int year)
        {
            Items = new ObservableCollection<LosungsItem>();
            //BindingOperations.EnableCollectionSynchronization(Items, _lock);

            _fileName = $"Losung_{year}_XML.zip";

            Initialize();
        }

        public ObservableCollection<LosungsItem> Items { get; }

        public Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<DownloadOperation> progress)
        {
            return Task.Run(async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var file = await DownloadFileAsync(cancellationToken, progress);
                Initialize(file, cancellationToken);
            }, cancellationToken);
        }

        private IAsyncOperation<StorageFile> GetLocalFile()
        {
            return ApplicationData.Current.RoamingFolder.GetFileAsync(_fileName);
        }

        private IAsyncOperation<StorageFile> CreateLocalFile(CreationCollisionOption option=CreationCollisionOption.ReplaceExisting)
        {
            return ApplicationData.Current.RoamingFolder.CreateFileAsync(_fileName, option);
        }

        private async Task<string> DownloadFileAsync(CancellationToken cancellationToken,
            IProgress<DownloadOperation> progress)
        {
            var remoteFile = $"https://www.losungen.de/fileadmin/media-losungen/download/{_fileName}";
            //var webClient = new WebClient();

            //cancellationToken.Register(() => webClient.CancelAsync());

            //if (progress != null)
            //{
            //    webClient.DownloadProgressChanged += (s, y) =>
            //    {
            //        progress.Report(y);
            //    };
            //}
            //try
            //{
            //    await webClient.DownloadFileTaskAsync(remoteFile, _localFileName);
            //    return new FileInfo(_localFileName);
            //}
            //catch (Exception)
            //{
            //    return null;
            //}

            var file = await CreateLocalFile();
            var durl = new Uri(remoteFile);
            var backgroundDownloader = new BackgroundDownloader();
            var downloadOperation = backgroundDownloader.CreateDownload(durl, file);
            //Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progressChanged);

            try
            {
                await downloadOperation.StartAsync().AsTask(cancellationToken, progress);
                return downloadOperation.ResultFile.Path;
            }
            catch (TaskCanceledException)
            {

                await downloadOperation.ResultFile.DeleteAsync();
                downloadOperation = null;

            }
            return null;
        }

        private XDocument UnZipLosungen(string zipFileName)
        {
            if (File.Exists(zipFileName))
            {
                using (var zipArchive = ZipFile.Open(zipFileName, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                    {
                        if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var sr = new StreamReader(entry.Open()))
                            {
                                return XDocument.Load(sr);
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async void Initialize()
        {
            try
            {
                var file = await GetLocalFile();
                if (file.IsAvailable)
                {
                    Initialize(file.Path, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                
            }
            finally
            {
                
            }
        }

        private void Initialize(string zipFile, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var xml = UnZipLosungen(zipFile);
            Items.Clear();
            foreach (var xElement in xml.Descendants("Losungen"))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                Items.Add(new LosungsItem(xElement));
            }
        }
        
    }

    public class LosungsItem
    {
        public LosungsItem(XElement xElement)
        {
            Day = DateTime.Parse(xElement.Element("Datum")?.Value ?? "");
            Losungstext = xElement.Element("Losungstext")?.Value;
            Losungsvers = xElement.Element("Losungsvers")?.Value;
            Lehrtext = xElement.Element("Lehrtext")?.Value;
            Lehrtextvers = xElement.Element("Lehrtextvers")?.Value;
            
            Sonntag = xElement.Element("Sonntag")?.Value;
        }

        public DateTime Day { get; }

        public string DisplayDay => Day.ToString("dd. MM. yyyy - dddd", CultureInfo.CurrentCulture);

        public string Losungstext { get; }
        public string Losungsvers { get; }

        public string Lehrtext { get; }
        public string Lehrtextvers { get; }

        public string Sonntag { get; }

        /*
    <Losungstext>Nicht hat euch der HERR angenommen und euch erwählt, weil ihr größer wäret als alle Völker – denn du bist das kleinste unter allen Völkern –, sondern weil er euch geliebt hat.</Losungstext>
    <Losungsvers>5.Mose 7,7-8</Losungsvers>
    <Lehrtext>Was gering ist vor der Welt und was verachtet ist, das hat Gott erwählt, was nichts ist, damit er zunichtemache, was etwas ist, auf dass sich kein Mensch vor Gott rühme.</Lehrtext>
    <Lehrtextvers>1.Korinther 1,28-29</Lehrtextvers>
    */
    }

    interface IApplicationDataFolder
    {
        string GetFolderName { get; }
    }
}

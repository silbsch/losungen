using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Losungen
{
    public class Losungen
    {
        private readonly string _fileName;
        private readonly string _localFileName;
        public Losungen(int year)
        {
            Items=new ObservableCollection<LosungsItem>();
            //BindingOperations.EnableCollectionSynchronization(Items, _lock);

            _fileName = $"Losung_{year}_XML.zip";
            _localFileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _fileName);

            Initialize(new FileInfo(_localFileName), CancellationToken.None);
        }

        public ObservableCollection<LosungsItem> Items { get; }

        public Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
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

        private async Task<FileInfo> DownloadFileAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {
            var remoteFile = $"https://www.losungen.de/fileadmin/media-losungen/download/{_fileName}";
            var webClient = new WebClient();

            cancellationToken.Register(() => webClient.CancelAsync());

            if (progress != null)
            {
                webClient.DownloadProgressChanged += (s, y) =>
                {
                    progress.Report(y);
                };
            }
            try
            {
                await webClient.DownloadFileTaskAsync(remoteFile, _localFileName);
                return new FileInfo(_localFileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private XDocument UnZipLosungen(FileInfo file)
        {
            
            using (var zipArchive = ZipFile.Open(file.FullName,ZipArchiveMode.Read))
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

            return null;
        }

        private void Initialize(FileInfo zipFile, CancellationToken cancellationToken)
        {
                if (!(zipFile?.Exists ?? false) || cancellationToken.IsCancellationRequested)
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
}

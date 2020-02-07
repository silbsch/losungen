using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LosungenStandard
{
    public class Losungen
    {
        private readonly string _fileName;
        private readonly string _localFileName;

        public Losungen(int year)
        {
            _losungsItems = new List<LosungsItem>();
            _fileName = $"Losung_{year}_XML.zip";
            _localFileName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                _fileName);
        }

        private readonly List<LosungsItem> _losungsItems;
        public IEnumerable<LosungsItem> Items => _losungsItems;

        public bool IsInitialzed { get; private set; }

        public Task<IEnumerable<LosungsItem>> InitializeAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {

            return IsInitialzed
                ? Task.FromResult(Items)
                : Task.Run(async () =>
                {
                    //await Task.Delay(10000);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var file = await DownloadFileAsync(cancellationToken, progress);
                        Initialize(file, cancellationToken);
                    }

                    return Items;
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
            if (IsInitialzed || !(zipFile?.Exists ?? false) || cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            IsInitialzed = true;

            var xml = UnZipLosungen(zipFile);
            _losungsItems.Clear();
            foreach (var xElement in xml.Descendants("Losungen"))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _losungsItems.Add(new LosungsItem(xElement));
            }
        }
    }

    public class LosungsItem
    {
        public LosungsItem(XElement xElement)
        {
            Day=DateTime.Parse(xElement.Element("Datum")?.Value??"");
            Losungstext = xElement.Element("Losungstext")?.Value;
            Losungsvers = xElement.Element("Losungsvers")?.Value;
            Lehrtext = xElement.Element("Lehrtext")?.Value;
            Lehrtextvers = xElement.Element("Lehrtextvers")?.Value;

            Sonntag = xElement.Element("Sonntag")?.Value;
        }

        public DateTime Day { get; }
    
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
}

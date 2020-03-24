using LiteDB;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;

namespace Losungen.Standard
{
    public interface IDataServiceLocator
    {
        string GetDatabaseFolder();
    }

    class DataService
    {
        private static readonly Lazy<DataService> Lazy = new Lazy<DataService>(true);


        public static DataService Instance => Lazy.Value;

        public async Task<XDocument> GetLosungDocumentAsync(int year, CancellationToken cancellationToken,IProgress<DownloadProgressChangedEventArgs> progress)
        {
            XDocument xDocument = null;
            using (var db = new LiteDatabase(GetDatabaseFile()))
            {

                var collection = db.GetCollection<DataStoreItem>();
                var item = collection.FindById(year);
                if (item == null)
                {
                    var zip = await DownloadFileAsync(year, cancellationToken, progress);
                    if (zip != null)
                    {
                        xDocument = UnZipLosungen(zip);
                        //db.FileStorage.Upload()
                        collection.Insert(year, new DataStoreItem(year, xDocument));
                        zip.Delete();
                    }
                }
                else
                {
                    xDocument = DataStoreItem.Base64ToDocument(item.Base64Xml);
                }
            }

            return xDocument;
        }

        private async Task<FileInfo> DownloadFileAsync(int year, CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {
            var remoteFile = $"https://www.losungen.de/fileadmin/media-losungen/download/Losung_{year}_XML.zip";
            var localFile = Path.Combine(GetDataFolder(), $"Losung_{year}_XML.zip");
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
                await webClient.DownloadFileTaskAsync(remoteFile, localFile);
                return new FileInfo(localFile);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private XDocument UnZipLosungen(FileInfo file)
        {

            using (var zipArchive = ZipFile.Open(file.FullName, ZipArchiveMode.Read))
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
        
        private string GetDataFolder()
        {
            string path;
            try
            {
                var subFolder = "Losungen";

                var folder = DependencyService.Get<IDataServiceLocator>()?.GetDatabaseFolder();
                if (string.IsNullOrEmpty(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }

                path = Path.Combine(folder, subFolder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return path;
        }

        private string GetDatabaseFile()=> Path.Combine(GetDataFolder(), "losungen.lid");
    }

    public class DataStoreItem
    {
        public DataStoreItem()
        {
        }

        public DataStoreItem(int id, XDocument document)
        {
            Id = id;
            Base64Xml = DocumentToBase64(document);
        }

        public int Id { get; set; }

        public string Base64Xml { get; set; }

        public static string DocumentToBase64(XDocument document)
            => Convert.ToBase64String(Encoding.Unicode.GetBytes(document.ToString()));

        public static XDocument Base64ToDocument(string text)
            => XDocument.Parse(Encoding.Unicode.GetString(Convert.FromBase64String(text)));
    }

}

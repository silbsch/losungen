using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Losungen.Standard
{
    public class Losungen
    {
        private static readonly object LockObject = new object();


        public Losungen(int year)
        {
            Year = year;
            _losungsItems = new List<LosungItem>();
        }

        public int Year { get; }

        private readonly List<LosungItem> _losungsItems;

        public IEnumerable<LosungItem> Items
        {
            get
            {
                lock (LockObject)
                {
                    return _losungsItems;
                }
            }
        }

        public bool IsInitialzed { get; private set; }

        public Task<IEnumerable<LosungItem>> GetLosungItemsAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {

            return IsInitialzed
                ? Task.FromResult(Items)
                : Task.Run(async () =>
                {
                    //await Task.Delay(10000);
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var xdoc = await DataService.Instance.GetLosungDocumentAsync(Year, cancellationToken, progress);
                        Initialize(xdoc, cancellationToken);
                    }

                    return Items;
                }, cancellationToken);
        }

        private void Initialize(XDocument xdoc, CancellationToken cancellationToken)
        {
            if (IsInitialzed || cancellationToken.IsCancellationRequested)
            {
                return;
            }


            lock (LockObject)
            {
                if (xdoc != null)
                {
                    _losungsItems.Clear();
                    _losungsItems.AddRange(xdoc.Descendants("Losungen").Select(xElement => new LosungItem(xElement)));
                }

                IsInitialzed = true;
            }
        }
    }
}

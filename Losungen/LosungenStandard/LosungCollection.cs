using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LosungenStandard
{
    public class LosungCollection
    {
        private readonly ConcurrentDictionary<int, Losungen> _losungen;
        private readonly ObservableCollection<LosungsItem> _items;
        public LosungCollection()
        {
            _losungen = new ConcurrentDictionary<int, Losungen>();
            _items = new ObservableCollection<LosungsItem>();
            Items = new ReadOnlyObservableCollection<LosungsItem>(_items);
        }

        public ReadOnlyObservableCollection<LosungsItem> Items { get;}

        public Task InitialiseAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {
            var year = DateTime.Now.Year;
            return InitialLosungAsync(year, cancellationToken, progress);
        }

        public LosungsItem Today => _items.FirstOrDefault(i => i.Day == DateTime.Today);

        public Task<LosungsItem> NextSunday(LosungsItem fromThisDay, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            return OnSundayExecute(fromThisDay, true, cancellationToken, progress);
        }

        public Task<LosungsItem> PrevSunday(LosungsItem fromThisDay, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            return OnSundayExecute(fromThisDay, false, cancellationToken, progress);
        }

        private async Task InitialLosungAsync(int year, CancellationToken cancellationToken,
        IProgress<DownloadProgressChangedEventArgs> progress)
        {
            if (!_losungen.TryGetValue(year, out var losung))
            {
                losung = new Losungen(year);
                _losungen.TryAdd(year, losung);
            }

            if (!losung.IsInitialzed)
            {
                var items= await losung.InitializeAsync(cancellationToken, progress);
                foreach (var item in items) _items.Add(item);
            }
        }

        private async Task<LosungsItem> OnSundayExecute(LosungsItem fromThisDay, bool nextSunday, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            LosungsItem rv=null;
            try
            {
                var date = fromThisDay?.Day ?? DateTime.Today;

                var sunday = nextSunday
                    ? date.AddDays(7 - (int)date.DayOfWeek)
                    : date.AddDays(date.DayOfWeek == DayOfWeek.Sunday
                        ? -7
                        : -(int)date.DayOfWeek);

                rv = Items.FirstOrDefault(i => i.Day == sunday);

                if (rv==null && !_losungen.ContainsKey(sunday.Year))
                {
                    await InitialLosungAsync(sunday.Year, cancellationToken, progress);
                    rv = await OnSundayExecute(fromThisDay, nextSunday, cancellationToken, progress);
                }
            }
            catch
            {
                //ToDo
            }

            return rv;
        }
    }
}

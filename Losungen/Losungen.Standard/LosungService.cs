using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Losungen.Standard
{
    public interface ILosungService
    {
        ReadOnlyObservableCollection<LosungItem> Items { get; }

        Task InitialiseAsync(CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress);

        LosungItem Today { get; }

        Task<LosungItem> GetDayAsync(DateTime day, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress);

        Task<LosungItem> NextSunday(LosungItem fromThisDay, CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress);

        Task<LosungItem> PrevSunday(LosungItem fromThisDay, CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress);
    }

    public class LosungService: ILosungService
    {
        private readonly ConcurrentDictionary<int, Losungen> _losungen;
        private readonly ObservableCollection<LosungItem> _items;
        public LosungService()
        {
            _losungen = new ConcurrentDictionary<int, Losungen>();
            _items = new ObservableCollection<LosungItem>();
            Items = new ReadOnlyObservableCollection<LosungItem>(_items);

        }

        public ReadOnlyObservableCollection<LosungItem> Items { get;}

        public Task InitialiseAsync(CancellationToken cancellationToken,
            IProgress<DownloadProgressChangedEventArgs> progress)
        {
            var year = DateTime.Now.Year;
            return InitialLosungAsync(year, cancellationToken, progress);
        }

        public LosungItem Today => _items.FirstOrDefault(i => i.Day == DateTime.Today);

        public async Task<LosungItem> GetDayAsync(DateTime day, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            if(_losungen.TryGetValue(day.Year, out var l))
            {
                return (await l.GetLosungItemsAsync(cancellationToken, progress)).FirstOrDefault(i => i.Day == day);
            }

            await InitialLosungAsync(day.Year, cancellationToken, progress);
            return await GetDayAsync(day, cancellationToken, progress);
        }

        public Task<LosungItem> NextSunday(LosungItem fromThisDay, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            return OnSundayExecute(fromThisDay, true, cancellationToken, progress);
        }

        public Task<LosungItem> PrevSunday(LosungItem fromThisDay, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            return OnSundayExecute(fromThisDay, false, cancellationToken, progress);
        }

        private async Task InitialLosungAsync(int year, CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            if (!_losungen.TryGetValue(year, out var losung))
            {
                losung = new Losungen(year);
                _losungen.TryAdd(year, losung);
            }

            if (!losung.IsInitialzed)
            {
                var items= await losung.GetLosungItemsAsync(cancellationToken, progress);
                var itemsArray = items.ToArray();
                for (var i = 0; i < itemsArray.Length; i++)
                {
                    _items.Insert(i, itemsArray[i]);
                }

                //foreach (var item in items) _items.Add(item);
            }
        }

        private Task<LosungItem> OnSundayExecute(LosungItem fromThisDay, bool nextSunday,
            CancellationToken cancellationToken, IProgress<DownloadProgressChangedEventArgs> progress)
        {
            try
            {
                var date = fromThisDay?.Day ?? DateTime.Today;

                var sunday = nextSunday
                    ? date.AddDays(7 - (int) date.DayOfWeek)
                    : date.AddDays(date.DayOfWeek == DayOfWeek.Sunday
                        ? -7
                        : -(int) date.DayOfWeek);

                return GetDayAsync(sunday, cancellationToken, progress);

            }
            catch(Exception ex)
            {
                return Task.FromException<LosungItem>(ex);
            }
        }
    }
}

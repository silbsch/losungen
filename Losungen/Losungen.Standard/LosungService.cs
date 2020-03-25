using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Losungen.Standard
{
    public interface ILosungService
    {
        event EventHandler ItemsChanged;

        IEnumerable<LosungItem> Items { get; }

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
        public event EventHandler ItemsChanged;

        private readonly ConcurrentDictionary<int, Losungen> _losungen;

        public LosungService()
        {
            _losungen = new ConcurrentDictionary<int, Losungen>();
            Items = Enumerable.Empty<LosungItem>();

        }

        private IEnumerable<LosungItem> _items;
        public IEnumerable<LosungItem> Items { get =>_items;
            private set
            {
                _items = value;
                ItemsChanged?.Invoke(this,EventArgs.Empty);
            }
        }

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

            var items = new List<LosungItem>();
            foreach (var keyValue in _losungen.OrderBy(kv => kv.Key))
            {
                items.AddRange(await keyValue.Value.GetLosungItemsAsync(cancellationToken, progress));
            }

            Items = items;
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

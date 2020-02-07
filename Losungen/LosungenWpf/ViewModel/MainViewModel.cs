using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using LosungenStandard;
using Prism.Commands;
using Prism.Mvvm;

namespace LosungenWpf.ViewModel
{
    public class MainViewModel : BindableBase
    {
        private readonly object _lock = new object();

        //private readonly Losungen _losungen;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<int, Losungen> _losungen;

        public MainViewModel()
        {
            _losungen = new Dictionary<int, Losungen>();
            Items = new ObservableCollection<LosungsItem>();
            BindingOperations.EnableCollectionSynchronization(Items, _lock);
        }

        public ObservableCollection<LosungsItem> Items { get; }

        public ICommand FillCommand => new DelegateCommand(
            async () => { await InitialLosungAsync(DateTime.Now.Year); }, 
            () => !IsBusy).ObservesProperty(() => IsBusy);


        public ICommand CancellationCommand => new DelegateCommand(() =>
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch
            {
                StateText = "Konnte Download nicht abbrechen!";
            }

        }).ObservesCanExecute(() => IsBusy);

        public ICommand ClipboardCommand => new DelegateCommand(() =>
        {
            try
            {
                if (SelectedLosung != null)
                {
                    Clipboard.SetData(DataFormats.Text, LosungToClipboard(SelectedLosung));
                }
            }
            catch
            {
                StateText = "Konnte Losung nicht in Zwischenablage kopieren!";
            }

        }, () => SelectedLosung != null
            ).ObservesProperty(() => SelectedLosung);

        public ICommand NextSundayCommand => new DelegateCommand(async() => await OnSundayExecute(true),
            () => !IsBusy).ObservesProperty(() => IsBusy);

        public ICommand PrevSundayCommand => new DelegateCommand(async() => await OnSundayExecute(false));

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        private ICollectionView _view;
        public ICollectionView View => _view ?? (_view = CreateCollectionView());

        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value, () =>
                {

                    if (string.IsNullOrEmpty(_filterText) || !View.CanFilter)
                    {
                        View.Filter = null;
                    }
                    else
                    {
                        View.Filter = Filter;
                    }
                    RaisePropertyChanged(nameof(FilteredElements));
                });
            }
        }

        public int FilteredElements => View.Cast<LosungsItem>().Count();

        private LosungsItem _selectedLosung;

        public LosungsItem SelectedLosung
        {
            get => _selectedLosung;
            set => SetProperty(ref _selectedLosung, value);
        }

        private string _stateText;

        public string StateText
        {
            get => _stateText;
            private set => SetProperty(ref _stateText, value);
        }
        private ICollectionView CreateCollectionView()
        {
            Task.Run(async () =>
                {
                    await InitialLosungAsync(DateTime.Now.Year);
                    SelectedLosung = Items.FirstOrDefault(l => l.Day.Date == DateTime.Today);
                }
            );

            var v = CollectionViewSource.GetDefaultView(Items);

            using (v.DeferRefresh())
            {
                v.SortDescriptions.Clear();
                var sd = new SortDescription(nameof(LosungsItem.Day), ListSortDirection.Ascending);
                v.SortDescriptions.Add(sd);
            }
            v.MoveCurrentTo(SelectedLosung);
            return v;
        }

        private async Task InitialLosungAsync(int year)
        {
            IsBusy = true;
            if(!_losungen.TryGetValue(year, out var losung))
            {
                 losung = new Losungen(year);
                 _losungen[year] = losung;
            }

            if (!losung.IsInitialzed)
            {
                try
                {
                    using (_cancellationTokenSource = new CancellationTokenSource())
                    {
                        var p = new Progress<DownloadProgressChangedEventArgs>(args =>
                        {
                            StateText = $"{args.ProgressPercentage}% | {args.BytesReceived}/{args.TotalBytesToReceive}";
                        });

                        var items = await losung.InitializeAsync(_cancellationTokenSource.Token, p);
                        Items.AddRange(items);
                    }
                }
                catch (Exception ex)
                {
                    StateText = ex.Message;
                }
                finally
                {
                    _cancellationTokenSource = null;
                }
            }
            IsBusy = false;
        }

        private bool Filter(object item)
        {
            if (!string.IsNullOrEmpty(FilterText) && item is LosungsItem losung)
            {
                try
                {
                    var search = FilterText.ToLower(CultureInfo.CurrentUICulture);
                    return
                        (losung.Losungstext?.ToLower() ?? "").Contains(search) ||
                        (losung.Losungsvers?.ToLower() ?? "").Contains(search) ||
                        (losung.Lehrtext?.ToLower() ?? "").Contains(search) ||
                        (losung.Lehrtextvers?.ToLower() ?? "").Contains(search) ||
                        losung.Day.ToString("dddd", CultureInfo.CurrentUICulture).ToLower().Contains(search);
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        private string LosungToClipboard(LosungsItem losung)
        {
            if (losung == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(losung.Sonntag))
            {
                sb.AppendLine(losung.Sonntag);
                sb.AppendLine("");
            }

            sb.AppendLine(MultiLineText(losung.Losungstext));
            sb.AppendLine($"<align:r><i>{losung.Losungsvers}</i></align>");
            sb.AppendLine("");
            sb.AppendLine(MultiLineText(losung.Lehrtext));
            sb.AppendLine($"<align:r><i>{losung.Lehrtextvers}</i></align>");

            return sb.ToString();
        }

        private string MultiLineText(string raw)
        {
            return String.Join($";{Environment.NewLine}", raw.Split(new []{"; "},StringSplitOptions.RemoveEmptyEntries));
        }

        private async Task OnSundayExecute(bool nextSunday)
        {
            try
            {
                var date = SelectedLosung?.Day ?? DateTime.Today;

                var sunday = nextSunday
                    ? date.AddDays(7 - (int) date.DayOfWeek) 
                    : date.AddDays(date.DayOfWeek==DayOfWeek.Sunday
                                   ? -7
                                   : -(int)date.DayOfWeek);

                var sundayItem = Items.FirstOrDefault(i => i.Day == sunday);

                if (sundayItem != null)
                {
                    View.MoveCurrentTo(sundayItem);
                }
                else if(!_losungen.ContainsKey(sunday.Year))
                {
                    await InitialLosungAsync(sunday.Year);
                    await OnSundayExecute(nextSunday);
                }
            }
            catch
            {
                StateText = "Konnte Losung nicht in Zwischenablage kopieren!";
            }
        }
    }
}

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Windows.Networking.BackgroundTransfer;

namespace LosungenUwp
{
    public class MainViewModel : BindableBase
    {
        private readonly Losungen _losungen;
        private CancellationTokenSource _cancellationTokenSource;
        public MainViewModel()
        {
            _losungen=new Losungen(DateTime.Today.Year);
            SelectedLosung = _losungen.Items.FirstOrDefault(l => l.Day.Date == DateTime.Today);
            LosungItems = _losungen.Items;
        }

        private ObservableCollection<LosungsItem> _losungItems;

        public ObservableCollection<LosungsItem> LosungItems
        {
            get => _losungItems;
            set => SetProperty(ref _losungItems, value);
        }

        public ICommand FillCommand => new DelegateCommand(async () =>
        {
            IsBusy = true;
            try
            {
                _cancellationTokenSource=new CancellationTokenSource();
                var p=new Progress<DownloadOperation>(downloadOperation =>
                {
                    var prozent = 100 * (downloadOperation.Progress.BytesReceived /
                                         (double) downloadOperation.Progress.TotalBytesToReceive);
                    StateText = $"{prozent}% | {downloadOperation.Progress.BytesReceived }/{downloadOperation.Progress.TotalBytesToReceive}";
                });
                
                await _losungen.InitializeAsync(_cancellationTokenSource.Token, p);
            }
            finally
            {
                IsBusy = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

        }, () => !IsBusy).ObservesProperty(() => IsBusy);


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
        
        public ICommand NextSundayCommand => new DelegateCommand(() => OnSundayExecute(true));

        public ICommand PrevSundayCommand => new DelegateCommand(() => OnSundayExecute(false));

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }
        
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                SetProperty(ref _filterText, value, () =>
                {
                    LosungItems =
                        string.IsNullOrEmpty(_filterText)
                            ? _losungen.Items
                            : new ObservableCollection<LosungsItem>(_losungen.Items.Where(Filter));

                    RaisePropertyChanged(nameof(FilteredElements));
                });
            }
        }

        public int FilteredElements => LosungItems.Count;


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
        
        private bool Filter(LosungsItem item)
        {
            if (!string.IsNullOrEmpty(FilterText) && item !=null)
            {
                try
                {
                    var search = FilterText.ToLower();
                    return
                        (item.Losungstext?.ToLower() ?? "").Contains(search) ||
                        (item.Losungsvers?.ToLower() ?? "").Contains(search) ||
                        (item.Lehrtext?.ToLower() ?? "").Contains(search) ||
                        (item.Lehrtextvers?.ToLower() ?? "").Contains(search) ||
                        item.Day.ToString("dddd", CultureInfo.CurrentUICulture).ToLower().Contains(search);
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

        private void OnSundayExecute(bool nextSunday)
        {
            try
            {
                var date = SelectedLosung?.Day ?? DateTime.Today;
             
                var sunday =
                    nextSunday
                        ? LosungItems
                            .Where(i => i.Day.DayOfWeek == DayOfWeek.Sunday && i.Day > date)
                            .OrderBy(i => i.Day)
                            .FirstOrDefault()
                        : LosungItems
                            .Where(i => i.Day.DayOfWeek == DayOfWeek.Sunday && i.Day < date)
                            .OrderBy(i => i.Day)
                            .LastOrDefault();



                if (sunday != null)
                {
                    SelectedLosung = sunday;
                }

            }
            catch
            {
                StateText = "Konnte Losung nicht in Zwischenablage kopieren!";
            }
        }
    }
}

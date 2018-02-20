using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Losungen;
using Prism.Commands;
using Prism.Mvvm;

namespace LosungenWpf.ViewModel
{
    public class MainViewModel : BindableBase
    {
        private readonly object _lock = new object();

        private readonly Losungen.Losungen _losungen;
        private CancellationTokenSource _cancellationTokenSource;
        public MainViewModel()
        {
            _losungen=new Losungen.Losungen(DateTime.Today.Year);
            BindingOperations.EnableCollectionSynchronization(_losungen.Items, _lock);
            SelectedLosung = _losungen.Items.FirstOrDefault(l => l.Day.Date == DateTime.Today);
        }

        public ICommand FillCommand => new DelegateCommand(async () =>
        {
            IsBusy = true;
            try
            {
                _cancellationTokenSource=new CancellationTokenSource();
                var p=new Progress<DownloadProgressChangedEventArgs>(args =>
                {
                    StateText = $"{args.ProgressPercentage}% | {args.BytesReceived}/{args.TotalBytesToReceive}";
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

        public ICommand NextSundayCommand => new DelegateCommand(() => OnSundayExecute(true));

        public ICommand PrevSundayCommand => new DelegateCommand(() => OnSundayExecute(false));

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

                    if (string.IsNullOrEmpty(_filterText)
                        || !View.CanFilter)
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
            var v = CollectionViewSource.GetDefaultView(_losungen.Items);

            using (v.DeferRefresh())
            {
                v.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(nameof(LosungsItem.Day), ListSortDirection.Ascending);
                v.SortDescriptions.Add(sd);
            }
            v.MoveCurrentTo(SelectedLosung);
            return v;
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

        private void OnSundayExecute(bool nextSunday)
        {
            try
            {
                var date = SelectedLosung?.Day ?? DateTime.Today;
             
                var sunday =
                    nextSunday
                        ? View.Cast<LosungsItem>()
                            .Where(i => i.Day.DayOfWeek == DayOfWeek.Sunday && i.Day > date)
                            .OrderBy(i => i.Day)
                            .FirstOrDefault()
                        : View.Cast<LosungsItem>()
                            .Where(i => i.Day.DayOfWeek == DayOfWeek.Sunday && i.Day < date)
                            .OrderBy(i => i.Day)
                            .LastOrDefault();

                

                if (sunday != null)
                { View.MoveCurrentTo(sunday);}
            }
            catch
            {
                StateText = "Konnte Losung nicht in Zwischenablage kopieren!";
            }
        }
    }
}

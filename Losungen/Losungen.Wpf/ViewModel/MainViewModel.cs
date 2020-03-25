using Prism.Commands;
using Prism.Mvvm;
using System;
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
using Losungen.Standard;

namespace Losungen.Wpf.ViewModel
{
    public class MainViewModel : BindableBase
    {
        private readonly object _lock = new object();

        private CancellationTokenSource _cancellationTokenSource;
        private readonly LosungService _losungen;

        public MainViewModel()
        {
            _losungen = new LosungService();
            _losungen.ItemsChanged += OnItemsChanged;
            BindingOperations.EnableCollectionSynchronization(_losungen.Items, _lock);

            Initialise();
        }

        

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

        public ICommand NextSundayCommand => new DelegateCommand(async() => await MoveTo(_losungen.NextSunday),
            () => !IsBusy).ObservesProperty(() => IsBusy);

        public ICommand PrevSundayCommand => new DelegateCommand(async() => await MoveTo(_losungen.PrevSunday),
            () => !IsBusy).ObservesProperty(() => IsBusy);

        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        private ICollectionView _view;

        public ICollectionView View
        {
            get => _view;
            private set => SetProperty(ref _view, value);
        }

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

        public int FilteredElements => View?.Cast<LosungItem>().Count()??0;

        private LosungItem _selectedLosung;

        public LosungItem SelectedLosung
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

        private void Initialise()
        {
            Task.Run(async () =>
                {
                    try
                    {
                        IsBusy = true;
                        await Task.Delay(10000);
                        using (_cancellationTokenSource = new CancellationTokenSource())
                        {
                            await _losungen.InitialiseAsync(_cancellationTokenSource.Token, GetProgress());
                        }
                    }
                    catch (Exception ex)
                    {
                        StateText = ex.Message;
                    }
                    finally
                    {
                        _cancellationTokenSource = null;
                        IsBusy = false;
                    }

                    SelectedLosung = _losungen.Today;
                }
            ).ConfigureAwait(false);
        }

        private void OnItemsChanged(object sender, EventArgs e)
        {
            var v = CollectionViewSource.GetDefaultView(_losungen.Items);

            using (v.DeferRefresh())
            {
                v.SortDescriptions.Clear();
                var sd = new SortDescription(nameof(LosungItem.Day), ListSortDirection.Ascending);
                v.SortDescriptions.Add(sd);
            }

            v.MoveCurrentTo(SelectedLosung);
            View = v;
        }

        private bool Filter(object item)
        {
            if (!string.IsNullOrEmpty(FilterText) && item is LosungItem losung)
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

        private string LosungToClipboard(LosungItem losung)
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
        
        private async Task MoveTo(Func<LosungItem, CancellationToken , Progress<DownloadProgressChangedEventArgs>, Task<LosungItem>>  func)
        {
            if (func != null)
            {
                IsBusy = true;
                try
                {
                    using(_cancellationTokenSource=new CancellationTokenSource())
                    {
                        var to= await func(SelectedLosung, _cancellationTokenSource.Token, GetProgress());
                        if (to != null)
                        {
                            View.MoveCurrentTo(to);
                        }
                    }
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
        private Progress<DownloadProgressChangedEventArgs> GetProgress()
            => new Progress<DownloadProgressChangedEventArgs>(args =>
        {
            StateText = $"{args.ProgressPercentage}% | {args.BytesReceived}/{args.TotalBytesToReceive}";
        });
    }
}

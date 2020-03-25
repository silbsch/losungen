using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Losungen.Standard;
using Xamarin.Forms;


namespace Losungen.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ILosungService _losungService;

        public MainViewModel()
        {
            Title = "Losungen";
            _losungService = DependencyService.Get<ILosungService>();
            _losungService.ItemsChanged += (sender, args) => SetItemsFromService();
            
            SetItemsFromService();
            LoadItemsCommand = new Command(async () => await LoadLosungItemsAsync());
            SelectTodayCommand = new Command(() => SelectedItem = Today, () => !IsBusy);
            NextSundayCommand = new Command(async () => SelectedItem = await _losungService.NextSunday(SelectedItem, CancellationToken.None, null),
                () => !IsBusy);
            PrevSundayCommand = new Command(async () => SelectedItem = await _losungService.PrevSunday(SelectedItem, CancellationToken.None, null),
                () => !IsBusy);
            //MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
            //{
            //    var newItem = item as Item;
            //    Items.Add(newItem);
            //    await DataStore.AddItemAsync(newItem);
            //});
        }

        public IEnumerable<LosungItem> Items { get; private set; }

        public Command LoadItemsCommand { get; set; }

        public Command SelectTodayCommand { get; }

        public Command NextSundayCommand { get; }

        public Command PrevSundayCommand { get; }

        public async Task LoadLosungItemsAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            
            try
            {
                await _losungService.InitialiseAsync(CancellationToken.None, null);
                SelectedItem = _losungService.Today;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private LosungItem _selectedItem;
        
        public LosungItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
        public LosungItem Today => _losungService.Today;

        private void SetItemsFromService()
        {
            Items= _losungService.Items;//.Select(i => new LosungItemViewModel(i));
            OnPropertyChanged(nameof(Items));
        }

        protected override void OnIsBusyChanged()
        {
            base.OnIsBusyChanged();
            PrevSundayCommand.ChangeCanExecute();
            NextSundayCommand.ChangeCanExecute();
            SelectTodayCommand.ChangeCanExecute();
        }
    }
}
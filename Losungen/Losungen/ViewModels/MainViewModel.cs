using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Losungen.Standard;
using Xamarin.Forms;


namespace Losungen.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ILosungService _losungService;  

        public ReadOnlyObservableCollection<LosungItem> Items { get; }

        public Command LoadItemsCommand { get; set; }

        public MainViewModel()
        {
            Title = "Losungen";
            _losungService = DependencyService.Get<ILosungService>();
            Items = _losungService.Items;
            LoadItemsCommand = new Command(async () => await LoadLosungItemsAsync());

            //MessagingCenter.Subscribe<NewItemPage, Item>(this, "AddItem", async (obj, item) =>
            //{
            //    var newItem = item as Item;
            //    Items.Add(newItem);
            //    await DataStore.AddItemAsync(newItem);
            //});
        }

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
        public LosungItem Today
        {
            get => _losungService.Today;
        }
    }
}
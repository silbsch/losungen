using Losungen.Standard;
using Losungen.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Losungen.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemsPage
    {
        private readonly MainViewModel _viewModel;
        private bool _isAppearing;
        
        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new MainViewModel();
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            if (!_isAppearing && (args.SelectedItem is LosungItem item))
            {
                await Navigation.PushAsync(new ItemDetailPage(item));
            }
        }
        protected override async void OnAppearing()
        {
            _isAppearing = true;

            var task = _viewModel.Items.Count == 0
                ? _viewModel.LoadLosungItemsAsync()
                : Task.CompletedTask;

            base.OnAppearing();
            try
            {
                await task;

                ItemsListView.ScrollTo(
                    _viewModel.SelectedItem ?? _viewModel.Today,
                    ScrollToPosition.MakeVisible, false);
            }
            finally
            {
                _isAppearing = false;
            }
        }
    }
}
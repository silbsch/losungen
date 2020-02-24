using System;
using System.Linq;
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
        private bool _isDisappeared;

        private ItemsCarouselPage _itemsCarouselPage;

        public ItemsPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new MainViewModel();
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            if (!_isDisappeared && !_isAppearing && args.SelectedItem!=null)
            {
                if (_itemsCarouselPage == null)
                {
                    _itemsCarouselPage = new ItemsCarouselPage
                    {
                        BindingContext = _viewModel
                    };
                }
                await Navigation.PushAsync(_itemsCarouselPage);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isDisappeared = true;
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

                ItemsListView.ScrollTo(_viewModel.SelectedItem ?? _viewModel.Today,
                    ScrollToPosition.MakeVisible, false);
            }
            finally
            {
                _isAppearing = _isDisappeared= false;
            }
        }


        private void TodayClicked(object sender, EventArgs e)
        {
            _isDisappeared = true;
            _viewModel.SelectedItem = _viewModel.Today;
            _isDisappeared = false;
            ItemsListView.ScrollTo(_viewModel.Today,
                ScrollToPosition.MakeVisible, true);
        }
    }
}
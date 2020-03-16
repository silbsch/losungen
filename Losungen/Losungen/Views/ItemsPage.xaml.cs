using System;
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

        private readonly Lazy<ItemsCards> _lazyCards;

        public ItemsPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _lazyCards = new Lazy<ItemsCards>(() => new ItemsCards(_viewModel) /*new ItemsCarouselPage { BindingContext = _viewModel };*/ );
            BindingContext = _viewModel;
        }

        void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            if (!_isDisappeared && !_isAppearing && args.SelectedItem!=null)
            {
                ItemsListView.ScrollTo(args.SelectedItem, ScrollToPosition.MakeVisible, true);
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

                ItemsListView.ScrollTo(_viewModel.SelectedItem ?? _viewModel.Today, ScrollToPosition.MakeVisible, false);
            }
            finally
            {
                _isAppearing = _isDisappeared= false;
            }
        }



        private async void OnItemTapped(object sender, ItemTappedEventArgs args)
        {
            if (!_isDisappeared && !_isAppearing && args.Item != null)
            {
                await Navigation.PushAsync(_lazyCards.Value);
            }
        }
    }
}
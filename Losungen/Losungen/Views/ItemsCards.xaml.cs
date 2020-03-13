using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Losungen.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Losungen.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ItemsCards : ContentPage
    {
        public ItemsCards()
        {
            InitializeComponent();
        }

        public ItemsCards(MainViewModel mainViewModel) :
            this()
        {
            BindingContext = mainViewModel;
            //CoverFlowView.SelectedItem = mainViewModel.SelectedItem;
        }
    }
}
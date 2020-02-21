using System;
using System.ComponentModel;
using Losungen;
using Losungen.Standard;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Losungen.ViewModels;

namespace Losungen.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemDetailPage : ContentPage
    {
        LosungItem _losung;

        public ItemDetailPage(LosungItem losung)
        {
            InitializeComponent();

            BindingContext = _losung = losung;
        }

        public ItemDetailPage()
        {
            InitializeComponent();
        }
    }
}
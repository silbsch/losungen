using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Losungen.Views;
using Xamarin.Forms;

namespace Losungen
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage 
    {
        private readonly List<NavigationPage> _pages;

        public MainPage()
        {
            _pages = new List<NavigationPage>();
            InitializeComponent();
        }

        public async Task NavigateFromMenu(MenuItemType menuItemType)
        {
            if (Detail is NavigationPage np && !_pages.Contains(np))
            {
                _pages.Add(np);
            }

            NavigationPage GeneratePage<T>() where T:Page, new() {
                var ip = new NavigationPage(new T());
                _pages.Add(ip);
                return ip;
            }

            NavigationPage newPage = null;
            switch (menuItemType)
                {
                    case MenuItemType.Browse:
                        newPage=_pages.FirstOrDefault(p=>p.CurrentPage.GetType()==typeof(ItemsPage))??
                              GeneratePage<ItemsPage>();
                        break;
                    case MenuItemType.About:
                        newPage = _pages.FirstOrDefault(p => p.CurrentPage.GetType() == typeof(AboutPage)) ??
                                  GeneratePage<AboutPage>();
                        break;
                }

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                    await Task.Delay(100);

                IsPresented = false;
            }
        }
    }
}

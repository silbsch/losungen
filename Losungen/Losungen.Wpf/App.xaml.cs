
using System;
using Losungen.Wpf.Services;
using xf=Xamarin.Forms;

namespace Losungen.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App 
    {
        App()
        {
            try
            {
                xf.Forms.Init();
                xf.DependencyService.Register<DataServiceLocator>();
            }
            catch (Exception e)
            {

            }
            
        }
    }
}

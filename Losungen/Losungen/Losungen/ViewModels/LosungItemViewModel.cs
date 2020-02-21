using System.Globalization;
using Losungen.Standard;

namespace Losungen.ViewModels
{
    public class LosungItemViewModel
    {
        private readonly LosungItem _losungItem;

        public LosungItemViewModel(LosungItem losungItem)
        {
            _losungItem = losungItem;
        }

        public string Day => _losungItem.Day.ToString("D", CultureInfo.CurrentUICulture);
        
        public string Losungstext => _losungItem.Losungstext;

        public string Losungsvers => _losungItem.Losungsvers;

        public string Lehrtext => _losungItem.Lehrtext;
        public string Lehrtextvers => _losungItem.Lehrtextvers;

        public string Sonntag => _losungItem.Sonntag;
    }
}

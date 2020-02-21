using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Foundation;
using Losungen.Standard;
using UIKit;

namespace Losungen.iOS.Services
{
    class DataServiceLocator : IDataServiceLocator
    {
        public string GetDatabaseFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library"); ;
        }
    }
}
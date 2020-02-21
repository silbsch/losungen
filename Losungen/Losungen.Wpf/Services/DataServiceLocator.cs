using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Losungen.Standard;

namespace Losungen.Wpf.Services
{
    class DataServiceLocator: IDataServiceLocator
    {
        public string GetDatabaseFolder()
        {
            return Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        }
    }
}

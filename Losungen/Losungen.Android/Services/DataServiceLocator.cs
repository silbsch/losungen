using Losungen.Standard;

namespace Losungen.Droid.Services
{
    class DataServiceLocator : IDataServiceLocator
    {
        public string GetDatabaseFolder()
        {
            return System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        }
    }
}
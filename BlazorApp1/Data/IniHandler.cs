using IniParser;
using IniParser.Model;

namespace BlazorApp1.Data
{
    public class IniHandler
    {
        private static readonly SemaphoreSlim semaphore = new(1, 1);
        private static readonly string file = "config.ini";
        private readonly IniData _ini;

        public IniHandler()
        {
            FileIniDataParser iniParser = new();
            semaphore.Wait();
            _ini = iniParser.ReadFile(file);
            semaphore.Release();
            Console.WriteLine("config.ini");
        }

        public KeyDataCollection this[string section]
        {
            get => _ini[section];
        }
    }
}
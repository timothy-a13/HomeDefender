using Python.Runtime;

namespace BlazorApp1.Data
{
    public class PythonManager
    {
        private readonly object _locker = new();
        private readonly bool _isConfigured;

        public PythonManager()
        {
            string? pathToVirtualEnv = AppEnvironment.PythonHome;
            _isConfigured = !string.IsNullOrWhiteSpace(pathToVirtualEnv);
            if (!_isConfigured) return;

            Runtime.PythonDLL = Path.Combine(pathToVirtualEnv!, "python39.dll");
            PythonEngine.PythonHome = pathToVirtualEnv;
            PythonEngine.PythonPath = string.Join(
                Path.PathSeparator,
                Path.Combine(pathToVirtualEnv!, "Lib"),
                Path.Combine(pathToVirtualEnv!, "Lib", "site-packages"),
                AppEnvironment.CorePath);
        }

        public bool IsPlayable(string fileName)
        {
            if (!_isConfigured)
                throw new InvalidOperationException(
                    "Set HOMEDEFENDER_PYTHON_HOME before using Python-based media validation.");

            bool isPlayable;
            lock (_locker)
            {
                PythonEngine.Initialize();
                using (Py.GILState _ = Py.GIL())
                {
                    dynamic ffprobe = Py.Import("ffprobe");
                    isPlayable = ffprobe.IsPlayable(fileName);
                }
                PythonEngine.Shutdown();
            }
            return isPlayable;
        }
    }
}

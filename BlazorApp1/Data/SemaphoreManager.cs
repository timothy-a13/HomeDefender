namespace BlazorApp1.Data
{
    public class SemaphoreManager
    {
        private readonly Dictionary<string, SemaphoreSlim> _semaphores;

        public SemaphoreManager()
        {
            _semaphores = new Dictionary<string, SemaphoreSlim>();
        }

        public SemaphoreSlim? this[string key]
        {
            get
            {
                if (_semaphores.ContainsKey(key))
                    return _semaphores[key];
                return null;
            }
            set
            {
                if (value is null) throw new ArgumentNullException(nameof(value));
                if (!_semaphores.ContainsKey(key)) _semaphores[key] = value;
            }
        }

        public bool NewSemaphore(string key, int initialCount, int maxCount)
        {
            if (!_semaphores.ContainsKey(key))
            {
                _semaphores[key] = new SemaphoreSlim(initialCount, maxCount);
                return true;
            }
            return false;
        }
    }
}

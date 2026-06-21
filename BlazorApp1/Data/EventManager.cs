using System;

namespace BlazorApp1.Data
{
    public class EventManager
    {
        private Dictionary<string, object> _events = new();

        public object? this[string key]
        {
            get
            {
                if (_events.ContainsKey(key))
                    return _events[key];
                return null;
            }
        }

        public void NewEvent<Args>(string key, Action<object?, Args> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));
            if (!_events.ContainsKey(key))
            {
                _events[key] = new EventHandler<Args>(func);
            }
        }
    }
}

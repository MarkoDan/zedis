using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zedis
{
    public class DataStore
    {
        private readonly ConcurrentDictionary<string, string> _store = new();

        public string Set(string key, string value)
        {
            _store[key] = value;
            return "OK";
        }

        public string Get(string key)
        {
            return _store.TryGetValue(key, out var value) ? value : "(nil)";
        }
        
    }
}
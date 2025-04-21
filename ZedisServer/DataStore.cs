using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Zedis
{
    public class DataStore
    {
        private readonly ConcurrentDictionary<string, string> _store = new();
        private readonly ConcurrentDictionary<string, DateTime> _expireStore = new();

        public string Set(string key, string value)
        {
            _store[key] = value;
            return "OK";
        }

        public string Get(string key)
        {
            return _store.TryGetValue(key, out var value) ? value : "(nil)";
        }
        
        public string Del(IEnumerable<string> keys) 
        {
            int count = 0;
            foreach (string key in keys) 
            {
                bool removed = _store.TryRemove(key, out _);
                if (removed) count++;
            }
            return count.ToString();
        }

        public string Exists(IEnumerable<string> keys) 
        {
            int count = 0;
            foreach (string key in keys) 
            {
                bool exists = _store.TryGetValue(key, out _);
                if (exists) count++;
            }
            return count.ToString();
        }

        public string Incr(string key) 
        {
            bool exists = _store.TryGetValue(key, out _);
            
            if (exists)
            {
                if (Int32.TryParse(_store[key], out int value))
                {
                    value++;
                
                    _store[key] = value.ToString();

                    return value.ToString();
                }
                else{
                    return "ERR value is not an integer";
                }
                
            }
            else{
                _store[key] = "1";
                return _store[key];
            }
        }

        public string Type(string key) 
        {
            return _store.ContainsKey(key) ? "string" : "none";
        }

        public string Expire(IEnumerable<string> parts) 
        {
            var args = parts.ToList();

            if (args.Count != 2) 
            {
                return "ERR wrong number of arguments for 'expire' command";
            }

            var key = args[0];
            var success = int.TryParse(args[1], out int seconds);

            if (!success || seconds < 0) 
            {
                return "ERR invalid expiration time"; 
            }

            if (!_store.ContainsKey(key)) 
            {
                return "0";
            }

            var expireAt = DateTime.UtcNow.AddSeconds(seconds);
            _expireStore[key] = expireAt;

            return "1";
        } 
        
    }
}
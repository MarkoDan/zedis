using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Zedis
{
    public class DataStore
    {
        private readonly ConcurrentDictionary<string, string> _store = new();
        private readonly ConcurrentDictionary<string, DateTime> _expireStore = new();
        private ConcurrentDictionary<string, LinkedList<string>> _lists = new();
        private readonly object _lock = new();

        public string Set(string key, string value)
        {
            lock (_lock)
            {
                _store[key] = value;
                return "OK";
            }
        }

        public string Get(string key)
        {
            lock (_lock)
            {
                if (_expireStore.TryGetValue(key, out var expiry) && expiry < DateTime.UtcNow)
                {
                    _store.TryRemove(key, out _);
                    _expireStore.TryRemove(key, out _);
                    return "(nil)";
                }
                return _store.TryGetValue(key, out var value) ? value : "(nil)";
            }
        }

        public string Del(IEnumerable<string> keys)
        {
            lock (_lock)
            {
                int count = 0;
                foreach (string key in keys)
                {
                    bool removed = _store.TryRemove(key, out _);
                    if (removed) count++;
                }
                return count.ToString();
            }
        }

        public string Exists(IEnumerable<string> keys)
        {
            lock (_lock)
            {
                int count = 0;
                foreach (string key in keys)
                {
                    bool exists = _store.TryGetValue(key, out _);
                    if (exists) count++;
                }
                return count.ToString();
            }
        }

        public string Incr(string key)
        {
            lock (_lock)
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
                    else
                    {
                        return "ERR value is not an integer";
                    }

                }
                else
                {
                    _store[key] = "1";
                    return _store[key];
                }
            }
        }

        public string Type(string key)
        {
            lock (_lock)
            {
                return _store.ContainsKey(key) ? "string" : "none";
            }
        }

        public string Expire(IEnumerable<string> parts)
        {
            lock (_lock)
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

        public string Ttl(string key)
        {
            lock (_lock)
            {
                if (!_store.ContainsKey(key))
                    return "-2"; // Key does not exist

                bool existsExpiry = _expireStore.TryGetValue(key, out var expiry);

                if (existsExpiry && expiry < DateTime.UtcNow)
                {
                    _store.TryRemove(key, out _);
                    _expireStore.TryRemove(key, out _);
                    return "-2"; // Key existed but is now expired
                }

                if (existsExpiry)
                {
                    int secondsLeft = (int)(expiry - DateTime.UtcNow).TotalSeconds;
                    return secondsLeft.ToString();
                }

                return "-1"; // Key exists but has no expiration
            }
        }

        public string Echo(string message)
        {
            lock (_lock)
            {
                return message;
            }
        }

        public string Save()
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(_store);
                File.WriteAllText("dump.zedis.json", json);
                return "OK";
            }
        }

        public void Load()
        {
            lock (_lock)
            {
                if (File.Exists("dump.zedis.json"))
                {
                    var json = File.ReadAllText("dump.zedis.json");
                    var loaded = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                    if (loaded != null)
                    {
                        _store.Clear();
                        foreach (var kv in loaded)
                        {
                            _store[kv.Key] = kv.Value;
                        }
                    }
                }
            }
        }

        public string BgSave() 
        {
            Task.Run(() => {
                
                lock(_lock) 
                {
                    var json = JsonSerializer.Serialize(_store);
                    File.WriteAllText("dump.zedis.json", json);
                }
                
            });
            return "Background saving started";
        }

        public string Append(string key, string value) 
        {
            lock(_lock) 
            {
                _store.AddOrUpdate(key, value, (k , existing) => existing + value);
            }
            
            return _store[key].Length.ToString();

        }

        public string Strlen(string key) 
        {
            if (_store.TryGetValue(key, out var value))
            {
                return value.Length.ToString();
            }
            return "0";
        }

        public string MSet(IEnumerable<string> args) 
        {
            int count = args.Count();

            if(count % 2 != 0)
            {
                return "ERR wrong number of arguments for 'mset' command";
            }

            List<string> argsList= args.ToList();

            lock(_lock)
            {
                for (int i = 0; i < count; i += 2)
                {
                    _store[argsList[i]] = argsList[i+1];
                }
            }
        
            return "OK";   
        }

        public string MGet(IEnumerable<string> args) 
        {   
            var sb = new StringBuilder();
            int count = 1;
            
            foreach (var arg in args) 
            {   
                var value = Get(arg);
                sb.AppendLine($"{count}) {value}");
                count++;    
            }

            return sb.ToString().TrimEnd();
        }

        public string Setex(string key, string seconds, string value)
        {
            if (!int.TryParse(seconds, out int secs) || secs < 0)
            {
                return "ERR invalid expiration time";
            }

            lock (_lock)
            {
                Set(key, value);
                _expireStore[key] = DateTime.UtcNow.AddSeconds(secs);
            }

            return "OK";
        }

        public string Setnx(string key, string value)
        {
            if(_store.TryGetValue(key, out var _))
            {
                return "(integer) 0";
            }

            lock(_lock)
            {
                _store[key] = value;
                return "(integer) 1";
            }
            
        }

        public string IncrBy(string key, string incrValue)
        {
            int intIncrValue = int.Parse(incrValue);
            if(_store.TryGetValue(key, out _)) 
            {
                if(int.TryParse(_store[key], out var value))
                {
                    value += intIncrValue;
                    _store[key] = value.ToString();
                    return value.ToString();
                }
                return "ERR value is not an integer";
            }
            return "ERR key does not exists";

        }

        public string Decr(string key)
        {
            if(_store.TryGetValue(key, out var valueStr))
            {
                if(int.TryParse(valueStr, out var value))
                {
                    lock(_lock)
                    {
                        value--;
                        _store[key] = value.ToString();
                        return value.ToString();
                    }
                    
                }
                else
                {
                    return "ERR value is not an integer";
                }
            }
            return "key does not exists";
        }

        public string DecrBy(string key, string decrValue)
        {
            int intDecrValue = int.Parse(decrValue);
            if(_store.TryGetValue(key, out var valueStr))
            {
                if(int.TryParse(valueStr, out var value))
                {
                    lock(_lock)
                    {
                        value -= intDecrValue;
                        _store[key] = value.ToString();
                        return value.ToString();
                    }
                    
                }
                else
                {
                    return "ERR value is not an integer";
                }

            }
            return "key does not exists";
        }

        public string LPush(List<string> arguments)
        {
            string key = arguments[0];
            var values = arguments.Skip(1);

            var list = _lists.GetOrAdd(key, _ => new LinkedList<string>());

            foreach (var value in values)
            {
                list.AddFirst(value);
            }

            return list.Count.ToString();

        }

        public string RPush(List<string> arguments)
        {
            string key = arguments[0];
            var values = arguments.Skip(1);

            var list = _lists.GetOrAdd(key, _ => new LinkedList<string>());

            foreach (var value in values)
            {
                list.AddLast(value);
            }

            return list.Count.ToString();
        }

        public object LPop(List<string> args)
        {
           string key = args[0];
           int count = 1;

           if(args.Count == 2) 
           {
                if(!int.TryParse(args[1], out count))
                {
                    return "ERR invalid count";
                }
           }

           if (!_lists.TryGetValue(key, out var list))
           {
                return "(nil)";
           }
           if (list.Count == 0)
           {
                return "(nil)";
           }
           if (count == 1)
           {
                var value = list.First.Value ;
                list.RemoveFirst();
                return value;
           }
           else
           {
                var result = new List<string>();
                while (count-- > 0 && list.Count > 0)
                {
                    result.Add(list.First.Value);
                    list.RemoveFirst();
                }
                return result;
           }
        }

        public object RPop(List<string> args)
        {
           string key = args[0];
           int count = 1;

           if(args.Count == 2) 
           {
                if(!int.TryParse(args[1], out count))
                {
                    return "ERR invalid count";
                }
           }

           if (!_lists.TryGetValue(key, out var list))
           {
                return "(nil)";
           }
           if (list.Count == 0)
           {
                return "(nil)";
           }
           if (count == 1)
           {
                var value = list.Last.Value;
                list.RemoveLast();
                return value;
           }
           else
           {
                var result = new List<string>();
                while (count-- > 0 && list.Count > 0)
                {
                    result.Add(list.Last.Value);
                    list.RemoveLast();
                }
                return result;
           }
        }

        public object LRange(string key, string value1, string value2)
        {
            int start = int.Parse(value1);
            int end = int.Parse(value2);

            if(!_lists.TryGetValue(key, out var list))
            {
                return "(nil)";
            }

            if (end == -1)
            {
                end = list.Count - 1;
            }
            
            List<string> result = new List<string>();
            int currentIndex = 0;

            foreach (var item in list)
            {
                if (currentIndex >= start && currentIndex <= end)
                {
                    result.Add(item);
                    
                }
                currentIndex++;
            }

            return result;

        }

        
    }
}
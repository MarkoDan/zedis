
# GitHub Issues for Zedis Project

Use these to create issues on GitHub manually or via the GitHub CLI. Each issue is tagged by development phase.

---

## 📊 Phase 1: Core Key-Value Store

### Issue: Implement core commands
**Labels:** phase:1-core, feature, done
```
- [x] RESP protocol support
- [x] TCP socket server
- [x] Command dispatcher
- [x] SET, GET, DEL, EXISTS
- [x] EXPIRE, TTL, INCR, PING, TYPE
- [x] Zedis CLI client with RESP
- [X] ECHO
- [X] QUIT to close client
- [X] Command logging to file or console
```

### Issue: Add missing base commands
**Labels:** phase:1-core, enhancement
```

- [X] Zedis configuration file
```

---

## 💾 Phase 2: Persistence (Disk Storage)

### Issue: Basic persistence
**Labels:** phase:2-persistence, feature
```
- [X] SAVE to snapshot current store
- [X] LOAD data on server startup
- [X] BGSAVE background save
```

### Issue: Append-only log
**Labels:** phase:2-persistence, enhancement
```
- [X] AOF persistence
- [X] CONFIG GET/SET to manage behavior
```

---

## 📊 Phase 3: String Commands

### Issue: Extended string operations
**Labels:** phase:3-strings, feature
```
- [X] APPEND
- [X] STRLEN
- [X] MSET,
- [ ] MGET,
- [X] SETEX, SETNX, GETSET
- [ ] GETSET
- [X] INCRBY, DECR, DECRBY
```

---

## 🔧 Phase 4: Data Types

### Issue: Lists
**Labels:** phase:4-datatypes, feature
```
- [X] LPUSH, RPUSH
- [X] LPOP, RPOP
- [X] LRANGE, LLEN
```

### Issue: Sets
**Labels:** phase:4-datatypes, feature
```
- [X] SADD, SREM
- [X] SMEMBERS, SCARD, SISMEMBER
```

### Issue: Hashes
**Labels:** phase:4-datatypes, feature
```
- [X] HSET, HGET
- [X] HDEL, HGETALL, HLEN
```

---

## 📧 Phase 5: Pub/Sub

### Issue: Implement pub/sub
**Labels:** phase:5-pubsub, feature
```
- [X] SUBSCRIBE, UNSUBSCRIBE
- [X] PUBLISH
- [ ] Maintain channel-to-client map
```

---

## 🔐 Phase 6: Admin + Auth

### Issue: Auth and security
**Labels:** phase:6-auth, security
```
- [ ] AUTH <password>
- [ ] Add zedis.conf for config
```

### Issue: Info and config
**Labels:** phase:6-admin, enhancement
```
- [ ] INFO
- [ ] CONFIG GET/SET
```

### Issue: Client commands
**Labels:** phase:6-admin, feature
```
- [ ] CLIENT LIST
- [ ] CLIENT ID
```

---

## 💡 Phase 7: Advanced Features

### Issue: Eviction and memory
**Labels:** phase:7-advanced, enhancement
```
- [ ] Eviction: LRU, LFU
```

### Issue: Lua scripting
**Labels:** phase:7-advanced, feature
```
- [ ] EVAL
- [ ] EVALSHA
```

### Issue: Transactions
**Labels:** phase:7-advanced, feature
```
- [ ] MULTI, EXEC
- [ ] DISCARD, WATCH
```

---

## 🌐 Phase 8: Clustering

### Issue: Replication
**Labels:** phase:8-cluster, feature
```
- [ ] SLAVEOF, SYNC
- [ ] REPLCONF
```

### Issue: Cluster mode
**Labels:** phase:8-cluster, enhancement
```
- [ ] Slot assignment, key hashing
- [ ] Gossip, heartbeat
```

---

## 🧪 Phase 9: RESP v3 + Pipelining

### Issue: Upgrade RESP
**Labels:** phase:9-resp, feature
```
- [ ] RESP v3 support
- [ ] Pipelining commands
- [ ] RESP arrays for MGET, KEYS
```

---

## 🚀 Phase 10: CLI & Monitoring

### Issue: CLI polish
**Labels:** phase:10-cli, enhancement
```
- [ ] Command history, autocomplete
- [ ] MONITOR
- [ ] KEYS, SCAN
- [ ] FLUSHALL, FLUSHDB
```

---

## 🌟 Final: Full Redis Compatibility

### Issue: Drop-in Redis replacement
**Labels:** final, milestone
```
- [ ] Pass redis-cli compatibility tests
- [ ] Run Redis-based apps against Zedis
```

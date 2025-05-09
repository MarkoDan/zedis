using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZedisServer.Models
{
    public class ClientInfo
    {
        public int Id { get; set; }
        public string Address { get ; set;}  = "";
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActive { get; set; }
        public int Subscriptions { get; set; }
    }
}
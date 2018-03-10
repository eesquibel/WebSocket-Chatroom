using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketApp.Model
{
    public class ChatMessage
    {
        public Guid Id { get; set; }

        public string Author { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public static implicit operator string(ChatMessage instance)
        {
            return JsonConvert.SerializeObject(instance);
        }

        public static implicit operator RedisValue(ChatMessage instance)
        {
            return JsonConvert.SerializeObject(instance);
        }
    }
}

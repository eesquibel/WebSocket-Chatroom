using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketApp.Model
{
    public class ChatMessageCollection : SortedList<long, ChatMessage>
    {
        public ChatMessageCollection() : base()
        {

        }

        public ChatMessageCollection(IDictionary<long, ChatMessage> dictionary) : base(dictionary)
        {

        }

        public static implicit operator string(ChatMessageCollection instance)
        {
            return JsonConvert.SerializeObject(instance.Values, typeof(ChatMessageCollection), JsonSettings.Serializer);
        }

        public static implicit operator RedisValue(ChatMessageCollection instance)
        {
            return JsonConvert.SerializeObject(instance.Values, typeof(ChatMessageCollection), JsonSettings.Serializer);
        }
    }
}

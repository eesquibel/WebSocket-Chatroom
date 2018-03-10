using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketApp
{
    public static class RedisStore
    {
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection;

        public static ConfigurationOptions RedisConfiguration { get; private set; }

        static RedisStore()
        {
            RedisConfiguration = new ConfigurationOptions();
            LazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(RedisConfiguration));
        }

        public static ConnectionMultiplexer Connection => LazyConnection.Value;

        public static IDatabase Database => Connection.GetDatabase();

        public static ISubscriber Subscriber => Connection.GetSubscriber();
    }
}

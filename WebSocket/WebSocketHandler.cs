using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketApp.Model;

namespace WebSocketApp
{
    public class WebSocketHandler
    {
        static ISubscriber Subscriber { get; set; }
        static IDatabase Database { get; set; }
        static RedisChannel Channel { get; set; }

        static ConcurrentDictionary<Guid, WebSocketHandler> Sockets;

        static WebSocketHandler()
        {
            Subscriber = RedisStore.Subscriber;
            Database = RedisStore.Database;
            Channel = new RedisChannel("Chat", RedisChannel.PatternMode.Auto);
            Sockets = new ConcurrentDictionary<Guid, WebSocketHandler>();

            Subscriber.Subscribe(Channel, ChannelMessage);
        }

        static void ChannelMessage(RedisChannel channel, RedisValue message)
        {
            var obj = JsonConvert.DeserializeObject(message, JsonSettings.Serializer);

            if (obj is ChatMessage)
            {
                var payload = new WebWrapper { Payload = obj };
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload));

                foreach (var socket in Sockets)
                {
                    socket.Value.Send(buffer);
                }
            }
        }

        HttpContext Context { get; set; }
        WebSocket WebSocket { get; set; }
        public Guid Id { get; private set; }
 
        public WebSocketHandler(HttpContext context, WebSocket webSocket)
        {
            Context = context;
            WebSocket = webSocket;
            Id = Guid.NewGuid();

            Sockets.TryAdd(Id, this);

            Database.SortedSetRangeByRankAsync("Messages", -10, -1).ContinueWith<Task>(task =>
            {
                var messages = new ChatMessageCollection(task.Result.Select(value => JsonConvert.DeserializeObject<ChatMessage>(value)).ToDictionary(m => m.Timestamp.Ticks));
                var wrapper = new WebWrapper { Payload = messages };
                return Send(wrapper);
            });
        }

        void ChannelPublish(ArraySegment<byte> message)
        {
            RedisValue value = Encoding.UTF8.GetString(message.ToArray());
            Subscriber.Publish(Channel, value, CommandFlags.FireAndForget);
        }

        void ChannelPublish(string message)
        {
            Subscriber.PublishAsync(Channel, message, CommandFlags.FireAndForget);
        }

        public Task Send(ArraySegment<byte> message)
        {
            return WebSocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public Task Send(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            return WebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task Handle()
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var resultBuffer = new ArraySegment<byte>(buffer, 0, result.Count);
                var resultArray = resultBuffer.ToArray();
                var resultString = Encoding.UTF8.GetString(resultArray);
                var message = JsonConvert.DeserializeObject(resultString, JsonSettings.Serializer);

                if (message is JObject)
                {
                    var jobject = (JObject)message;
                    if (jobject["Type"] != null && jobject["Payload"] != null)
                    {
                        switch(jobject["Type"].ToString())
                        {
                            case "ChatMessage":
                                var chat = jobject["Payload"].ToObject<ChatMessage>();

                                chat.Id = this.Id;
                                chat.Timestamp = DateTime.UtcNow;

                                Database.SortedSetAdd("Messages", chat, chat.Timestamp.Ticks, CommandFlags.FireAndForget);
                                ChannelPublish(chat);
                                break;
                        }
                    }
                }

                result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            WebSocketHandler removed;
            Sockets.TryRemove(Id, out removed);

            await WebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

        }
    }
}

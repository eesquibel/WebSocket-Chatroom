using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketApp.Model
{
    public class WebWrapper
    {
        string type;

        public string Type
        {
            get
            {
                return payload == null ? type : payload.GetType().Name;
            }
            set
            {
                type = value;
            }
        }

        object payload;

        public object Payload
        {
            get
            {
                if (payload is ChatMessageCollection)
                {
                    return JArray.FromObject(((ChatMessageCollection)payload).Values);
                }

                return JObject.FromObject(payload);
            }
            set
            {
                payload = value;
            }
        }

        public object GetPayload()
        {
            return payload;
        }

        public T GetPayload<T>() where T: class
        {
            if (payload == null)
            {
                return null;
            }

            return payload is T ? (T)payload : null;
        }

        public static implicit operator string(WebWrapper instance)
        {
            return JsonConvert.SerializeObject(instance);
        }
    }
}

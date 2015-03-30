using System.Collections.Generic;

namespace RtuBroker.Test.Publisher
{
    public class Msg
    {
        public string Topic { get; private set; }
        public Dictionary<string, object> Headers { get; private set; }
        public byte[] Body { get; private set; }

        public Msg(string topic, Dictionary<string,object> headers, byte[] body)
        {
            Topic = topic;
            Headers = headers;
            Body = body;
        }
    }
}
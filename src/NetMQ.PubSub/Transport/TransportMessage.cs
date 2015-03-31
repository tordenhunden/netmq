using System.Collections.Generic;

namespace NetMQ.PubSub.Transport
{
    public class TransportMessage
    {
        public string Topic { get; private set; }
        public int SequenceNumber { get; private set; }
        public Dictionary<string, object> Headers { get; private set; }
        public byte[] Body { get; private set; }

        public TransportMessage(string topic, int sequenceNumber, Dictionary<string, object> headers, byte[] body)
        {
            Topic = topic;
            SequenceNumber = sequenceNumber;
            Headers = headers;
            Body = body;
        }
    }
}
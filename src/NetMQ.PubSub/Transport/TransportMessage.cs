using System.Collections.Generic;
using System.Linq;
using NetMQ.PubSub.Utils;

namespace NetMQ.PubSub.Transport
{
    public sealed class TransportMessage
    {
        public string Topic { get; set; }
        public int SequenceNumber { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }

        public TransportMessage(string topic, int sequenceNumber, Dictionary<string, string> headers, byte[] body)
        {
            Topic = topic;
            SequenceNumber = sequenceNumber;
            Headers = headers;
            Body = body;
        }

        public override string ToString()
        {
            return string.Format("Topic: {0}, SequenceNumber: {1}, Headers: {2}, Body: {3}", 
                Topic, 
                SequenceNumber, 
                string.Join(", ", Headers.Select(pair => 
                    string.Format("{0}=>{1}::{2}", 
                    pair.Key, 
                    pair.Value, 
                    pair.Value == null
                        ? "?"
                        : pair.Value.GetType().ToString()))), 
                ByteArrayFormat.ByteArrayToHexString(Body));
        }
    }
}
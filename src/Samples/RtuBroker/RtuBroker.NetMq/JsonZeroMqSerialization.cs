using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ZeroMQ;

namespace RtuBroker.ZeroMq
{
    

    static class JsonZeroMqSerialization
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static void WriteTransportMessage(ZMessage zmsg, TransportMessage msg)
        {
            var headers = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg.Headers, JsonSettings));
            zmsg.Add(new ZFrame(msg.Topic));
            zmsg.Add(new ZFrame(msg.SequenceNumber));
            zmsg.Add(new ZFrame(headers));
            zmsg.Add(new ZFrame(msg.Body));
        }

        public static TransportMessage ReadTransportMessage(ZMessage msg)
        {
            var topic = msg.Pop().ReadString();
            var sequenceNumber = msg.Pop().ReadInt32();
            var headers = JsonConvert.DeserializeObject<Dictionary<string,object>>(msg.Pop().ReadString(Encoding.UTF8));
            var bodyFrame = msg.Pop();
            var length = bodyFrame.Length;
            var body = new byte[length];
            bodyFrame.Read(body, 0, (int)length);
            return new TransportMessage(topic, sequenceNumber, headers, body);
        }
    }
}
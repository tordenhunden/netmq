﻿using System;
using System.Collections.Generic;
using System.Text;
using NetMQ.PubSub.Transport;
using Newtonsoft.Json;

namespace NetMQ.PubSub.Json
{
    public static class JsonSerialization
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static void WriteTransportMessage(NetMQMessage zmsg, TransportMessage msg)
        {
            var json = JsonConvert.SerializeObject(msg.Headers, JsonSettings);
            var headers = Encoding.UTF8.GetBytes(json);
            zmsg.Push(msg.Body);
            zmsg.Push(headers);
            zmsg.Push(msg.SequenceNumber);
            zmsg.Push(msg.Topic);
        }

        public static TransportMessage ReadTransportMessage(NetMQMessage msg)
        {
            //pop
            var topicFrame = msg.Pop();
            var seqNoFrame = msg.Pop();
            var headerFrame = msg.Pop();
            var bodyFrame = msg.Pop();

            //convert / copy
            var topic = topicFrame.ConvertToString(Encoding.UTF8);
            var sequenceNumber = seqNoFrame.ConvertToInt32();
            var headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(headerFrame.ConvertToString(Encoding.UTF8));
            var body = new byte[bodyFrame.MessageSize];
            Array.Copy(bodyFrame.Buffer, body, bodyFrame.MessageSize);

            return new TransportMessage(topic, sequenceNumber, headers, body);
        }
    }
}

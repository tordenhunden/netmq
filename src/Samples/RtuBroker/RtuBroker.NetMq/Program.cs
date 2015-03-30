using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RtuBroker.ZeroMq
{
    class TopicSpecificSequenceNumberState
    {
        private readonly Dictionary<string, SeqNoContainer> _dic = new Dictionary<string, SeqNoContainer>();

        public int Next(string topic)
        {
            SeqNoContainer seqNoContainer;
            if (!_dic.TryGetValue(topic, out seqNoContainer))
            {
                seqNoContainer = new SeqNoContainer();
                _dic.Add(topic,seqNoContainer);
            }

            return seqNoContainer.SeqNo++;
        }

        internal class SeqNoContainer
        {
            public int SeqNo;
        }
    }

    

    class Program
    {
        public static void Main(string[] args)
        {
            //var gc = GcMonitor.GetInstance();
            //gc.StartGcMonitoring();

            Action<TransportMessage> publish;
            var publisher = ZeroMqPublishSubscribe.StartPublisher(
                "tcp://*:6000", 
                JsonZeroMqSerialization.WriteTransportMessage,
                Console.WriteLine, 
                out publish);


            var subscriber = ZeroMqPublishSubscribe.StartSubscriber(
                "tcp://127.0.0.1:6000", 
                new []{"hans","steff"},
                JsonZeroMqSerialization.ReadTransportMessage,
                Console.WriteLine,
                message => 
                    Console.WriteLine("seqNo:{0}\ntopic: {1}\nheaders: <{2}>\nmessage: {3}\n",
                        message.SequenceNumber,
                        message.Topic,
                        string.Join(", ", message.Headers.Select(pair => string.Format("{0}=>{1}", pair.Key, pair.Value))),
                        Encoding.UTF8.GetString(message.Body)));


            var seqGen = new TopicSpecificSequenceNumberState();
            for (int i = 0; i < 1000; i++)
            {
                var topic = "stefftopic";
                publish(new TransportMessage(topic, seqGen.Next(topic), new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("" + i)));
            }

            //publish(new TransportMessage("stefftopic", new Dictionary<string, object>{{"hest",2}}, Encoding.UTF8.GetBytes("1")));
            //publish(new TransportMessage("stefftopic", new Dictionary<string, object>{{"hest",2}}, Encoding.UTF8.GetBytes("2")));
            //publish(new TransportMessage("stefftopic", new Dictionary<string, object>{{"hest",2}}, Encoding.UTF8.GetBytes("3")));
            //publish(new TransportMessage("stefftopic", new Dictionary<string, object>{{"hest",2}}, Encoding.UTF8.GetBytes("Steffen er en hest")));

            var hanstopic = "hanstopic";
            publish(new TransportMessage(hanstopic, seqGen.Next(hanstopic), new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("1")));
            publish(new TransportMessage(hanstopic, seqGen.Next(hanstopic), new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("2")));
            publish(new TransportMessage(hanstopic, seqGen.Next(hanstopic), new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("3")));
            publish(new TransportMessage(hanstopic, seqGen.Next(hanstopic), new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("Hans er en hest")));


            Console.ReadLine();
            
            publisher.Dispose();
            subscriber.Dispose();

            Console.WriteLine("Cancelled");

            Console.ReadLine();
        }
    }
}

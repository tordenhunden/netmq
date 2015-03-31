using System;
using System.Configuration;
using System.Threading;
using NetMQ;
using NetMQ.PubSub.SeqNoValidated;
using NetMQ.PubSub.Transport;
using RtuBtoker.JsonSerialization;

namespace RtuBroker.Test.Proxy
{
    class Proxy
    {
        static void Main(string[] args)
        {
            var c = NetMQContext.Create();

            var seqVal = new TopicSpecificSequenceNumberValidator();
            var i = 0;
            var termSig = new CancellationTokenSource();

            new Thread(() =>
                ZeroMqPublishSubscribe.Proxy(
                    c,
                    ConfigurationManager.AppSettings["xpubEndpoint"],
                    ConfigurationManager.AppSettings["xsubEndpoint"],
                    m => m,
                    m =>
                    {
                        var msg = JsonSerialization.ReadTransportMessage(m);
                        int exp;
                        var isValid = seqVal.IsValid(msg.Topic, msg.SequenceNumber, out exp);
                        switch (isValid)
                        {
                            case EValid.Valid:
                                if (i++ % 10000 == 0)
                                {
                                    Console.Write(".");
                                }
                                break;
                            case EValid.BadSequenceNumber:
                                Console.WriteLine("BadSequenceNumber {0}, exp {1}", msg.SequenceNumber, exp);
                                break;
                            case EValid.NotSubscribed:
                                Console.WriteLine("Received unsubscribed message{0}", msg.SequenceNumber);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                    },
                    (s, topic) =>
                    {
                        switch (s)
                        {
                            case ESubscriptionAction.Subscribe:
                                seqVal.Subscribe(topic);
                                break;
                            case ESubscriptionAction.Unsubscribe:
                                seqVal.Unsubscribe(topic);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("s");
                        }
                        Console.WriteLine("{0}:{1}", s, topic);
                    },
                    (m, e) => Console.WriteLine("Error: {0}--{1}", m, e),
                    exception => Console.WriteLine("Error in proxy: " + exception),
                    termSig.Token))
            {
                Name = "Proxy"
            }.Start();

            Console.ReadLine();
            termSig.Cancel();
        }
    }
}

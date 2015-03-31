using System;
using System.Configuration;
using System.Linq;
using System.Text;
using NetMQ;
using RtuBroker.ZeroMq.SeqNoValidated;
using RtuBroker.ZeroMq.Transport;
using RtuBtoker.JsonSerialization;
using Msg = RtuBroker.Test.Publisher.Msg;

namespace RtuBroker.Test.Subscriber
{
    class Subscriber
    {
        static void Main(string[] args)
        {
            var context = NetMQContext.Create();

            var count = 0;
            Action<string> subscribe;
            Action<string> unsubscribe;


            SeqNoValidatedPublishSubscribe.StartSubscriber(
                context,
                ConfigurationManager.AppSettings["endpoint"],
                new string[] {},
                JsonSerialization.ReadTransportMessage,
                m => new Msg(m.Topic, m.Headers, m.Body),
                Console.WriteLine, //crash
                (msg, e) => Console.WriteLine(msg + " -- " + e), //userhandler
                (bv, msg) => Console.WriteLine("Wrong seqNo={0}, exp={1}", bv.Actual, bv.Expected),
                msg => Console.WriteLine("Unsubscribed message. topic={0}", msg.Topic),
                m =>
                {
                    if (count++ % 10000 == 0)
                        Console.WriteLine("topic: {0}\nheaders: <{1}>\nmessage: {2}\n",
                            m.Topic,
                            string.Join(", ",
                                m.Headers.Select(pair => string.Format("{0}=>{1}", pair.Key, pair.Value))),
                            Encoding.UTF8.GetString(m.Body));
                },
                out subscribe,
                out unsubscribe);

            Console.WriteLine("subscribe");
            subscribe("steff");

            var i = 0;
            while(Console.ReadLine() != "q")
            {
                i++;

                if (i % 2 == 0)
                {
                    Console.WriteLine("subscribe");
                    subscribe("steff");
                }

                if (i % 2 == 1)
                {
                    Console.WriteLine("unsubscribe");
                    unsubscribe("steff");
                }
            }
            context.Dispose();
            Console.WriteLine("done");
        }
    }
}

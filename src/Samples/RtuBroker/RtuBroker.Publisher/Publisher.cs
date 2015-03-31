using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using NetMQ;
using RtuBroker.ZeroMq.SeqNoValidated;
using RtuBroker.ZeroMq.Transport;
using RtuBtoker.JsonSerialization;

namespace RtuBroker.Test.Publisher
{
    class Publisher
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            
            new Thread(() =>
            {
                Console.ReadLine();
                cts.Cancel();
            })
            {
                Name = "Wait for input"
            }.Start();
            

            Action<Msg> publish;
            using (SeqNoValidatedPublishSubscribe.StartPublisher(
                NetMQContext.Create(),
                ConfigurationManager.AppSettings["endpoint"],
                JsonSerialization.WriteTransportMessage,
                (m, seqNo) => new TransportMessage(m.Topic, seqNo, m.Headers, m.Body),
                m => m.Topic,
                Console.WriteLine,
                out publish))
            {
                Thread.Sleep(1000);
                Console.WriteLine("Started");
                var sw = new Stopwatch();
                sw.Start();
                long i = 0;
                while (!cts.IsCancellationRequested)
                {
                    publish(new Msg("steff", new Dictionary<string, object> { { "hest", 2 } }, Encoding.UTF8.GetBytes("" + i)));

                    if (i % 100 == 0)
                    {
                        Thread.Sleep(10);
                    }

                    if (i % 10000 == 0)
                    {
                        Console.Write(".");
                    }
                    i++;
                }
                sw.Stop();

                Console.WriteLine("Messages: " + i);
                Console.WriteLine("Messages/sec: " + i / (double)sw.ElapsedMilliseconds * 1000);
            }


            Console.WriteLine("done");
            Console.ReadLine();
        }
    }
}

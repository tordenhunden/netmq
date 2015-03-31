﻿using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace NetMQ.SimpleTests
{
    internal abstract class LatencyBenchmarkBase : ITest
    {
        protected const int Iterations = 20000;

        private static readonly int[] s_messageSizes = { 8, 64, 512, 4096, 8192, 16384, 32768 };

        public string TestName { get; protected set; }

        public void RunTest()
        {
            Console.Out.WriteLine(" Iterations: {0:#,##0}", Iterations);
            Console.Out.WriteLine();
            Console.Out.WriteLine(" {0,-6} {1}", "Size", "Latency (µs)");
            Console.Out.WriteLine("---------------------");

            var client = new Thread(ClientThread) { Name = "Client" };
            var server = new Thread(ServerThread) { Name = "Server" };

            server.Start();
            client.Start();

            server.Join();
            client.Join();
        }

        private void ClientThread()
        {
            using (var context = NetMQContext.Create())
            using (var socket = CreateClientSocket(context))
            {
                socket.Connect("tcp://127.0.0.1:9000");

                foreach (int messageSize in s_messageSizes)
                {
                    var ticks = DoClient(socket, messageSize);

                    const long tripCount = Iterations*2;
                    double seconds = (double)ticks/Stopwatch.Frequency;
                    double microsecond = seconds*1000000.0;
                    double microsecondsPerTrip = microsecond / tripCount;

                    Console.Out.WriteLine(" {0,-7} {1,6:0.0}", messageSize, microsecondsPerTrip);
                }
            }
        }

        private void ServerThread()
        {
            using (var context = NetMQContext.Create())
            using (var socket = CreateServerSocket(context))
            {
                socket.Bind("tcp://*:9000");

                foreach (int messageSize in s_messageSizes)
                {
                    DoServer(socket, messageSize);
                }
            }
        }

        [NotNull] protected abstract NetMQSocket CreateClientSocket([NotNull] NetMQContext context);
        [NotNull] protected abstract NetMQSocket CreateServerSocket([NotNull] NetMQContext context);

        protected abstract long DoClient([NotNull] NetMQSocket socket, int messageSize);
        protected abstract void DoServer([NotNull] NetMQSocket socket, int messageSize);
    }

    internal class LatencyBenchmark : LatencyBenchmarkBase
    {
        public LatencyBenchmark()
        {
            TestName = "Req/Rep Latency Benchmark";
        }

        protected override long DoClient(NetMQSocket socket, int messageSize)
        {
            var msg = new byte[messageSize];

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                socket.Send(msg);
                socket.SkipFrame(); // ignore response
            }

            return watch.ElapsedTicks;
        }

        protected override void DoServer(NetMQSocket socket, int messageSize)
        {
            for (int i = 0; i < Iterations; i++)
            {
                byte[] message = socket.ReceiveFrameBytes();
                socket.Send(message);
            }
        }

        protected override NetMQSocket CreateClientSocket(NetMQContext context)
        {
            return context.CreateRequestSocket();
        }

        protected override NetMQSocket CreateServerSocket(NetMQContext context)
        {
            return context.CreateResponseSocket();
        }
    }

    internal class LatencyBenchmarkReusingMsg : LatencyBenchmarkBase
    {
        public LatencyBenchmarkReusingMsg()
        {
            TestName = "Req/Rep Latency Benchmark (reusing Msg)";
        }

        protected override long DoClient(NetMQSocket socket, int messageSize)
        {
            var msg = new Msg();            
            var watch = Stopwatch.StartNew();

            for (int i = 0; i < Iterations; i++)
            {
                msg.InitGC(new byte[messageSize], messageSize);
                socket.Send(ref msg, SendReceiveOptions.None);
                socket.Receive(ref msg);
                msg.Close();
            }

            return watch.ElapsedTicks;
        }

        protected override void DoServer(NetMQSocket socket, int messageSize)
        {
            Msg msg  = new Msg();
            msg.InitEmpty();

            for (int i = 0; i < Iterations; i++)
            {
#pragma warning disable 618
                socket.Receive(ref msg, SendReceiveOptions.None);
#pragma warning restore 618

                socket.Send(ref msg, SendReceiveOptions.None);
            }
        }

        protected override NetMQSocket CreateClientSocket(NetMQContext context)
        {
            return context.CreateRequestSocket();
        }

        protected override NetMQSocket CreateServerSocket(NetMQContext context)
        {
            return context.CreateResponseSocket();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using ZeroMQ;

namespace RtuBroker.ZeroMq
{
    static class ZeroMqPublishSubscribe
    {
        public static IDisposable StartPublisher<T>(string endpoint, Action<ZMessage,T> writeMessage, Action<ZException> zeroMqError, out Action<T> publish)
        {
            var context = ZContext.Create();
            try
            {
                var p = ZSocket.Create(context, ZSocketType.PUB);
                try
                {
                    p.Bind(endpoint);
                    
                    var zmsg = new ZMessage();
                    publish = msg =>
                    {
                        zmsg.Clear();
                        writeMessage(zmsg, msg);

                        ZError error;
                        if (p.Send(zmsg, ZSocketFlags.None, out error)) return;

                        zeroMqError(new ZException(error));
                    };

                    return new LambdaDisposable(() =>
                    {
                        try { p.Dispose(); } catch {}
                        try { context.Dispose(); } catch {}
                    });
                }
                catch
                {
                    p.Dispose();
                    throw;
                }
            }
            catch
            {
                context.Dispose();
                throw;
            }
        }

        public static IDisposable StartSubscriber<T>(
            string endpoint, 
            IEnumerable<string> topics,
            Func<ZMessage,T> readMessage,
            Action<Exception> crashException, 
            Action<T> handler,
            out Action<string> subscribe,
            out Action<string> unsubscribe)
        {
            var tcs = new CancellationTokenSource();
            var connected = new ManualResetEventSlim();
            var ts = new ThreadStart(() => SubscribeLoop(endpoint, topics, readMessage, crashException, handler, connected, tcs.Token));
            var t = new Thread(ts);
            t.Start();

            //TODO subscribe
            subscribe = s => { };
            unsubscribe = s => { };

            return new LambdaDisposable(tcs.Cancel);
        }

        private static void SubscribeLoop<T>(
            string endpoint, 
            IEnumerable<string> topics, 
            Func<ZMessage, T> readMessage,
            Action<Exception> crashException, 
            Action<T> handler, 
            ManualResetEventSlim connected,
            CancellationToken token)
        {
            try
            {
                using (var context = ZContext.Create())
                using (var subscriber = ZSocket.Create(context, ZSocketType.SUB))
                {
                    ZError cError;
                    if (!subscriber.Connect(endpoint, out cError))
                    {
                        throw new ZException(cError);
                    }
                    
                    connected.Set();
                    foreach (var topic in topics)
                    {
                        subscriber.Subscribe(topic);
                    }

                    var zmsg = new ZMessage();
                    while (!token.IsCancellationRequested)
                    {
                        zmsg.Clear();
                        ZError error;
                        if (!subscriber.ReceiveMessage(ref zmsg, ZSocketFlags.DontWait, out error))
                        {
                            if (error == ZError.ETERM)
                                break; // Interrupted
                            if (error == ZError.EAGAIN)
                            {
                                Thread.Sleep(1);
                                continue;
                            }
                            throw new ZException(error);
                        }

                        handler(readMessage(zmsg));
                    }
                }
            }
            catch (Exception e)
            {
                crashException(e);
            }
        }
    }
}
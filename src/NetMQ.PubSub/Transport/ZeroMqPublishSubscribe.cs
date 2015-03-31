using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NetMQ.PubSub.Transport
{
    public static class ZeroMqPublishSubscribe
    {
        public static void Proxy<T>(
            NetMQContext context,
            string pubEndpoint,
            string subEndpoint,
            Func<NetMQMessage, T> readMessage,
            Action<T> handleMessage,
            Action<ESubscriptionAction,string> handleSubscription,
            Action<NetMQMessage,Exception> handlerError,
            Action<Exception> crash,
            CancellationToken cancel)
        {
            var internalTerm = new CancellationTokenSource();
            var termSig = CancellationTokenSource.CreateLinkedTokenSource(internalTerm.Token,cancel);

            var listenerEndpoint = "inproc://listener_" + Guid.NewGuid();

            using (var p = context.CreateXPublisherSocket())
            using (var s = context.CreateXSubscriberSocket())
            using (var l = context.CreatePairSocket())
            {
                s.Connect(subEndpoint);
                p.Bind(pubEndpoint);
                l.Bind(listenerEndpoint);

                var poller = new Poller(p, s, l) {PollTimeout = 0};

                var t = new Thread(() => ListenerLoop(context, readMessage, handleMessage, handlerError, crash, listenerEndpoint, termSig.Token))
                {
                    Name = "ProxyListener: " + listenerEndpoint
                };
                t.Start();

                s.ReceiveReady += (sender, a) =>
                {
                    var m = a.Socket.ReceiveMultipartMessage();
                    p.SendMessage(m);
                    l.SendMessage(m);
                };

                p.ReceiveReady += (sender, a) =>
                {
                    var m = a.Socket.ReceiveMultipartMessage();
                    s.SendMessage(m);

                    try
                    {
                        var frame = m.Pop();
                        var sAction = frame.Buffer[0] == 1
                            ? ESubscriptionAction.Subscribe
                            : ESubscriptionAction.Unsubscribe;
                        var topic = Encoding.UTF8.GetString(frame.Buffer, 1, frame.MessageSize - 1);
                        handleSubscription(sAction, topic);
                    }
                    catch (Exception e)
                    {
                        handlerError(m, e);
                    }
                };

                poller.PollTillCancelledAnd(() => !termSig.Token.IsCancellationRequested);
                termSig.Cancel();
            }
        }

        private static void ListenerLoop<T>(
            NetMQContext context, 
            Func<NetMQMessage, T> readMessage,
            Action<T> handleMessage, 
            Action<NetMQMessage, Exception> handlerError,
            Action<Exception> crash, 
            string listenerEndpoint, 
            CancellationToken termSig)
        {
            try
            {
                using (var l = context.CreatePairSocket())
                {
                    var poller = new Poller(l);
                    l.Connect(listenerEndpoint);

                    l.ReceiveReady += (sender, a) =>
                    {
                        var m = a.Socket.ReceiveMultipartMessage();
                        try
                        {
                            handleMessage(readMessage(m));
                        }
                        catch (Exception e)
                        {
                            handlerError(m, e);
                        }
                    };

                    poller.PollTillCancelledAnd(() => !termSig.IsCancellationRequested);
                }
            }
            catch (Exception e)
            {
                crash(e);
            }
        }


        public static IDisposable StartPublisher<T>(
            NetMQContext context,
            string endpoint,
            Action<NetMQMessage, T> writeMessage,
            Action<NetMQException> zeroMqError, 
            out Action<T> publish)
        {
            var p = context.CreatePublisherSocket();
            p.Bind(endpoint);
                    
            var zmsg = new NetMQMessage();
            publish = msg =>
            {
                zmsg.Clear();
                writeMessage(zmsg, msg);

                p.SendMessage(zmsg);
            };

            return new LambdaDisposable(() =>
            {
                try { p.Dispose(); } catch {}
            });
        }

        public static void StartSubscriber<T>(
            NetMQContext context,
            string endpoint,
            IEnumerable<string> topics,
            Func<NetMQMessage, T> readMessage,
            Action<Exception> crash,
            Action<NetMQMessage, Exception> userHandlerException,
            Action<T> handler,
            Action<string> subscribed,
            Action<string> unsubscribed,
            out Action<string> subscribe,
            out Action<string> unsubscribe)
        {
            var subscriptionProxyEndpoint = "inproc://" + Guid.NewGuid();

            var inProcBindDone = new ManualResetEventSlim();
            var termSig = new CancellationTokenSource();


            new Thread(() =>
            {
                try
                {
                    Subscribe(
                        context, endpoint, subscriptionProxyEndpoint, topics, readMessage, userHandlerException,
                        handler, subscribed, unsubscribed, inProcBindDone, termSig.Token);
                }
                catch
                {
                    termSig.Cancel();
                }
            })
            {
                Name = "IncomingMessageHandler"
            }.Start();

            inProcBindDone.Wait(termSig.Token);

            var sharedSubscriber = new ConcurrentZeroMqSubscriber(context, subscriptionProxyEndpoint, termSig.Token);
            new Thread(() =>
            {
                try
                {
                    sharedSubscriber.Consume();
                }
                catch
                {
                    termSig.Cancel();
                }
            })
            {
                Name = "SubscriptionPropagator"
            }.Start();

            subscribe = sharedSubscriber.Subscribe;
            unsubscribe = sharedSubscriber.Unsubscribe;
        }

        private static void Subscribe<T>(
            NetMQContext context, 
            string endpoint, 
            string proxyEndpoint,
            IEnumerable<string> topics,
            Func<NetMQMessage, T> readMessage,
            Action<NetMQMessage, Exception> userHandlerException,
            Action<T> handler, 
            Action<string> subscribed,
            Action<string> unsubscribed,
            ManualResetEventSlim inProcBindDone,
            CancellationToken termSig)
        {
            using (var subscriber = context.CreateSubscriberSocket())
            using (var subscriptionsProxy = context.CreatePullSocket())
            {
                var poller = new Poller(subscriber, subscriptionsProxy)
                {
                    PollTimeout = 0
                };
                subscriptionsProxy.Bind(proxyEndpoint);
                inProcBindDone.Set();
                subscriber.Connect(endpoint);
                
                foreach (var topic in topics)
                {
                    subscriber.Subscribe(topic);
                }


                subscriptionsProxy.ReceiveReady += (sender, a) =>
                {
                    var zmsg = a.Socket.ReceiveMultipartMessage();
                    var action = (ESubscriptionAction)zmsg.Pop().ConvertToInt32();
                    var topic = zmsg.Pop().ConvertToString(Encoding.UTF8);

                    if (action == ESubscriptionAction.Subscribe)
                    {
                        subscriber.Subscribe(topic);
                        subscribed(topic);
                    }
                    if (action == ESubscriptionAction.Unsubscribe)
                    {
                        subscriber.Unsubscribe(topic);
                        unsubscribed(topic);
                    }
                };

                subscriber.ReceiveReady += (sender, a) =>
                {
                    var zmsg = a.Socket.ReceiveMultipartMessage();
                    try
                    {
                        handler(readMessage(zmsg));
                    }
                    catch (Exception e)
                    {
                        userHandlerException(zmsg, e);
                    }
                };

                poller.PollTillCancelledAnd(() => !termSig.IsCancellationRequested);
            }
        }


        private class ConcurrentZeroMqSubscriber
        {
            private readonly NetMQContext _context;
            private readonly string _proxyEndpoint;
            private readonly CancellationToken _termSig;
            private readonly BlockingCollection<Tuple<ESubscriptionAction, string>> _queue = new BlockingCollection<Tuple<ESubscriptionAction, string>>(100);

            public ConcurrentZeroMqSubscriber(NetMQContext context, string proxyEndpoint, CancellationToken termSig)
            {
                _context = context;
                _proxyEndpoint = proxyEndpoint;
                _termSig = termSig;
            }

            public void Subscribe(string topic)
            {
                try
                {
                    _queue.Add(Tuple.Create(ESubscriptionAction.Subscribe, topic), _termSig);
                }
                catch (OperationCanceledException){}
            }

            public void Unsubscribe(string topic)
            {
                try
                {
                    _queue.Add(Tuple.Create(ESubscriptionAction.Unsubscribe, topic), _termSig);
                }
                catch (OperationCanceledException){}
            }

            public void Consume()
            {
                using (var proxy = _context.CreatePushSocket())
                {
                    proxy.Connect(_proxyEndpoint);

                    while (!_termSig.IsCancellationRequested)
                    {
                        Tuple<ESubscriptionAction, string> v;
                        if (!_queue.TryTake(out v, 1))
                        {
                            continue;
                        }
                        var m = new NetMQMessage();
                        
                        m.Push(v.Item2);
                        m.Push((int)v.Item1);

                        proxy.SendMessage(m);
                    }
                }
            }
        }
    }
}
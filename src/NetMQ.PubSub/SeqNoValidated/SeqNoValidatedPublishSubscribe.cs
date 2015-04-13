using System;
using System.Collections.Generic;
using NetMQ.PubSub.Transport;

namespace NetMQ.PubSub.SeqNoValidated
{
    public static class SeqNoValidatedPublishSubscribe
    {
        public static IDisposable StartPublisher<T>(
            NetMQContext context, 
            string endpoint,
            Action<NetMQMessage, TransportMessage> writeMessage,
            Func<T,byte[]> serialize,
            Action<NetMQException> zeroMqError, 
            out Action<string,Dictionary<string,object>,T> publish)
        {
            var seqNoGen = new TopicSpecificSequenceNumberGenerator();

            Action<TransportMessage> innerPublish;
            var pub = ZeroMqPublishSubscribe.StartPublisher(context, endpoint, writeMessage, zeroMqError, out innerPublish);
            publish = (topic,headers,msg) =>
            {
                var seqNo = seqNoGen.Gen(topic);
                innerPublish(new TransportMessage(topic,seqNo,headers,serialize(msg)));
            };
            return pub;
        }

        public static void StartSubscriber<T>(
            NetMQContext context,
            string endpoint,
            IEnumerable<string> topics,
            Func<NetMQMessage, TransportMessage> readMessage,
            Func<TransportMessage,T> createTMessage,
            Action<Exception> crash,
            Action<NetMQMessage, Exception> userHandlerException,
            Action<BadValue<int>, T> receivedWrongSeqNo,
            Action<string,T> receivedNotSubscribedMessage,
            Action<string,Dictionary<string,object>,T> handler,
            out Action<string> subscribe,
            out Action<string> unsubscribe)
        {
            var seqNoVal = new TopicSpecificSequenceNumberValidator();

            Action<TransportMessage> h = m =>
            {
                var tM = createTMessage(m);
                int exp;

                switch (seqNoVal.IsValid(m.Topic, m.SequenceNumber, out exp))
                {
                    case EValid.Valid:
                        handler(m.Topic,m.Headers,tM);
                        break;
                    case EValid.BadSequenceNumber:
                        receivedWrongSeqNo(new BadValue<int>(exp, m.SequenceNumber), tM);
                        break;
                    default:
                        receivedNotSubscribedMessage(m.Topic, tM);
                        break;
                }
            };
            Action<string> unsubscribeInner;
            Action<string> subscribeInner;
            ZeroMqPublishSubscribe.StartSubscriber(
                context,
                endpoint, 
                topics, 
                readMessage, 
                crash,
                userHandlerException, 
                h, 
                s => { }, 
                seqNoVal.Unsubscribe,
                out subscribeInner,
                out unsubscribeInner);

            subscribe = s =>
            {
                seqNoVal.Subscribe(s);
                subscribeInner(s);
            };

            unsubscribe = s =>
            {
                seqNoVal.Unsubscribe(s);
                unsubscribeInner(s);
            };
        }
    }

    class TopicSpecificSequenceNumberGenerator
    {
        private readonly Dictionary<string, SeqNoContainer> _dic = new Dictionary<string, SeqNoContainer>();

        public int Gen(string topic)
        {
            SeqNoContainer seqNoContainer;
            if (!_dic.TryGetValue(topic, out seqNoContainer))
            {
                seqNoContainer = new SeqNoContainer();
                _dic.Add(topic, seqNoContainer);
            }

            return seqNoContainer.SeqNo++;
        }

        internal class SeqNoContainer
        {
            public int SeqNo;
        }
    }

    public class TopicSpecificSequenceNumberValidator
    {
        private readonly Dictionary<string, int> _lastSeqNoDic = new Dictionary<string, int>();

        public EValid IsValid(string topic, int seqNo, out int expectedSeqNo)
        {
            int lastSeqNo;
            if (!_lastSeqNoDic.TryGetValue(topic, out lastSeqNo))
            {
                expectedSeqNo = -1;
                return EValid.NotSubscribed;
            }

            //fresh subscription. Always accept
            if (lastSeqNo == -1)
            {
                lastSeqNo = seqNo - 1;
            }

            expectedSeqNo = lastSeqNo + 1;

            //always line-in to new seqno, so holes only show once
            _lastSeqNoDic[topic] = seqNo;

            return seqNo == expectedSeqNo ? EValid.Valid : EValid.BadSequenceNumber;
        }

        public void Unsubscribe(string topic)
        {
            _lastSeqNoDic.Remove(topic);
        }

        public void Subscribe(string topic)
        {
            _lastSeqNoDic.Add(topic, -1);
        }

        public bool IsSubscribed(string topic)
        {
            return _lastSeqNoDic.ContainsKey(topic);
        }
    }

    public enum EValid
    {
        Valid, BadSequenceNumber, NotSubscribed
    }

    public class BadValue<T>
    {
        public T Expected { get; private set; }
        public T Actual { get; private set; }

        public BadValue(T expected, T actual)
        {
            Expected = expected;
            Actual = actual;
        }
    }
}

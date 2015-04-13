using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ.PubSub.SeqNoValidated;

namespace NetMQ.UnitTests
{
    [TestClass]
    public class IntegrationTests
    {

        [TestMethod]
        public void M()
        {
            var ctx = NetMQContext.Create();
            var gen = new TopicSpecificSequenceNumberGenerator();

            Action<string, Dictionary<string, object>, string> publish;
            SeqNoValidatedPublishSubscribe.StartPublisher(
                ctx,
                "tcp://localhost:111",
                JsonSerialization.WriteTransportMessage,
                s => Encoding.ASCII.GetBytes(s),
                exception => Assert.Fail(),
                out publish);


            var countDownEvent = new CountdownEvent(5);

            Action<string> subscribe;
            Action<string> unsubscribe;
            SeqNoValidatedPublishSubscribe.StartSubscriber(
                ctx,
                "tcp://localhost:111",
                new string[]{},
                JsonSerialization.ReadTransportMessage,
                msg => Encoding.ASCII.GetString(msg.Body),
                exception => Assert.Fail(),
                (message, exception) => Assert.Fail(),
                (value, s) => Assert.Fail("Wwrong seq no"),
                (topic,s) => Assert.Fail("Received unsubscribed msg"),
                (topic,headers,s) => countDownEvent.Signal(),
                out subscribe,
                out unsubscribe);

            subscribe("topic");

            var emptyheaders = new Dictionary<string, object>();

            publish("topic", emptyheaders, "message1");
            publish("topic", emptyheaders, "message2");
            publish("topic", emptyheaders, "message3");
            publish("topic", emptyheaders, "message4");
            publish("topic", emptyheaders, "message5");

            if (!countDownEvent.Wait(50))
            {
                Assert.Fail("Timed out");
            }
            //received 5 events
            
        }
    }
}

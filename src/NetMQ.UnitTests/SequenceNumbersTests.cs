using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ.PubSub.SeqNoValidated;

namespace NetMQ.UnitTests
{
    [TestClass]
    public class SequenceNumbersTests
    {
        [TestMethod]
        public void Gen()
        {
            var gen = new TopicSpecificSequenceNumberGenerator();
            var i0 = gen.Gen("Horse");
            var i1 = gen.Gen("Horse");
            var i2 = gen.Gen("Horse");
            var i3 = gen.Gen("Horse");

            var j0 = gen.Gen("Hest");
            var j1 = gen.Gen("Hest");
            var j2 = gen.Gen("Hest");
            var j3 = gen.Gen("Hest");

            Assert.AreEqual(0, i0);
            Assert.AreEqual(1, i1);
            Assert.AreEqual(2, i2);
            Assert.AreEqual(3, i3);

            Assert.AreEqual(0, j0);
            Assert.AreEqual(1, j1);
            Assert.AreEqual(2, j2);
            Assert.AreEqual(3, j3);
        }

        [TestMethod]
        public void GenValidate()
        {
            int exp;
            const string topic = "Horse";

            var gen = new TopicSpecificSequenceNumberGenerator();
            var validate = new TopicSpecificSequenceNumberValidator();



            Assert.IsFalse(validate.IsSubscribed(topic));
            validate.Subscribe(topic);
            Assert.IsTrue(validate.IsSubscribed(topic));


            var i0 = gen.Gen(topic);
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, i0, out exp));
            Assert.IsTrue(validate.IsSubscribed(topic));

            var i1 = gen.Gen(topic);
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, i1, out exp));
            Assert.IsTrue(validate.IsSubscribed(topic));

            var i2 = gen.Gen(topic);
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, i2, out exp));
            Assert.IsTrue(validate.IsSubscribed(topic));

            var i3 = gen.Gen(topic);
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, i3, out exp));
            Assert.IsTrue(validate.IsSubscribed(topic));

            validate.Unsubscribe(topic);
            Assert.IsFalse(validate.IsSubscribed(topic));
        }

        [TestMethod]
        public void ValidateNegative()
        {
            int exp;
            const string topic = "Horse";
            var validate = new TopicSpecificSequenceNumberValidator();

            Assert.IsFalse(validate.IsSubscribed(topic));
            validate.Subscribe(topic);
            Assert.IsTrue(validate.IsSubscribed(topic));

            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, 0, out exp));
            Assert.IsTrue(validate.IsSubscribed(topic));

            Assert.AreEqual(EValid.BadSequenceNumber, validate.IsValid(topic, 3, out exp));

            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, 4, out exp));
        }


        [TestMethod]
        public void ValidateNegative2()
        {
            int exp;
            const string topic = "Horse";
            var validate = new TopicSpecificSequenceNumberValidator();

            Assert.AreEqual(EValid.NotSubscribed, validate.IsValid(topic, 0, out exp));
            validate.Subscribe(topic);
            
            //first always valid
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, 1, out exp));
            Assert.AreEqual(EValid.Valid, validate.IsValid(topic, 2, out exp));
        }
    }
}

﻿using System;
using NUnit.Framework;

namespace NetMQ.Tests
{
    [TestFixture]
    public class SocketOptionsTests
    {
        [Test]
        public void DefaultValues()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreateRouterSocket())
            {
                Assert.IsNull(socket.Options.Identity);
//                Assert.IsNull(socket.Options.TcpAcceptFilter);
                Assert.AreEqual(false, socket.Options.ReceiveMore);
            }
        }

        [Test]
        public void GetAndSetAllProperties()
        {
            using (var context = NetMQContext.Create())
            using (var socket = context.CreateRouterSocket())
            {
                socket.Options.Affinity = 1L;
                Assert.AreEqual(1L, socket.Options.Affinity);

                socket.Options.Identity = new[] { (byte)1 };
                Assert.AreEqual(1, socket.Options.Identity.Length);
                Assert.AreEqual(1, socket.Options.Identity[0]);

                socket.Options.MulticastRate = 100;
                Assert.AreEqual(100, socket.Options.MulticastRate);

                socket.Options.MulticastRecoveryInterval = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.MulticastRecoveryInterval);

                socket.Options.ReceiveBuffer = 100;
                Assert.AreEqual(100, socket.Options.ReceiveBuffer);

//                socket.Options.ReceiveMore = true;

                socket.Options.Linger = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.Linger);

                socket.Options.ReconnectInterval = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.ReconnectInterval);

                socket.Options.ReconnectIntervalMax = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.ReconnectIntervalMax);

                socket.Options.Backlog = 100;
                Assert.AreEqual(100, socket.Options.Backlog);

                socket.Options.MaxMsgSize = 100;
                Assert.AreEqual(100, socket.Options.MaxMsgSize);

                socket.Options.SendHighWatermark = 100;
                Assert.AreEqual(100, socket.Options.SendHighWatermark);

                socket.Options.ReceiveHighWatermark = 100;
                Assert.AreEqual(100, socket.Options.ReceiveHighWatermark);

                socket.Options.MulticastHops = 100;
                Assert.AreEqual(100, socket.Options.MulticastHops);

#pragma warning disable 618
                socket.Options.ReceiveTimeout = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.ReceiveTimeout);
#pragma warning restore 618

                socket.Options.SendTimeout = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.SendTimeout);

                socket.Options.IPv4Only = true;
                Assert.AreEqual(true, socket.Options.IPv4Only);

                Assert.IsNull(socket.Options.LastEndpoint);

                socket.Options.RouterMandatory = true;
//                Assert.AreEqual(true, socket.Options.RouterMandatory);

                socket.Options.TcpKeepalive = true;
                Assert.AreEqual(true, socket.Options.TcpKeepalive);

//                socket.Options.TcpKeepaliveCnt = 100;
//                Assert.AreEqual(100, socket.Options.TcpKeepaliveCnt);

                socket.Options.TcpKeepaliveIdle = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.TcpKeepaliveIdle);

                socket.Options.TcpKeepaliveInterval = TimeSpan.FromMilliseconds(100);
                Assert.AreEqual(TimeSpan.FromMilliseconds(100), socket.Options.TcpKeepaliveInterval);

#pragma warning disable 618
                socket.Options.TcpAcceptFilter = "1.0.0.0:1234";
#pragma warning restore 618
//                Assert.AreEqual("1.0.0.0:1234", socket.Options.TcpAcceptFilter);

                socket.Options.DelayAttachOnConnect = true;
                Assert.AreEqual(true, socket.Options.DelayAttachOnConnect);

                socket.Options.RouterRawSocket = true;
//                Assert.AreEqual(true, socket.Options.RouterRawSocket);

                socket.Options.Endian = Endianness.Little;
                Assert.AreEqual(Endianness.Little, socket.Options.Endian);

                Assert.IsFalse(socket.Options.DisableTimeWait);
                socket.Options.DisableTimeWait = true;
                Assert.IsTrue(socket.Options.DisableTimeWait);
            }

            using (var context = NetMQContext.Create())
            using (var socket = context.CreateXPublisherSocket())
            {
                socket.Options.XPubVerbose = true;
//                Assert.AreEqual(true, socket.Options.XPubVerbose);

                socket.Options.ManualPublisher = true;
//                Assert.AreEqual(true, socket.Options.ManualPublisher);
            }
        }
    }
}

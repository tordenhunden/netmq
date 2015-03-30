using System;
using System.Threading;

namespace RtuBroker.ZeroMq
{
    public sealed class GcMonitor
    {
        private static volatile GcMonitor instance;
        private static object syncRoot = new object();

        private Thread gcMonitorThread;
        private ThreadStart gcMonitorThreadStart;

        private bool isRunning;

        public static GcMonitor GetInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    instance = new GcMonitor();
                }
            }

            return instance;
        }

        private GcMonitor()
        {
            isRunning = false;
            gcMonitorThreadStart = new ThreadStart(DoGCMonitoring);
            gcMonitorThread = new Thread(gcMonitorThreadStart);
        }

        public void StartGcMonitoring()
        {
            if (!isRunning)
            {
                gcMonitorThread.Start();
                isRunning = true;
            }
        }

        private void DoGCMonitoring()
        {
            long beforeGC = 0;
            long afterGC = 0;

            try
            {
                GC.RegisterForFullGCNotification(1,1);
                while (true)
                {
                    // Check for a notification of an approaching collection.
                    GCNotificationStatus s = GC.WaitForFullGCApproach(10000);
                    if (s == GCNotificationStatus.Succeeded)
                    {
                        //Call event
                        beforeGC = GC.GetTotalMemory(false);
                        Console.Write("GC");
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC is about to begin. Memory before GC: %d", beforeGC);
                        GC.Collect();

                    }
                    else if (s == GCNotificationStatus.Canceled)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC about to begin event was cancelled");
                    }
                    else if (s == GCNotificationStatus.Timeout)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC about to begin event was timeout");
                    }
                    else if (s == GCNotificationStatus.NotApplicable)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC about to begin event was not applicable");
                    }
                    else if (s == GCNotificationStatus.Failed)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC about to begin event failed");
                    }

                    // Check for a notification of a completed collection.
                    s = GC.WaitForFullGCComplete(10000);
                    if (s == GCNotificationStatus.Succeeded)
                    {
                        //Call event
                        afterGC = GC.GetTotalMemory(false);
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC has ended. Memory after GC: %d", afterGC);

                        long diff = beforeGC - afterGC;

                        if (diff > 0)
                        {
                            //Console.WriteLine("===> GC <=== " + Environment.NewLine + "Collected memory: %d", diff);
                        }

                    }
                    else if (s == GCNotificationStatus.Canceled)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC finished event was cancelled");
                    }
                    else if (s == GCNotificationStatus.Timeout)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC finished event was timeout");
                    }
                    else if (s == GCNotificationStatus.NotApplicable)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC finished event was not applicable");
                    }
                    else if (s == GCNotificationStatus.Failed)
                    {
                        //Console.WriteLine("===> GC <=== " + Environment.NewLine + "GC finished event failed");
                    }

                    Thread.Sleep(1500);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("  ********************   Garbage Collector Error  ************************ ");
                Console.WriteLine(e);
                Console.WriteLine("  -------------------   Garbage Collector Error  --------------------- ");
            }
        }

    }
}
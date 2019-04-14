using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Messaging;
using System.IO.Ports;
using Samraksh.SPOT.Emulator.Network;

namespace Smart_Bus
{
    public class RequestDriver
    {
        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        private static readonly int TIME_MULTIPLIER = 1;

        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / TIME_MULTIPLIER;
        }

        private static RequestDriver instance;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        static UInt16 appId;

        private RequestDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        private void NetworkBroadcast(byte[] msg, int size)
        {
            NetPort.Broadcast(msg, size);
        }

        private static void BuildAndBroadcast(object obj)
        {
            Request req = obj as Request;
            String msg = BuildMessage(req);
            Debug.Print("Sending message " + msg);
            RequestDriver driver = RequestDriver.getInstance();
            byte[] msgBytes = Utilities.StringToByteArray(msg);
            driver.NetworkBroadcast(msgBytes, msg.Length);
        }
        
        public static RequestDriver getInstance()
        {
            if (RequestDriver.instance == null)
            {
                RequestDriver.instance = new RequestDriver();
            }
            return RequestDriver.instance;
        }

        private static string BuildMessage(Request request)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(appId.ToString() + " ");
            msg.Append(request.origin.id.ToString() + " ");
            msg.Append(request.destination.id.ToString() + " ");
            msg.Append(request.earliestServingTime.ToString() + " ");
            msg.Append(request.latestServingTime.ToString());
            return msg.ToString();
        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {
            // Received message from network (probably a broadcast from a stop or bus).
            // We don't need to do anything about it.

            String msgString = Utilities.ByteArrayToString(msg);
            Debug.Print("Received message: " + msgString);
        }

        public static void Main()
        {
            RequestDriver driver = RequestDriver.getInstance();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            DateTime startTime = DateTime.Now;
            Debug.Print("startTime = " + startTime.ToString("HH:mm:ss.fff"));
            IRequestPattern pattern = new RequestPattern_2P_2S();
            int i = 0;

            while (pattern.remainingRequests() > 0)
            {
                Request request = pattern.getNextRequest();
                TimeSpan elapsedTime = DateTime.Now - startTime;
                int elapsedMillis = (int)elapsedTime.Milliseconds;
                int delayTilSend = System.Math.Max(0, getRealSendTime(request.earliestServingTime) - elapsedMillis);
                new Timer(new TimerCallback(BuildAndBroadcast), request, delayTilSend, 0);
                Debug.Print("Scheduled request " + i++ + " for " + delayTilSend + " ms");
            }

            Thread.Sleep(Timeout.Infinite);            
        }

    }
}

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

        private static int GetRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
        }

        private static RequestDriver instance;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        IRequestPattern pattern;
        static UInt16 appId;

        private RequestDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        private static void BuildAndSendStartSimBroadcast(DateTime startTime)
        {
            RequestDriver driver = RequestDriver.getInstance();
            SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.PASSENGER);
            SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint();
            IMessagePayload payload = new PayloadSimStart(startTime, driver.pattern.numberOfBuses());
            SBMessage message = new SBMessage(SBMessage.MessageType.START_SIMULATION, origin, destination, payload);
            message.Broadcast(RequestDriver.getInstance().NetPort);
        }

        private static void BuildAndSendPassengerRequest(object obj)
        {
            Request req = obj as Request;
            SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.PASSENGER);
            SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS_STOP, req.origin.stop.id);
            IMessagePayload payload = req;
            SBMessage message = new SBMessage(SBMessage.MessageType.SEND_PASSENGER_REQUEST, origin, destination, payload);
            message.Broadcast(RequestDriver.getInstance().NetPort);
        }

        public static RequestDriver getInstance()
        {
            if (RequestDriver.instance == null)
            {
                RequestDriver.instance = new RequestDriver();
            }
            return RequestDriver.instance;
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
            driver.pattern = new RequestPattern_1P_2S_1B();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            DateTime startTime = DateTime.Now;
            Utilities.SimStart = startTime;
            BuildAndSendStartSimBroadcast(startTime);
            Debug.Print("startTime = " + startTime.ToString("HH:mm:ss.fff"));
            int i = 0;

            while (driver.pattern.remainingRequests() > 0)
            {
                Request request = driver.pattern.NextRequest();
                int elapsedMillis = Utilities.ElapsedMillis();
                int delayTilSend = System.Math.Max(0, GetRealSendTime(request.origin.earliestServingTime) - elapsedMillis);
                new Timer(new TimerCallback(BuildAndSendPassengerRequest), request, delayTilSend, 0);
                Debug.Print("Scheduled request " + i++ + " for " + delayTilSend + " ms");
            }

            Thread.Sleep(Timeout.Infinite);
        }

    }
}

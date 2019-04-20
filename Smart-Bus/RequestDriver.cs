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

        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
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

        //private void NetworkBroadcast(byte[] msg, int size)
        //{
        //    NetPort.Broadcast(msg, size);
        //}

        private static void BuildAndSendStartSimBroadcast(DateTime startTime)
        {
            SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.PASSENGER);
            SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint();
            IMessagePayload payload = new PayloadDateTime(startTime);
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

            //String msg = BuildMessage(req);
            //Debug.Print("Sending message " + msg);
            //RequestDriver driver = RequestDriver.getInstance();
            //byte[] msgBytes = Utilities.StringToByteArray(msg);
            //driver.NetworkBroadcast(msgBytes, msg.Length);
        }

        public static RequestDriver getInstance()
        {
            if (RequestDriver.instance == null)
            {
                RequestDriver.instance = new RequestDriver();
            }
            return RequestDriver.instance;
        }

        //private static string BuildMessage(Request request)
        //{
        //    StringBuilder msg = new StringBuilder();
        //    msg.Append(appId.ToString() + " ");
        //    msg.Append(request.origin.stop.id.ToString() + " ");
        //    msg.Append(request.destination.stop.id.ToString() + " ");
        //    msg.Append(request.earliestPickupTime.ToString() + " ");
        //    msg.Append(request.earliestDeliveryTime.ToString());
        //    return msg.ToString();
        //}

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
            BuildAndSendStartSimBroadcast(startTime);
            Debug.Print("startTime = " + startTime.ToString("HH:mm:ss.fff"));
            IRequestPattern pattern = new RequestPattern_2P_2S_2B();
            int i = 0;

            while (pattern.remainingRequests() > 0)
            {
                Request request = pattern.getNextRequest();
                TimeSpan elapsedTime = DateTime.Now - startTime;
                int elapsedMillis = (int)elapsedTime.Milliseconds;
                int delayTilSend = System.Math.Max(0, getRealSendTime(request.origin.earliestServingTime) - elapsedMillis);
                Debug.Print("--------\nScheduling request " + i + " for " + delayTilSend + " ms");
                new Timer(new TimerCallback(BuildAndSendPassengerRequest), request, delayTilSend, 0);
                Debug.Print("Scheduled request " + i++ + " for " + delayTilSend + " ms\n--------");
            }

            Thread.Sleep(Timeout.Infinite);
        }

    }
}

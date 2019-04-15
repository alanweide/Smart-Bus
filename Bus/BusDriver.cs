using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Messaging;
using System.IO.Ports;
using Samraksh.SPOT.Emulator.Network;
using Smart_Bus;

namespace Smart_Bus
{
    public class BusDriver
    {
        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        private static readonly int TIME_MULTIPLIER = 1;

        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / TIME_MULTIPLIER;
        }

        private static BusDriver instance;

        // Requires an id; this will be equivalent to appId, so do not create bus until after appId is initialized
        Bus myBus;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        static UInt16 appId;

        private BusDriver()
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
            BusDriver driver = BusDriver.getInstance();
            byte[] msgBytes = Utilities.StringToByteArray(msg);
            driver.NetworkBroadcast(msgBytes, msg.Length);
        }

        public static BusDriver getInstance()
        {
            if (BusDriver.instance == null)
            {
                BusDriver.instance = new BusDriver();
            }
            return BusDriver.instance;
        }

        private static string BuildMessage(Request request)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(appId.ToString() + " ");
            msg.Append(request.origin.id.ToString() + " ");
            msg.Append(request.destination.id.ToString() + " ");
            msg.Append(request.earliestPickupTime.ToString() + " ");
            msg.Append(request.earliestDeliveryTime.ToString());
            return msg.ToString();
        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {
            // Received message from network
            // There are a few possibilities for this message:
            //  If broadcast from a stop, decide if we're close enough to respond
            //  (i.e., the stop that sent it is on our route in the future),
            //  then respond with our route info and current location.
            // If it's a "start simulation" message from the RequestDriver,
            //  keep track of the start time to know current location
            // If it's any other message, or we aren't close enough to the stop to
            // reply, then ignore it.

            string msgString = Utilities.ByteArrayToString(msg);
            Debug.Print("Received message: " + msgString);
            if (isStartSimulationMessage(msgString))
            {
                
            }
            else if (isBusStopBroadcast(msgString))
            {
                int stopId = 0;
                if (BusDriver.getInstance().myBus.futureRouteContains(stopId))
                {
                    // TODO: send reply
                }
            }
        }

        private static bool isStartSimulationMessage(string msg)
        {
            // TODO: Implement this
            return true;
        }

        private static bool isBusStopBroadcast(string msg)
        {
            // TODO: Implement this
            return true;
        }

        public static void Main()
        {
            BusDriver driver = BusDriver.getInstance();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            instance.myBus = new Smart_Bus.Bus(appId, 0, 0, 100000);

            Thread.Sleep(Timeout.Infinite);
        }

    }
}

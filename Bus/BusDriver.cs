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
        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
        }

        private static BusDriver instance;

        // Requires an id, which is equivalent to appId, so do not create bus until after appId is initialized
        Bus myBus;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        private static UInt16 appId;
        private static DateTime SimStart;

        private BusDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        private void NetworkBroadcast(byte[] msg, int size)
        {
            NetPort.Broadcast(msg, size);
        }

        private static void BroadcastMessage(string msg)
        {
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

        //private static string BuildMessage(Request request)
        //{
        //    StringBuilder msg = new StringBuilder();
        //    msg.Append(appId.ToString() + " ");
        //    msg.Append(request.origin.id.ToString() + " ");
        //    msg.Append(request.destination.id.ToString() + " ");
        //    msg.Append(request.earliestPickupTime.ToString() + " ");
        //    msg.Append(request.earliestDeliveryTime.ToString());
        //    return msg.ToString();
        //}

        private static string BuildRouteReply(int stopId)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(appId.ToString() + " ");
            Bus bus = getInstance().myBus;
            msg.Append(bus.id.ToString() + " ");
            for (int i = 0; i < bus.route.Count; i++)
            {
                msg.Append(bus.route[i].stopId + "," + bus.route[i].duration + " ");
            }
            return msg.ToString();
        }

        // produces a message to stopId either confirming or rejecting a route addition
        private static string BuildAckReply(int stopId, bool didConfirm)
        {
            return appId.ToString() + " " + stopId.ToString() + " " + (didConfirm ? "Y" : "N");
        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {
            // Received message from network
            // There are a few possibilities for this message:
            //   If broadcast from a stop, decide if we're close enough to respond
            //     (i.e., the stop that sent it is on our route in the future),
            //     then respond with our route info and current location.
            //   If request from stop to change route
            //     Attempt to change route; reply either confirming or rejecting the change
            //   If it's a "start simulation" message from the RequestDriver,
            //     keep track of the start time to know current location
            //   If it's any other message, or we aren't close enough to the stop to
            //     reply, then ignore it.

            string msgString = Utilities.ByteArrayToString(msg);
            Debug.Print("Received message: " + msgString);
            if (IsStartSimulationMessage(msgString))
            {
                // TODO: Parse this from msgString, rather than setting it to "now"
                BusDriver.SimStart = DateTime.Now;
            }
            else if (IsBusStopRouteRequest(msgString))
            {
                // TODO: Get stopId from msgString
                int stopId = 0;
                if (BusDriver.getInstance().myBus.StopsUntilEncounter(stopId) >= 0)
                {
                    // A bus stop is asking for route information from "nearby" buses,
                    //   and we're one of them
                    // TODO: send reply
                    string replyMsg = BuildRouteReply(stopId);
                    BroadcastMessage(replyMsg);                    
                }
            }
            else if (IsRouteChangeConfirmationRequest(msgString))
            {
                // TODO: Get stopId from msgString
                int stopId = 0;

                // TODO: Figure out whether to confirm a route change
                bool confirm = true;
                string replyMsg = BuildAckReply(stopId, confirm);
                BroadcastMessage(replyMsg);
            }
        }

        private static bool IsRouteChangeConfirmationRequest(string msgString)
        {
            // TODO: Implement this
            return true;
        }

        private static bool IsStartSimulationMessage(string msg)
        {
            // TODO: Implement this
            return true;
        }

        private static bool IsBusStopRouteRequest(string msg)
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

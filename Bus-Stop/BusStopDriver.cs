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
    class BusStopDriver
    {
        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
        }

        private static BusStopDriver instance;

        // Requires an id, which is equivalent to appId, so do not create bus until after appId is initialized
        BusStop myBusStop;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        private static UInt16 appId;
        private static DateTime SimStart;
        private const int NearbyThreshold = 1;

        private BusStopDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        public static BusStopDriver getInstance()
        {
            if (BusStopDriver.instance == null)
            {
                BusStopDriver.instance = new BusStopDriver();
            }
            return BusStopDriver.instance;
        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {

            string msgString = Utilities.ByteArrayToString(msg);
            SBMessage message = new SBMessage(msgString);
            Debug.Print("Received message: " + msgString);
            SBMessage.MessageType msgType = message.header.type;
            BusStop stop = BusStopDriver.getInstance().myBusStop;

            switch (msgType)
            {
                case SBMessage.MessageType.START_SIMULATION:
                    {
                        BusStopDriver.SimStart = ((PayloadDateTime)message.payload).date;
                        Utilities.SimStart = BusStopDriver.SimStart;
                        instance.myBusStop.SimStart = BusStopDriver.SimStart;
                        break;
                    }
                case SBMessage.MessageType.SEND_PASSENGER_REQUEST:
                    {
                        Request new_request = (Request)message.payload;

                        //If the bus stop does not have any busInfo, it has to send ROUTE_INFO_REQUEST first
                        if (instance.myBusStop.busInfo_list == null)
                        {
                            SBMessage.MessageSource origin = new SBMessage.MessageSource(SBMessage.MessageSource.SourceType.BUS_STOP);
                            SBMessage.MessageSource destination = new SBMessage.MessageSource();
                            IMessagePayload payload = null;
                            SBMessage m = new SBMessage(SBMessage.MessageType.ROUTE_INFO_REQUEST, origin, destination, payload);
                            m.Broadcast(BusStopDriver.getInstance().NetPort);

                            instance.myBusStop.request_receive(new_request, true);
                        }
                        else
                        {
                            instance.myBusStop.request_receive(new_request, false);

                            //Send ROUTE_CHANGE_REQUEST to the matched bus
                        }
                       
                        break;
                    }
                case SBMessage.MessageType.ROUTE_INFO_RESPONSE:
                    {
                        //Save the bus info (with route) to busInfo_list
                        //instance.myBusStop.Append_busInfo();
                        

                        break;
                    }
                case SBMessage.MessageType.ROUTE_CHANGE_ACK:
                    {
                        //The request has been accept, delete the request from request_list
                        break;
                    }
            }
        }


        public static void Main()
        {
            BusStopDriver driver = BusStopDriver.getInstance();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            instance.myBusStop = new Smart_Bus.BusStop(appId);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}

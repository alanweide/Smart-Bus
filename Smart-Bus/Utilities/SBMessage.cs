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
    public class SBMessage
    {
        public struct MessageSource
        {
            public enum SourceType
            {
                BROADCAST = 0,
                PASSENGER = 1,
                BUS_STOP = 2,
                BUS = 3
            }
            public SourceType srcType;
            public int srcId;

            // Initializer defaults to PASSENGER with no id
            public MessageSource(string srcType = "0", string srcId = "-1")
            {
                this.srcType = (SourceType)int.Parse(srcType);
                this.srcId = int.Parse(srcId);
            }

            public MessageSource(SourceType srcType = SourceType.BROADCAST, int srcId = -1)
            {
                this.srcType = srcType;
                this.srcId = srcId;
            }

            public override string ToString()
            {
                return this.srcType.ToString() + " " + this.srcId.ToString();
            }
        }

        public struct MessageHeader
        {
            public const int Length = 5;

            public MessageType type;
            public MessageSource origin;
            public MessageSource destination;

            public MessageHeader(MessageType type, MessageSource origin, MessageSource destination)
            {
                this.type = type;
                this.origin = origin;
                this.destination = destination;
            }

            public override string ToString()
            {
                return type.ToString() + " " + origin.ToString() + " " + destination.ToString();
            }
        }

        public enum MessageType
        {
            START_SIMULATION = 10,
            SEND_PASSENGER_REQUEST = 11,

            ROUTE_INFO_REQUEST = 20,
            ROUTE_INFO_RELAY_REQUEST = 21,
            ROUTE_CHANGE_REQUEST = 22,
            REQUEST_SCHEDULED = 23,

            ROUTE_INFO_RESPONSE = 30,
            ROUTE_CHANGE_ACK = 31
        }

        public MessageHeader header;
        public IMessagePayload payload;

        public SBMessage()
        {
        }

        public SBMessage(MessageType type, MessageSource origin, MessageSource destination, IMessagePayload payload)
        {
            this.header = new MessageHeader(type, origin, destination);
            this.payload = payload;
        }

        public SBMessage(string msgString)
        {
            string[] components = msgString.Split();
            MessageType msgType = (MessageType)int.Parse(components[1]);
            MessageSource source = new MessageSource(components[1], components[2]);
            MessageSource destination = new MessageSource(components[3], components[4]);
            int headLength = MessageHeader.Length;
            switch (msgType)
            {
                    // Message from Passenger
                case MessageType.START_SIMULATION:
                    this.payload = new PayloadDateTime(components, ref headLength);
                    break;
                case MessageType.SEND_PASSENGER_REQUEST:
                    this.payload = new Request(components, ref headLength);
                    break;

                    // Message from Bus Stop
                case MessageType.ROUTE_INFO_REQUEST:
                    this.payload = new PayloadSimpleString();
                    break;
                case MessageType.ROUTE_INFO_RELAY_REQUEST:
                    this.payload = new PayloadRouteRequestForward(components, ref headLength);
                    break;
                case MessageType.ROUTE_CHANGE_REQUEST:
                    this.payload = new Route(components, ref headLength);
                    break;
                case MessageType.REQUEST_SCHEDULED:
                    this.payload = new Request(components, ref headLength);
                    break;

                    // Message from Bus
                case MessageType.ROUTE_INFO_RESPONSE:
                    this.payload = new Route(components, ref headLength);
                    break;
                case MessageType.ROUTE_CHANGE_ACK:
                    this.payload = new PayloadRouteChangeAckResponse(components, ref headLength);
                    break;
            }
        }

        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(this.header.ToString() + " ");
            msg.Append(this.payload.BuildPayload());
            return msg.ToString();
        }

        public void Broadcast(NetInst NetPort)
        {
            Debug.Print("Sending message " + this.ToString());
            byte[] msgBytes = Utilities.StringToByteArray(this.ToString());
            NetPort.Broadcast(msgBytes, msgBytes.Length);
        }
    }
}

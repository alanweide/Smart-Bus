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
                PASSENGER = 1,
                BUS_STOP = 2,
                BUS = 3
            }
            public SourceType srcType;
            public int srcId;

            // Initializer defaults to PASSENGER with no source
            public MessageSource(string src = "1", string id = "-1")
            {
                this.srcType = (SourceType)Int32.Parse(src);
                this.srcId = Int32.Parse(id);
            }

            public override string ToString()
            {
                StringBuilder str = new StringBuilder(this.srcType.ToString());
                if (this.srcType != SourceType.PASSENGER)
                {
                    str.Append(" " + this.srcId.ToString());
                }
                return str.ToString();
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

        public MessageSource source;
        public MessageType msgType;
        public IMessagePayload payload;
        private string msgString;

        public SBMessage()
        {
        }

        public SBMessage(MessageSource src, MessageType type, IMessagePayload payload)
        {
            this.source = src;
            this.msgType = type;
            this.payload = payload;
        }

        public SBMessage(string msgString)
        {
            this.msgString = msgString;
            string[] components = msgString.Split();
            this.msgType = (MessageType)Int32.Parse(components[1]);
            string msgSrc = components[1];
            if ((MessageSource.SourceType)Int32.Parse(msgSrc) == MessageSource.SourceType.PASSENGER)
            {
                this.source = new MessageSource();
            }
            else
            {
                this.source = new MessageSource(msgSrc, components[2]);
            }

            switch (this.msgType)
            {
                case MessageType.START_SIMULATION:
                    this.payload = new PayloadDateTime(Utilities.ParseDateTime(components[2]));
                    break;
                case MessageType.SEND_PASSENGER_REQUEST:
                    this.payload = new Request(components, 2);
                    break;

                case MessageType.ROUTE_INFO_REQUEST:
                    this.payload = new SimplePayloadString();
                    break;
                case MessageType.ROUTE_INFO_RELAY_REQUEST:
                    this.payload = new RouteRequestForwardPayload(int.Parse(components[3]), int.Parse(components[4]));
                    break;
                case MessageType.ROUTE_CHANGE_REQUEST:
                    this.payload = new Route(components, 3);
                    break;
                case MessageType.REQUEST_SCHEDULED:
                    this.payload = new Request(components, 3);
                    break;

                case MessageType.ROUTE_INFO_RESPONSE:
                    this.payload = new Route(components, 3);
                    break;
                case MessageType.ROUTE_CHANGE_ACK:
                    this.payload = new SimplePayloadString(components[4]);
                    break;
            }
        }

        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(this.msgType.ToString() + " ");
            msg.Append(this.source.ToString() + " ");
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

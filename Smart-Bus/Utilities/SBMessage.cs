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
        public struct MessageEndpoint
        {
            public enum EndpointType
            {
                BROADCAST = 0,
                PASSENGER = 1,
                BUS_STOP = 2,
                BUS = 3
            }
            public EndpointType endptType;
            public int endptId;

            // Initializer defaults to BROADCAST with no id
            public MessageEndpoint(string endptType = "0", string endptId = "-1")
            {
                this.endptType = (EndpointType)int.Parse(endptType);
                this.endptId = int.Parse(endptId);
            }

            public MessageEndpoint(EndpointType endptType = EndpointType.BROADCAST, int endptId = -1)
            {
                this.endptType = endptType;
                this.endptId = endptId;
            }

            public override string ToString()
            {
                return this.endptType.ToString() + " " + this.endptId.ToString();
            }
        }

        public struct MessageHeader
        {
            public const int Length = 5;

            public MessageType type;
            public MessageEndpoint origin;
            public MessageEndpoint destination;

            public MessageHeader(MessageType type, MessageEndpoint origin, MessageEndpoint destination)
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

        private MessageHeader _header;
        private IMessagePayload _payload;

        public MessageHeader header
        {
            get { return _header; }
            private set { }
        }
        public IMessagePayload payload
        {
            get { return _payload; }
            private set { }
        }

        public SBMessage()
        {
        }

        public SBMessage(MessageType type, MessageEndpoint origin, MessageEndpoint destination, IMessagePayload payload)
        {
            this._header = new MessageHeader(type, origin, destination);
            this._payload = payload;
        }

        public SBMessage(string msgString)
        {
            string[] components = msgString.Split();
            MessageType msgType = (MessageType)int.Parse(components[1]);
            MessageEndpoint source = new MessageEndpoint(components[1], components[2]);
            MessageEndpoint destination = new MessageEndpoint(components[3], components[4]);
            int headLength = MessageHeader.Length;
            switch (msgType)
            {
                    // Message from Passenger
                case MessageType.START_SIMULATION:
                    this._payload = new PayloadSimStart(components, ref headLength);
                    break;
                case MessageType.SEND_PASSENGER_REQUEST:
                    this._payload = new Request(components, ref headLength);
                    break;

                    // Message from Bus Stop
                case MessageType.ROUTE_INFO_REQUEST:
                    this._payload = new PayloadSimpleString();
                    break;
                case MessageType.ROUTE_INFO_RELAY_REQUEST:
                    this._payload = new PayloadRouteRequestForward(components, ref headLength);
                    break;
                case MessageType.ROUTE_CHANGE_REQUEST:
                    this._payload = new Route(components, ref headLength);
                    break;
                case MessageType.REQUEST_SCHEDULED:
                    this._payload = new Request(components, ref headLength);
                    break;

                    // Message from Bus
                case MessageType.ROUTE_INFO_RESPONSE:
                    this._payload = new Route(components, ref headLength);
                    break;
                case MessageType.ROUTE_CHANGE_ACK:
                    this._payload = new PayloadRouteChangeAckResponse(components, ref headLength);
                    break;
            }
        }

        public override string ToString()
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(this._header.ToString() + " ");
            msg.Append(this._payload.BuildPayload());
            return msg.ToString();
        }

        public void Broadcast(NetInst NetPort)
        {
            Debug.Print(DateTime.Now.ToString("HH:mm:ss.fff") + ": Sending message " + this.ToString());
            byte[] msgBytes = Utilities.StringToByteArray(this.ToString());
            NetPort.Broadcast(msgBytes, msgBytes.Length);
        }
    }
}

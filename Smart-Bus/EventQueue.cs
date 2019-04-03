using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class EventQueue<EventType>
    {

        public class EventNotice
        {

            public EventType thisEvent;
            public Rider rider;
            public double time;

            public EventNotice(EventType thisEvent, Rider rider, double time)
            {
                this.thisEvent = thisEvent;
                this.rider = rider;
                this.time = time;
            }

            public String toString()
            {
                return "At time " + this.time + " : event " + this.thisEvent + " for voter "
                        + (this.rider == null ? "null" : "who arrived at " + this.rider.timeEnteredSystem());
            }
        }

        // TODO: figure this one out in C#
        private Object rep;
        //private PriorityQueue<EventNotice> rep = new PriorityQueue<EventNotice>(50, new Comparator<EventNotice>() {
        //    public int compare(EventNotice e1, EventNotice e2) {
        //        //assert e1 != null && e2 != null && e1 != e2;
        //        return Double.compare(e1.time, e2.time);
        //    }
        //});

        public void addEvent(EventType thisEvent, Rider rider, double time)
        {
            this.rep.add(new EventNotice(thisEvent, rider, time));
        }

        public EventNotice nextEvent()
        {
            return this.rep.remove();
        }

        public int numberOfEvents()
        {
            return this.rep.size();
        }

    }
}

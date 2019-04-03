using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    class Rider
    {
        /*--- Private -----------------------------------------------------------------------*/

        private enum State
        {
            PRE, AT_STOP, RIDING, POST
        }

        // Note: numStates must match the number of values in the State enum 
        private const int numStates = 4;

        private State currentState;
        private double timeOfLastStateChange;
        private double[] timeEnteredState = new double[numStates];
        private double[] durationOfState = new double[numStates];

        private void updateTimesForCurrentState(double currentTime)
        {
            this.timeEnteredState[(int)this.currentState] = this.timeOfLastStateChange;
            this.durationOfState[(int)this.currentState] += (currentTime - this.timeOfLastStateChange);
            this.timeOfLastStateChange = currentTime;
        }

        /*--- Public -----------------------------------------------------------------------*/

        //public enum QueueName
        //{
        //    FASTVOTE_VQ, FASTVOTE_PQ, STANDBY_Q, VOTING_Q, SCANNING_Q
        //}
        //public const int numQueues = 5;

        //public enum ServiceName
        //{
        //    CHECKIN_S, VOTING_S, SCANNING_S
        //}
        //public const int numServices = 3;

        public double earliestServingTime { get; private set; }
        public double latestServingTime { get; private set; }

        public BusStop origin { get; private set; }
        public BusStop destination { get; private set; }

        public Rider(double currentTime, double earliestServingTime, double latestServingTime)
        {
            this.earliestServingTime = earliestServingTime;
            this.latestServingTime = latestServingTime;
            this.currentState = State.PRE;
            this.timeOfLastStateChange = currentTime;
            for (int i = 0; i < this.timeEnteredState.Length; i++)
            {
                this.timeEnteredState[i] = 0.0;
            }
            for (int i = 0; i < this.durationOfState.Length; i++)
            {
                this.durationOfState[i] = 0.0;
            }
        }

        public void arriveAtStop(double currentTime, double earliestServingTime, double latestServingTime, BusStop origin, BusStop destination)
        {
            updateTimesForCurrentState(currentTime);
            this.currentState = State.AT_STOP;
            this.origin = origin;
            this.destination = destination;
        }

        public void boardBus(double currentTime)
        {
            updateTimesForCurrentState(currentTime);
            this.currentState = State.RIDING;
        }

        public void leaveSystem(double currentTime)
        {
            updateTimesForCurrentState(currentTime);
            this.timeEnteredState[(int)State.POST] = currentTime;
            this.currentState = State.POST;
        }

        public double timeEnteredSystem()
        {
            return this.timeEnteredState[(int)State.PRE];
        }

        public double timeRequestSent()
        {
            return this.timeEnteredState[(int)State.AT_STOP];
        }

        public double timeBoardedBus()
        {
            return this.timeEnteredState[(int)State.RIDING];
        }

        public double timeLeftSystem()
        {
            return this.timeEnteredState[(int)State.POST];
        }

        //public static String csvHeader() {
        //    String header = "isFastVoteUser";

        //    // Times of entry and durations of all states except POST

        //    for (int i = 0; i < State.values().length - 1; i++) {
        //        header += ", t-" + State.values()[i];
        //        header += ", d-" + State.values()[i];
        //    }

        //    // Time of entry of state POST; no duration for this state

        //    header += ", t-" + State.values()[State.values().length - 1];

        //    // Encountered lengths of physical queues

        //    for (int i = 0; i < QueueName.values().length; i++) {
        //        header += ", len-" + QueueName.values()[i];
        //    }

        //    return header;
        //}

        //public override String toString() {
        //    String result = Boolean.toString(this.isFastVoteUser);

        //    // Times of entry and durations of all states except POST

        //    for (int i = 0; i < this.timeEnteredState.length - 1; i++) {
        //        result += ", " + this.timeEnteredState[i];
        //        result += ", " + this.durationOfState[i];
        //    }

        //    // Time of entry of state POST; no duration for this state

        //    result += ", " + this.timeEnteredState[State.values().length - 1];

        //    // Encountered lengths of physical queues

        //    for (int i = 0; i < this.encounteredLengthOfQueue.length; i++) {
        //        result += ", " + this.encounteredLengthOfQueue[i];
        //    }

        //    return result;
        //}
    }
}

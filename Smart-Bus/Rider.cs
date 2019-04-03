using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    class Rider
    {
        /*--- Private -----------------------------------------------------------------------*/

        private enum State
        {
            PRE, FASTVOTE_VQ, FASTVOTE_ELIGIBLE, FASTVOTE_PQ, STANDBY_Q, CHECKIN_S, VOTING_Q, VOTING_S, SCANNING_Q, SCANNING_S, POST
        }

        // Note: numStates must match the number of values in the State enum 
        // (these haven't been updated to reflect the Smart bus usage, they still reflect the voting system simulator.)
        private const int numStates = 11;

        public bool isFastVoteUser {get; private set;}
        public double startOfWindow { get; private set; } // meaningful only for FastVote user
        public double endOfWindow { get; private set; } // meaningful only for FastVote user

        public double arrivalTime { get; private set; }

        private State currentState;
        private double timeOfLastStateChange;
        private double[] timeEnteredState = new double[numStates];
        private double[] durationOfState = new double[numStates];
        private int[] encounteredLengthOfQueue = new int[numQueues];

        private void updateTimesForCurrentState(double currentTime)
        {
            this.timeEnteredState[(int)this.currentState] = this.timeOfLastStateChange;
            this.durationOfState[(int)this.currentState] += (currentTime - this.timeOfLastStateChange);
            this.timeOfLastStateChange = currentTime;
        }

        /*--- Public -----------------------------------------------------------------------*/

        public enum QueueName
        {
            FASTVOTE_VQ, FASTVOTE_PQ, STANDBY_Q, VOTING_Q, SCANNING_Q
        }
        public const int numQueues = 5;

        public enum ServiceName
        {
            CHECKIN_S, VOTING_S, SCANNING_S
        }
        public const int numServices = 3;

        public Rider(double currentTime, double arrivalTime)
        {
            this.startOfWindow = 0.0; // only used if isFastVoteUser
            this.endOfWindow = 0.0; // only used if isFastVoteUser
            this.arrivalTime = arrivalTime;
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
            for (int i = 0; i < this.encounteredLengthOfQueue.Length; i++)
            {
                this.encounteredLengthOfQueue[i] = 0;
            }
        }

        public void leaveSystem(double currentTime)
        {
            //assert this.currentState == State.VOTING_S || this.currentState == State.SCANNING_S;
            updateTimesForCurrentState(currentTime);
            this.timeEnteredState[(int)State.POST] = currentTime;
            this.currentState = State.POST;
        }

        public double timeEnteredSystem()
        {
            return this.timeEnteredState[(int)State.PRE];
        }

        //public boolean isFastVoteUser() {
        //    return this.isFastVoteUser;
        //}

        //public double startOfWindow() {
        //    //assert this.isFastVoteUser;
        //    return this.startOfWindow;
        //}

        //public double endOfWindow() {
        //    assert this.isFastVoteUser;
        //    return this.endOfWindow;
        //}

        //public double desiredVotingTime() {
        //    return this.desiredVotingTime;
        //}

        public double timeEnteredFastVoteVQ()
        {

            // OK to call if not FastVote user; result is then 0.0

            return this.timeEnteredState[(int)State.FASTVOTE_VQ];
        }

        public double timeEnteredFastVotePQ()
        {

            // OK to call if not FastVote user; result is then 0.0

            return this.timeEnteredState[(int)State.FASTVOTE_PQ];
        }

        public double timeEnteredStandbyQ()
        {
            return this.timeEnteredState[(int)State.STANDBY_Q];
        }

        public double timeEnteredVotingQ()
        {
            return this.timeEnteredState[(int)State.VOTING_Q];
        }

        public double waitTimeInFastVoteVQ()
        {

            // OK to call if not FastVote user; result is then 0.0

            return this.durationOfState[(int)State.FASTVOTE_VQ];
        }

        public double waitTimeInEligibleSet()
        {

            // OK to call if not FastVote user; result is then 0.0

            return this.durationOfState[(int)State.FASTVOTE_ELIGIBLE];
        }

        public double waitTimeInFastVotePQ()
        {

            // OK to call if not FastVote user; result is then 0.0

            return this.durationOfState[(int)State.FASTVOTE_PQ];
        }

        public double waitTimeInStandbyQ()
        {
            return this.durationOfState[(int)State.STANDBY_Q];
        }

        public double waitTimeInVotingQ()
        {
            return this.durationOfState[(int)State.VOTING_Q];
        }

        public int lengthOfFastVoteVQ()
        {

            // OK to call if not FastVote user; result is then 0

            return this.encounteredLengthOfQueue[(int)QueueName.FASTVOTE_VQ];
        }

        public int lengthOfFastVotePQ()
        {

            // OK to call if not FastVote user; result is then 0

            return this.encounteredLengthOfQueue[(int)QueueName.FASTVOTE_PQ];
        }

        public int lengthOfStandbyQ()
        {
            return this.encounteredLengthOfQueue[(int)QueueName.STANDBY_Q];
        }

        public int lengthOfVotingQ()
        {
            return this.encounteredLengthOfQueue[(int)QueueName.VOTING_Q];
        }

        public int lengthOfScanningQ()
        {
            return this.encounteredLengthOfQueue[(int)QueueName.SCANNING_Q];
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

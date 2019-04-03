using System;
using System.Collections;
using Microsoft.SPOT;

namespace Smart_Bus
{
public class PollingPlaceSimulator {

    // PRNG

    private static Random random = new Random();

    // Simulation parameters

    private static int numberOfRiders = 2;
    private static int numberOfStops = 2;
    private static int numberOfBuses = 2;

	// Simulation events

	private enum EventType {
        //CHECK_FASTVOTE_WINDOW, START_FASTVOTE_VQ_WAITING, START_FASTVOTE_ELIGIBLE, START_FASTVOTE_PQ_WAITING, START_FASTVOTE_CHECKIN_SERVICE, START_STANDBY_WAITING, START_STANDBY_CHECKIN_SERVICE, START_VOTING_WAITING, START_VOTING_SERVICE, LEAVE
        SEND_REQUEST, BOARD_BUS, LEAVE_BUS
	}

	// Simulation data

	private const List<Queue<Rider>> votersThroughSystem = new ArrayList<Queue<Rider>();

	// Simulation method

	private void runOneSimulation(int simId) {

		// Initialize simulation objects

        //FastVoteFacility fastVoteQ = new FastVoteFacility(sp.eligibleSetSize(), sp.initialWindowDuration,
        //        sp.finalWindowDuration, sp.closingBuffer, sp.averageCheckInServiceTime,
        //        sp.numberOfFastVoteCheckInStations, sp.pollClosingTime);
        //ServiceFacility fastVoteS = new ServiceFacility(sp.numberOfFastVoteCheckInStations,
        //        Voter.ServiceName.CHECKIN_S);
        //QueueFacility standbyQ = new QueueFacility(0, Voter.QueueName.STANDBY_Q);
        //ServiceFacility standbyS = new ServiceFacility(sp.numberOfCheckInStations - sp.numberOfFastVoteCheckInStations,
        //        Voter.ServiceName.CHECKIN_S);
        //QueueFacility votingQ = new QueueFacility(sp.capacityOfVotingQueue, Voter.QueueName.VOTING_Q);
        //ServiceFacility votingS = new ServiceFacility(sp.numberOfVotingStations, Voter.ServiceName.VOTING_S);

		// Initialize simulation time distribution controllers

		RequestGenerator requests = new RequestGenerator(sp.arrivalPattern, sp.pollClosingTime);
        //ServiceGenerator checkin = new ServiceGenerator(sp.averageCheckInServiceTime, sp.checkinShape);
        //ServiceGenerator voting = new ServiceGenerator(sp.averageVotingServiceTime, sp.votingShape);

		/*
		 * Initialize simulation state (scheduled events)
		 */

		EventQueue<EventType> eventQueue = new EventQueue<EventType>();

		/*
		 * Generate voters, each with a desired voting time; some voters are
		 * FastVote users; some voters arrive early so desired voting time is
		 * just after polls open
		 */

		List<Rider> riders = new ArrayList<Rider>();

		for (int i = 0; i < sp.numberOfVoters(); i++) {
			double arrivalTime = requests.generatedRequestTime(0.0);
            //assert 0 <= desiredVotingTime && desiredVotingTime < sp.pollClosingTime;

			riders.add(new Rider(0.0, arrivalTime));
		}

		/*
		 * Add arrival event for each rider
		 */

		foreach (Rider rider in riders) {
			
				eventQueue.addEvent(EventType.SEND_REQUEST, rider, rider.arrivalTime);
		}

		// Simulator loop

		while (eventQueue.numberOfEvents() > 0) {

			/*
			 * Get next scheduled event, which appear in non-decreasing order of
			 * simulated time
			 */

			EventQueue<EventType>.EventNotice thisEvent = eventQueue.nextEvent();

			// Take appropriate action for this event
            // TODO: Update these events

			switch (thisEvent.event) {
			case CHECK_FASTVOTE_WINDOW: {
				assert sp.fastVotePopularity > 0.0;
				assert thisEvent.time < sp.pollClosingTime : "Checking FastVote window past closing time";

				/*
				 * Check FastVote app to determine whether offered window
				 * overlaps desired voting time, and act accordingly
				 */

				if (fastVoteQ.isClosed(thisEvent.time)) {

					/*
					 * Finding a closed FastVote queue, go to polling location
					 * at desired voting time
					 */

					thisEvent.voter.setFastVoteUser(false);
					eventQueue.addEvent(EventType.START_STANDBY_WAITING, thisEvent.voter,
							thisEvent.voter.desiredVotingTime());
				} else {

					/*
					 * FastVote queue is open; adjust desired voting time so it
					 * is not before current time (because voter might have
					 * checked FastVote previously when it was before desired
					 * voting time, but now it's past desired voting time)
					 */

					double desiredVotingTime = Math.max(thisEvent.time, thisEvent.voter.desiredVotingTime());
					double startOfOfferedWindow = fastVoteQ.startOfOfferedWindow(thisEvent.time);
					double endOfOfferedWindow = fastVoteQ.endOfOfferedWindow(thisEvent.time);

					if (desiredVotingTime < startOfOfferedWindow) {

						/*
						 * Voter's start of offered window is later than desired
						 * voting time; accept this window anyway with some
						 * probability, or reject this window and instead go to
						 * polling location at desired voting time
						 */

						if (Math.random() < sp.windowTooLateAcceptanceProbability) {

							// System.out.println(" window too late --
							// accepted");

							eventQueue.addEvent(EventType.START_FASTVOTE_VQ_WAITING, thisEvent.voter, thisEvent.time);
						} else {

							// System.out.println(" window too late -- rejected
							// "
							// + (startOfOfferedWindow - desiredVotingTime));

							thisEvent.voter.setFastVoteUser(false);
							eventQueue.addEvent(EventType.START_STANDBY_WAITING, thisEvent.voter, desiredVotingTime);
						}
					} else if (desiredVotingTime < endOfOfferedWindow) {

						/*
						 * Voter's desired voting time is within window; accept
						 * this window
						 */

						// System.out.println(" bracketed");

						eventQueue.addEvent(EventType.START_FASTVOTE_VQ_WAITING, thisEvent.voter, thisEvent.time);
					} else {

						/*
						 * Voter's end of offered window is before desired
						 * voting time; accept this window anyway with some
						 * probability, or reject this window now and come back
						 * to app to check again in the future (exponentially
						 * distributed inter-checking time could be changed if
						 * we had any data showing it is unrealistic)
						 */

						double nextWindowCheckTime = thisEvent.time
								+ Utilities.randomExponential(sp.averageTimeBetweenWindowChecks);
						if (nextWindowCheckTime >= sp.pollClosingTime
								|| Math.random() < sp.windowTooEarlyAcceptanceProbability) {

							// System.out.println("window too early --
							// accepted");

							eventQueue.addEvent(EventType.START_FASTVOTE_VQ_WAITING, thisEvent.voter, thisEvent.time);
						} else {

							// System.out.println("window too early -- rejected
							// "
							// + (desiredVotingTime - endOfOfferedWindow));

							eventQueue.addEvent(EventType.CHECK_FASTVOTE_WINDOW, thisEvent.voter, nextWindowCheckTime);
						}
					}
				}
			}
				break;
			case START_FASTVOTE_VQ_WAITING: {
				assert sp.fastVotePopularity > 0.0;

				// Put voter into FastVote queue

				boolean skipVQ = fastVoteQ.addToVirtualQueue(thisEvent.time, thisEvent.voter);
				if (skipVQ) {

					/*
					 * Make this voter eligible immediately; must be first in
					 * virtual queue because any other voter ahead of this one
					 * in virtual queue should already have become eligible
					 */

					Voter voter = fastVoteQ.makeEligible(thisEvent.time);
					assert voter == thisEvent.voter : "First voter in virtual queue not chosen to be eligible";

					/*
					 * Schedule this voter to arrive at the polling place to get
					 * in FastVote physical queue based on window duration;
					 * uniform distribution may be changed later
					 */

					double windowDuration = voter.endOfWindow() - thisEvent.time;
					eventQueue.addEvent(EventType.START_FASTVOTE_PQ_WAITING, voter,
							Utilities.randomUniform(thisEvent.time, windowDuration));
				} else {

					/*
					 * Do nothing now; voter will become eligible later as
					 * others from FastVote physical queue enter check-in
					 * service
					 */

				}
			}
				break;
			case START_FASTVOTE_ELIGIBLE: {
				assert sp.fastVotePopularity > 0.0;

				// Make next voter in virtual queue eligible, if there is one

				Voter voter = fastVoteQ.makeEligible(thisEvent.time);
				if (voter != null) {

					/*
					 * Schedule this voter to arrive at the polling place to get
					 * in FastVote physical queue based on window duration;
					 * uniform distribution may be changed later
					 */

					double windowDuration = voter.endOfWindow() - thisEvent.time;
					eventQueue.addEvent(EventType.START_FASTVOTE_PQ_WAITING, voter,
							Utilities.randomUniform(thisEvent.time, windowDuration));
				} else {

					/*
					 * Do nothing; it's possible there is no voter is FastVote
					 * virtual queue
					 */

				}
			}
				break;
			case START_FASTVOTE_PQ_WAITING: {
				assert sp.fastVotePopularity > 0.0;
				assert thisEvent.time < sp.pollClosingTime : "Voter trying to enter FastVote physical queue after closing";

				// Add voter to FastVote physical queue

				fastVoteQ.moveToPhysicalQueue(thisEvent.time, thisEvent.voter);

				/*
				 * Schedule check-in services to look for more work immediately,
				 * just in case this is the first voter in the queue and the
				 * check-in service is idle; no voter in event notice because
				 * voter starting service will be taken from queue
				 */

				eventQueue.addEvent(EventType.START_FASTVOTE_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_STANDBY_CHECKIN_SERVICE, null, thisEvent.time);
			}
				break;
			case START_FASTVOTE_CHECKIN_SERVICE: {

				/*
				 * Policy: some check-in stations give FastVote physical queue
				 * preference and the others give standby queue preference, but
				 * if any check-in station is available and its preferred queue
				 * is empty, it serves the other queue
				 */

				if ((fastVoteS.numberInService() < fastVoteS.numberOfStations())
						&& votingQ.positionIsAvailable(standbyS.numberInService() + fastVoteS.numberInService())) {

					/*
					 * FastVote check-in service has an opening, and there's
					 * room in the voting queue for at least as many voters who
					 * might leave check-in stations; get voter out of FastVote
					 * check-in queue (or out of standby queue, if FastVote
					 * queue is empty) and into FastVote check-in service
					 */

					if (fastVoteQ.numberInPhysicalQueue() > 0) {
						assert sp.fastVotePopularity > 0.0;

						// FastVote preference: FastVote physical queue first

						Voter voter = fastVoteQ.remove(thisEvent.time);
						fastVoteS.add(thisEvent.time, voter);

						// Schedule immediately making another voter eligible

						eventQueue.addEvent(EventType.START_FASTVOTE_ELIGIBLE, null, thisEvent.time);

						/*
						 * Schedule departure of this voter from FastVote
						 * check-in service
						 */

						double nextTime = checkin.nextServiceTime(thisEvent.time);
						eventQueue.addEvent(EventType.START_VOTING_WAITING, voter, nextTime);
					} else if (standbyQ.numberInQueue() > 0) {

						// Try standby queue next

						Voter voter = standbyQ.remove();
						fastVoteS.add(thisEvent.time, voter);

						/*
						 * Schedule departure of this voter from FastVote
						 * check-in service
						 */

						double nextTime = checkin.nextServiceTime(thisEvent.time);
						eventQueue.addEvent(EventType.START_VOTING_WAITING, voter, nextTime);
					}
				}
			}
				break;
			case START_STANDBY_WAITING: {
				assert thisEvent.time < sp.pollClosingTime : "Voter trying to enter standby queue after closing";

				// Put voter into check-in queue

				standbyQ.add(thisEvent.time, thisEvent.voter);

				/*
				 * Schedule check-in services to look for more work immediately,
				 * just in case this is the first voter in the queue and the
				 * check-in service is idle; no voter in event notice because
				 * voter starting service will be taken from queue
				 */

				eventQueue.addEvent(EventType.START_STANDBY_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_FASTVOTE_CHECKIN_SERVICE, null, thisEvent.time);
			}
				break;
			case START_STANDBY_CHECKIN_SERVICE: {
				if ((standbyS.numberInService() < standbyS.numberOfStations())
						&& votingQ.positionIsAvailable(standbyS.numberInService() + fastVoteS.numberInService())) {

					/*
					 * Standby check-in service has an opening, and there's room
					 * in the voting queue for at least as many voters who might
					 * leave check-in stations; get voter out of FastVote
					 * check-in queue only if it's "too long", otherwise out of
					 * standby queue
					 */

					if (fastVoteQ.numberInPhysicalQueue() > sp.criticalLengthOfFastVotePQ()) {
						assert sp.fastVotePopularity > 0.0;

						/*
						 * Always check FastVote physical queue first, and take
						 * someone from it if line is "too long"
						 */

						Voter voter = fastVoteQ.remove(thisEvent.time);
						standbyS.add(thisEvent.time, voter);

						// Schedule immediately making another voter eligible

						eventQueue.addEvent(EventType.START_FASTVOTE_ELIGIBLE, null, thisEvent.time);

						/*
						 * Schedule departure of this voter from standby
						 * check-in service
						 */

						double nextTime = checkin.nextServiceTime(thisEvent.time);
						eventQueue.addEvent(EventType.START_VOTING_WAITING, voter, nextTime);

					} else if (standbyQ.numberInQueue() > 0) {

						// Standby preference second

						Voter voter = standbyQ.remove();
						standbyS.add(thisEvent.time, voter);

						/*
						 * Schedule departure of this voter from standby
						 * check-in service
						 */

						double nextTime = checkin.nextServiceTime(thisEvent.time);
						eventQueue.addEvent(EventType.START_VOTING_WAITING, voter, nextTime);
					}
				}
			}
				break;
			case START_VOTING_WAITING: {

				// Get voter out of check-in service and into voting queue

				if (fastVoteS.contains(thisEvent.voter)) {
					fastVoteS.remove(thisEvent.voter);
				} else {
					standbyS.remove(thisEvent.voter);
				}
				votingQ.add(thisEvent.time, thisEvent.voter);

				/*
				 * Schedule both check-in services and voting service to look
				 * for more work immediately, just in case this is the first
				 * voter in the voting queue and the voting service is idle; no
				 * voter in event notice because voter starting service will be
				 * taken from queue
				 */

				eventQueue.addEvent(EventType.START_FASTVOTE_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_STANDBY_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_VOTING_SERVICE, null, thisEvent.time);
			}
				break;
			case START_VOTING_SERVICE: {
				if ((votingS.numberInService() < votingS.numberOfStations()) && votingQ.numberInQueue() > 0) {

					/*
					 * Voting service has opening and there's work to do; get
					 * voter out of voting queue and into voting service
					 */

					Voter voter = votingQ.remove();
					votingS.add(thisEvent.time, voter);

					// Schedule departure of this voter from voting service

					double nextTime = voting.nextServiceTime(thisEvent.time);
					eventQueue.addEvent(EventType.LEAVE, voter, nextTime);
				}
			}
				break;
			case LEAVE: {

				/*
				 * Get voter out of voting service and into list of voters who
				 * have left the system (from which we can record data about
				 * each voter's important times)
				 */

				votingS.remove(thisEvent.voter);
				thisEvent.voter.leaveSystem(thisEvent.time);
				votersThroughSystem.get(simId).add(thisEvent.voter);

				/*
				 * Schedule both check-in services and voting service to look
				 * for more work immediately; no voter in event notice because
				 * voter starting service will be taken from queue
				 */

				eventQueue.addEvent(EventType.START_FASTVOTE_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_STANDBY_CHECKIN_SERVICE, null, thisEvent.time);
				eventQueue.addEvent(EventType.START_VOTING_SERVICE, null, thisEvent.time);
			}
				break;
			default:
				break;
			}
		}
	}

	@GET
	@Path("run")
	@Produces("application/json")
	public static Response simulate(@Context UriInfo info) {

		SimulationParameters sp = new SimulationParameters();

		PollingPlaceSimulator sim = new PollingPlaceSimulator();

		MultivaluedMap<String, String> params = info.getQueryParameters();

		/*
		 * Change simulation parameters that are different from defaults
		 */

		if (params.getFirst("numberOfRuns") != null) {
			// sp.numberOfRuns =
			// Math.min(Integer.parseInt(params.getFirst("numberOfRuns")), 50);
			sp.numberOfRuns = Integer.parseInt(params.getFirst("numberOfRuns"));
		}
		if (params.getFirst("pollClosingTime") != null) {
			sp.pollClosingTime = Double.parseDouble(params.getFirst("pollClosingTime"));
		}
		if (params.getFirst("numberOfRegisteredVoters") != null) {
			sp.numberOfRegisteredVoters = Integer.parseInt(params.getFirst("numberOfRegisteredVoters"));
		}
		if (params.getFirst("turnout") != null) {
			sp.turnout = Double.parseDouble(params.getFirst("turnout"));
		}
		if (params.getFirst("numberOfCheckInStations") != null) {
			sp.numberOfCheckInStations = Integer.parseInt(params.getFirst("numberOfCheckInStations"));
		}
		if (params.getFirst("averageCheckInServiceTime") != null) {
			sp.averageCheckInServiceTime = Double.parseDouble(params.getFirst("averageCheckInServiceTime"));
		}
		if (params.getFirst("capacityOfVotingQueue") != null) {
			sp.capacityOfVotingQueue = Integer.parseInt(params.getFirst("capacityOfVotingQueue"));
		}
		if (params.getFirst("numberOfVotingStations") != null) {
			sp.numberOfVotingStations = Integer.parseInt(params.getFirst("numberOfVotingStations"));
		}
		if (params.getFirst("averageVotingServiceTime") != null) {
			sp.averageVotingServiceTime = Double.parseDouble(params.getFirst("averageVotingServiceTime"));
		}

		if (params.getFirst("fastVotePopularity") != null) {
			sp.fastVotePopularity = Double.parseDouble(params.getFirst("fastVotePopularity"));
		}
		if (params.getFirst("numberOfFastVoteCheckInStations") != null) {
			sp.numberOfFastVoteCheckInStations = Integer.parseInt(params.getFirst("numberOfFastVoteCheckInStations"));
		}
		if (params.getFirst("initialWindowDuration") != null) {
			sp.initialWindowDuration = Double.parseDouble(params.getFirst("initialWindowDuration"));
		}
		if (params.getFirst("finalWindowDuration") != null) {
			sp.finalWindowDuration = Double.parseDouble(params.getFirst("finalWindowDuration"));
		}
		if (params.getFirst("closingBuffer") != null) {
			sp.closingBuffer = Double.parseDouble(params.getFirst("closingBuffer"));
		}
		if (params.getFirst("criticalWaitingTimeForFastVotePQ") != null) {
			sp.criticalWaitingTimeForFastVotePQ = Double
					.parseDouble(params.getFirst("criticalWaitingTimeForFastVotePQ"));
		}
		if (params.getFirst("eligibleSetSizeAsThrougphutFraction") != null) {
			sp.eligibleSetSizeAsThroughputFraction = Double
					.parseDouble(params.getFirst("eligibleSetSizeAsThroughputFraction"));
		}

		if (params.getFirst("averageWindowCheckLeadTime") != null) {
			sp.averageWindowCheckLeadTime = Double.parseDouble(params.getFirst("averageWindowCheckLeadTime"));
		}
		if (params.getFirst("windowTooEarlyAcceptanceProbability") != null) {
			sp.windowTooEarlyAcceptanceProbability = Double
					.parseDouble(params.getFirst("windowTooEarlyAcceptanceProbability"));
		}
		if (params.getFirst("windowTooLateAcceptanceProbability") != null) {
			sp.windowTooLateAcceptanceProbability = Double
					.parseDouble(params.getFirst("windowTooLateAcceptanceProbability"));
		}
		if (params.getFirst("averageTimeBetweenWindowChecks") != null) {
			sp.averageTimeBetweenWindowChecks = Double.parseDouble(params.getFirst("averageTimeBetweenWindowChecks"));
		}

		/*
		 * Make sure parameters are valid; only non-trivial properties are
		 * checked
		 */

		assert sp.capacityOfVotingQueue == 0
				|| sp.capacityOfVotingQueue > sp.numberOfCheckInStations : "Invalid voting queue capacity";

		/*
		 * Create a list of voters through system for each run; useful to
		 * separate into different lists if threads are employed
		 */

		for (int i = 0; i < sp.numberOfRuns; i++) {
			sim.votersThroughSystem.add(new LinkedList<Voter>());
		}

		for (int i = 0; i < sp.numberOfRuns; i++) {
			sim.runOneSimulation(sp, i);
		}

		sim.summarizeData(sp);
		JSONArray report = new JSONArray();
		try {
			report = sim.reportSummaryJSON(sp);
		} catch (JSONException e) {
			report.put("ERROR: JSONException");
		}
		return Response.status(200).entity(report.toString()).build();
	}

	public static void main(String[] args) {

		SimulationParameters sp = new SimulationParameters();

		PollingPlaceSimulator sim = new PollingPlaceSimulator();

		/*
		 * Change simulation parameters that are different from defaults
		 */

		// sp.numberOfRuns = 1;
		// sp.numberOfCheckInStations = 4;
		// sp.numberOfVotingStations = 11;
		// sp.arrivalPattern = ArrivalPattern.COMPOSITE_LOWER_LATE;
		sp.arrivalPattern = ArrivalPattern.MID_MORNING_PEAK_1;
		sp.fastVotePopularity = 0.40;
		sp.numberOfFastVoteCheckInStations = 1;

		/*
		 * Make sure parameters are valid; only non-trivial properties are
		 * checked
		 */

		assert sp.capacityOfVotingQueue == 0
				|| sp.capacityOfVotingQueue > sp.numberOfCheckInStations : "Invalid voting queue capacity";

		/*
		 * Create a list of voters through system for each run; useful to
		 * separate into different lists if threads are employed
		 */

		for (int i = 0; i < sp.numberOfRuns; i++) {
			sim.votersThroughSystem.add(new LinkedList<Voter>());
		}

		// Run multiple simulations in parallel (?)

		long startTime = System.currentTimeMillis();

		for (int i = 0; i < sp.numberOfRuns; i++) {
			// final int simId = i;
			// threads.execute(new Runnable() {
			// @Override
			// public void run() {
			// runOneSimulation(simId);
			// }
			// });
			sim.runOneSimulation(sp, i);
		}

		// threads.shutdown();
		// try {
		// boolean didTerminate = threads.awaitTermination(30,
		// TimeUnit.SECONDS);
		// if (!didTerminate) {
		// System.out.println("Thread Pool timed out!");
		// }
		// } catch (InterruptedException e) {
		// e.printStackTrace();
		// }

		System.out.println(Integer.toString(sp.numberOfRuns) + " simulations (excluding reporting) took "
				+ (System.currentTimeMillis() - startTime) + " mSec");

		// Report data

		// reportIndividualVoterData();
		sim.summarizeData(sp);
		sim.reportSummaryData(sp);
		// try {
		// System.out.println(reportSummaryJSON(sp));
		// } catch (JSONException e) {
		// e.printStackTrace();
		// }

		System.out.println(Integer.toString(sp.numberOfRuns) + " simulations (including reporting) took "
				+ (System.currentTimeMillis() - startTime) + " mSec");
		// with "
		// + ((ThreadPoolExecutor) threads).getMaximumPoolSize() + " threads");
	}

}
}

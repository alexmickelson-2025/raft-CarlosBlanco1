using myclasslib;
using System.Timers;
using NSubstitute;

namespace myxunit;

public class UnitTest1
{
    // C Test #3
    [Fact]
    public void WhenNewNodeIsInitializedItShouldBeInFollowerState()
    {
        ServerNode sn1 = new ServerNode([]);

        Assert.True(sn1.State == ServerState.Follower);
    }

    // C Test #17
    [Fact]
    public void WhenFollowerReceivesAppendEntryRPCItSendsResponseBack()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 1;

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received().ResponseAppendEntriesRPC(follower.NodeId, Arg.Any<bool>());
    }

    // C Test #18
    [Fact]
    public void IfLeaderWithPreviousTermSendsEntryItGetsRejected()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 2);
        leader.CurrentTerm = 1;

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received().ResponseAppendEntriesRPC(follower.NodeId, isResponseRejecting:true);
    }

    // C Test #7
    [Fact]
    public void WhenFollowerGetsValidAppendEntriesRPCElectionTimerResets()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 2;

        Thread.Sleep(300);

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received().ResponseAppendEntriesRPC(follower.NodeId, isResponseRejecting:false);
    }

    // C Test #4 
    [Fact]
    public void WhenFollowerDoesntGetMessageFor300msItStartsElection()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        int leaderTerm = 2;
        var futureLeader = new ServerNode([follower1, follower2, follower3], startTerm: leaderTerm, electionTimeout:150);

        Thread.Sleep(300);
        follower1.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);
        follower2.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);
        follower3.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);

    }

    // C Test #5 
    [Fact]
    public void  NodesAreCreatedWithAtLeastOneUniqueRandomIntervalForElectionTimers()
    {
        var serverNodes = new List<ServerNode>();
        HashSet<double> randomTimerValues = new HashSet<double>();

        for (int i = 0; i < 5; i++)
        {
            serverNodes.Add(new ServerNode([]));
        }

        foreach (var serverNode in serverNodes)
        {
            double randomIntervalForNode = serverNode.ElectionTimer.Interval;

            Assert.InRange(randomIntervalForNode, 150, 300);

            randomTimerValues.Add(randomIntervalForNode);
        }

        Assert.True(randomTimerValues.Count > 1);
    }


    // C Test #6
    [Fact]
    public void WhenANewElectionBeginsTermIsIncrementedByOne()
    {
        int initialTerm = 1;
        IServerNode sn1 = Substitute.For<IServerNode>();
        ServerNode sn2 = new([sn1], startTerm:initialTerm);

        Thread.Sleep(300);

        Assert.True(sn2.CurrentTerm >= initialTerm + 1);
    }

    // C Test #2
    [Fact]
    public void WhenANodeReceivesAValidAppendEntriesFromAnotherNodeItRemembersItsId()
    {
        int followerTerm = 1;
        int leaderTerm = 2;

        var leader = Substitute.For<IServerNode>();
        leader.CurrentTerm = leaderTerm;
        var follower = new ServerNode([leader], startTerm: followerTerm);

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        Assert.True(follower.LeaderNodeId == leader.NodeId);
    }

    // C Test #1
    [Fact]
    public void ActiveLeaderSendsHeartbeatWithinFiftyMilliseconds()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;
        var leader = new ServerNode([follower1, follower2, follower3]);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
        follower2.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
        follower3.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
    }

    // C Test #11
    [Fact]
    public void GivenACandidateJustBecameACandidateItVotesForItself()
    {
        var futureCandidate = new ServerNode([]);

        futureCandidate.TransitionToCandidate();

        Assert.True(futureCandidate.IdToVotedForMe[futureCandidate.NodeId] == true);
    }

    // C Test #8 part1
    [Fact]
    public void GivenAnElectionWhenANodeGetsMajorityOfVotesItBecomesTheLeader()
    {
        var follower1 = new ServerNode([]);
        var futureLeader = new ServerNode([follower1], startTerm:2, electionTimeout:150);

        Thread.Sleep(300);
        Assert.True(futureLeader.State == ServerState.Candidate);
    }

    // C Test #8 part2
    [Fact]
    public void GivenAnElectionWhenANodeGetsMajorityOfVotesItBecomesTheLeader2()
    {
        var follower1 = new ServerNode([]);
        var follower2 = new ServerNode([]);
        var follower3 = new ServerNode([]);

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);
        follower1.IdToNode[futureLeader.NodeId] = futureLeader;
        follower2.IdToNode[futureLeader.NodeId] = futureLeader;
        follower3.IdToNode[futureLeader.NodeId] = futureLeader;

        Thread.Sleep(300);
        Assert.True(futureLeader.State == ServerState.Leader);
        Assert.True(follower1.LeaderNodeId == futureLeader.NodeId);
        Assert.True(follower2.LeaderNodeId == futureLeader.NodeId);
        Assert.True(follower3.LeaderNodeId == futureLeader.NodeId);
    }

    // Test 9
    [Fact]
    public void GivenACandidateReceivesMajorityOfVotesWhileWaitingForUnresponsiveNodeItStillBecomesLeader()
    {
        var follower1 = new ServerNode([]);
        var follower2 = new ServerNode([]);
        var follower3 = new ServerNode([]);
        follower3.TransitionToPaused();

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);
        follower1.IdToNode[futureLeader.NodeId] = futureLeader;
        follower2.IdToNode[futureLeader.NodeId] = futureLeader;
        follower3.IdToNode[futureLeader.NodeId] = futureLeader;

        Thread.Sleep(300);
        Assert.True(futureLeader.State == ServerState.Leader);
    }

    //Test 10

    [Fact]
    public void FollowerThatHasntVotedAndIsFromPreviousTermRespondsWithYes()
    {
        var candidate = Substitute.For<IServerNode>();
        candidate.CurrentTerm = 2;

        var follower = new ServerNode([candidate], startTerm: 1);

        follower.RequestVoteRPC(candidate.NodeId, candidate.CurrentTerm);

        candidate.Received().ResponseRequestVoteRPC(follower.NodeId, wasVoteGiven:true); 
    }

    //Test 12

    // [Fact]
    // public void GivenCandidateReceivesAppendEntriesMessageFromLaterTermThenBecomesFollower()
    // {
    //     var nodeWithHigherTerm = Substitute.For<IServerNode>();
    //     nodeWithHigherTerm.CurrentTerm = 3;

    //     var candidate = new ServerNode([nodeWithHigherTerm], startTerm: 1);

    //     follower.RequestVoteRPC(candidate.NodeId, candidate.CurrentTerm);

    //     candidate.Received().ResponseRequestVoteRPC(follower.NodeId, wasVoteGiven:true); 
    // }

    // C Test #19
    [Fact]
    public void WhenACandidateWinsElection_ItImmediatelySendsAHeartbeat()
    {
        var follower1 = new ServerNode([]);
        var follower2 = new ServerNode([]);
        var follower3 = new ServerNode([]);
        follower3.TransitionToPaused();

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);
        follower1.IdToNode[futureLeader.NodeId] = futureLeader;
        follower2.IdToNode[futureLeader.NodeId] = futureLeader;
        follower3.IdToNode[futureLeader.NodeId] = futureLeader;

        var follower4 = Substitute.For<IServerNode>();
        follower4.NodeId = 1;

        futureLeader.IdToNode[follower4.NodeId] = follower4;

        Thread.Sleep(300);

        Assert.True(futureLeader.State == ServerState.Leader);
        follower4.Received().AppendEntriesRPC(futureLeader.NodeId, futureLeader.CurrentTerm);
    }

    // C Test #15

    [Fact]
    public void IfANodeReceivesSecondVoteRequestForFutureTerm_ItShouldVoteForThatNode()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 2;

        follower.RequestVoteRPC(leader.NodeId, leader.CurrentTerm);
        follower.RequestVoteRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received(2).ResponseRequestVoteRPC(follower.NodeId, true);
    }

    // C Test #16

    [Fact]
    public void ElectionTimerExpiryStartsNewElectionForCandidate()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 2;

        follower.RequestVoteRPC(leader.NodeId, leader.CurrentTerm);
        follower.RequestVoteRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received(2).ResponseRequestVoteRPC(follower.NodeId, true);
    }

    


}
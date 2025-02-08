using myclasslib;
using NSubstitute;

namespace myxunit;

public class UnitTest1
{
    // Test 3
    [Fact]
    public void WhenNewNodeIsInitializedItShouldBeInFollowerState()
    {
        ServerNode sn1 = new ServerNode([]);

        Assert.True(sn1.State == ServerState.Follower);
    }

    //Test 17
    [Fact]
    public void WhenFollowerReceivesAppendEntryRPCItSendsResponseBack()
    {
        var leader = Substitute.For<IServerNode>();
        leader.CommitIndex = -1;

        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 1;

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));

        leader.Received().ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower.NodeId, false, follower.CurrentTerm, follower.CommitIndex, null));
    }

    //Test 18
    [Fact]
    public void IfLeaderWithPreviousTermSendsEntryItGetsRejected()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 2);
        leader.CurrentTerm = 1;

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));

        leader.Received().ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower.NodeId, true, follower.CurrentTerm, follower.CommitIndex, null));
    }

    //Test 7
    [Fact]
    public void WhenFollowerGetsValidAppendEntriesRPCElectionTimerResets()
    {
        var leader = Substitute.For<IServerNode>();
        leader.CommitIndex = -1;
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 2;

        follower.StartTheThing();
        Thread.Sleep(300);

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));

        leader.Received().ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower.NodeId, false, follower.CurrentTerm, follower.CommitIndex, null));
    }

    //Test 4 
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
        var futureLeader = new ServerNode([follower1, follower2, follower3], startTerm: leaderTerm, electionTimeout: 150);

        futureLeader.StartTheThing();
        Thread.Sleep(400);

        follower1.Received().RequestVoteRPC(new RequestVoteDTO(futureLeader.NodeId, leaderTerm + 1));
        follower2.Received().RequestVoteRPC(new RequestVoteDTO(futureLeader.NodeId, leaderTerm + 1));
        follower3.Received().RequestVoteRPC(new RequestVoteDTO(futureLeader.NodeId, leaderTerm + 1));

    }

    // //Test 5 
    [Fact]
    public void NodesAreCreatedWithAtLeastOneUniqueRandomIntervalForElectionTimers()
    {
        var serverNodes = new List<ServerNode>();
        HashSet<double> randomTimerValues = new HashSet<double>();

        for (int i = 0; i < 5; i++)
        {
            serverNodes.Add(new ServerNode([]));
        }

        foreach (var serverNode in serverNodes)
        {
            serverNode.StartTheThing();
            if (serverNode.ElectionTimer == null) throw new Exception("timer was null");
            double randomIntervalForNode = serverNode.ElectionTimer.Interval;

            Assert.InRange(randomIntervalForNode, 150, 300);

            randomTimerValues.Add(randomIntervalForNode);
        }

        Assert.True(randomTimerValues.Count > 1);
    }


    //Test 6
    [Fact]
    public void WhenANewElectionBeginsTermIsIncrementedByOne()
    {
        int initialTerm = 1;
        IServerNode sn1 = Substitute.For<IServerNode>();
        ServerNode sn2 = new([sn1], startTerm: initialTerm);

        sn2.StartTheThing();
        Thread.Sleep(300);

        Assert.True(sn2.CurrentTerm >= initialTerm + 1);
    }

    //Test 2
    [Fact]
    public void WhenANodeReceivesAValidAppendEntriesFromAnotherNodeItRemembersItsId()
    {
        int followerTerm = 1;
        int leaderTerm = 2;

        var leader = Substitute.For<IServerNode>();
        leader.CurrentTerm = leaderTerm;
        var follower = new ServerNode([leader], startTerm: followerTerm);

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));

        Assert.True(follower.LeaderNodeId == leader.NodeId);
    }

    //Test 1
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

        // leader.StartTheThing();
        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));
        follower2.Received().AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));
        follower3.Received().AppendEntriesRPC(new AppendEntriesDTO(leader.NodeId, leader.CurrentTerm, leader.CommitIndex));
    }

    //Test 11
    [Fact]
    public void GivenACandidateJustBecameACandidateItVotesForItself()
    {
        var futureCandidate = new ServerNode([]);

        _ = futureCandidate.TransitionToCandidate();

        Assert.True(futureCandidate.IdToVotedForMe[futureCandidate.NodeId] == true);
    }

    //Test 8 part1
    [Fact]
    public void GivenAnElectionWhenANodeGetsMajorityOfVotesItBecomesTheLeader()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var futureLeader = new ServerNode([follower1], electionTimeout: 150);

        follower1
            .RequestVoteRPC(Arg.Any<RequestVoteDTO>())
            .Returns(async call =>
                await futureLeader.ResponseRequestVoteRPC(
                    new ResponseRequestVoteDTO(follower1.NodeId, follower1.CurrentTerm, true)
                ));

        futureLeader.StartTheThing();
        Thread.Sleep(300);
        Assert.True(futureLeader.State == ServerState.Leader);
    }

    //Test 8 part2
    [Fact]
    public void GivenAnElectionWhenANodeGetsMajorityOfVotesItBecomesTheLeader2()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);

        futureLeader.StartTheThing();

        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower1.NodeId, follower1.CurrentTerm, true));
        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower2.NodeId, follower2.CurrentTerm, true));
        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower3.NodeId, follower3.CurrentTerm, true));

        Thread.Sleep(300);
        Assert.True(futureLeader.State == ServerState.Leader);
    }

    //Test 9
    [Fact]
    public void GivenACandidateReceivesMajorityOfVotesWhileWaitingForUnresponsiveNodeItStillBecomesLeader()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);

        futureLeader.StartTheThing();
        Thread.Sleep(300);

        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower1.NodeId, follower1.CurrentTerm, true));
        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower2.NodeId, follower2.CurrentTerm, true));
        //Node 3 Doesnt respond

        Assert.True(futureLeader.State == ServerState.Leader);
    }

    //Test 10

    [Fact]
    public void FollowerThatHasntVotedAndIsFromPreviousTermRespondsWithYes()
    {
        var candidate = Substitute.For<IServerNode>();
        candidate.CurrentTerm = 2;

        var follower = new ServerNode([candidate], startTerm: 1);

        _ = follower.RequestVoteRPC(new RequestVoteDTO(candidate.NodeId, candidate.CurrentTerm));

        candidate.Received().ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower.NodeId, follower.CurrentTerm, true)); 
    }

    //Test 12

    [Fact]
    public void GivenCandidateReceivesAppendEntriesMessageFromLaterTermThenBecomesFollower()
    {
        var nodeWithHigherTerm = Substitute.For<IServerNode>();
        nodeWithHigherTerm.CurrentTerm = 3;

        var candidate = new ServerNode([nodeWithHigherTerm], startTerm: 1);

        nodeWithHigherTerm.AppendEntriesRPC(new AppendEntriesDTO(nodeWithHigherTerm.NodeId, nodeWithHigherTerm.CurrentTerm, nodeWithHigherTerm.CommitIndex));

        Assert.True(candidate.State == ServerState.Follower);
    }

    //Test 13

    [Fact]
    public async Task GivenCandidateReceivesAppendEntriesMessageFromEqualTermThenBecomesFollower()
    {
        var nodeWithEqualTerm = Substitute.For<IServerNode>();
        nodeWithEqualTerm.CurrentTerm = 2;

        var candidate = new ServerNode([nodeWithEqualTerm], startTerm: 1);

        candidate.StartTheThing();
        Thread.Sleep(300);

        await candidate.AppendEntriesRPC(new AppendEntriesDTO(nodeWithEqualTerm.NodeId, nodeWithEqualTerm.CurrentTerm, nodeWithEqualTerm.CommitIndex));

        Assert.True(nodeWithEqualTerm.CurrentTerm == candidate.CurrentTerm);
        Assert.True(candidate.State == ServerState.Follower);
    }

    //Test 19
    [Fact]
    public void WhenACandidateWinsElection_ItImmediatelySendsAHeartbeat()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var futureLeader = new ServerNode([follower1, follower2, follower3], electionTimeout:150);

        futureLeader.StartTheThing();

        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower1.NodeId, follower1.CurrentTerm, true));
        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower2.NodeId, follower2.CurrentTerm, true));
        _ = futureLeader.ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower3.NodeId, follower3.CurrentTerm, true));

        Thread.Sleep(300);

        Assert.True(futureLeader.State == ServerState.Leader);
        follower1.Received().AppendEntriesRPC(new AppendEntriesDTO(futureLeader.NodeId, futureLeader.CurrentTerm, futureLeader.CommitIndex));
        follower2.Received().AppendEntriesRPC(new AppendEntriesDTO(futureLeader.NodeId, futureLeader.CurrentTerm, futureLeader.CommitIndex));
        follower3.Received().AppendEntriesRPC(new AppendEntriesDTO(futureLeader.NodeId, futureLeader.CurrentTerm, futureLeader.CommitIndex));
    }

    //Test 15

    [Fact]
    public void IfANodeReceivesSecondVoteRequestForFutureTerm_ItShouldVoteForThatNode()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 2;

        _ = follower.RequestVoteRPC(new RequestVoteDTO(leader.NodeId, leader.CurrentTerm));
        _ = follower.RequestVoteRPC(new RequestVoteDTO(leader.NodeId, leader.CurrentTerm));

        leader.Received(2).ResponseRequestVoteRPC(new ResponseRequestVoteDTO(follower.NodeId, follower.CurrentTerm, true));
    }

    //Test 16

    [Fact]
    public void StartNewElection_ElectionTimerExpiresDuringElection_InitiatesNewElection()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        ServerNode node = new ServerNode([follower1, follower2, follower3]);

        node.StartTheThing();

        Thread.Sleep(600);
        Assert.True(node.numberOfElectionsCalled >= 2);
    }

    //Test 14

    [Fact]
    public void RequestVote_SameTermSecondRequest_RespondsNo()
    {
        var candidate1 = Substitute.For<IServerNode>();
        candidate1.NodeId = 1;
        candidate1.CurrentTerm = 3;

        var candidate2 = Substitute.For<IServerNode>();
        candidate2.NodeId = 2;
        candidate2.CurrentTerm = 3;

        ServerNode voter = new ServerNode([candidate1, candidate2], startTerm: 3);

        _ = voter.RequestVoteRPC(new RequestVoteDTO(candidate1.NodeId, candidate1.CurrentTerm));
        _ = voter.RequestVoteRPC(new RequestVoteDTO(candidate2.NodeId, candidate2.CurrentTerm));

        candidate1.Received().ResponseRequestVoteRPC(new ResponseRequestVoteDTO(voter.NodeId, voter.CurrentTerm, true));
        candidate2.Received().ResponseRequestVoteRPC(new ResponseRequestVoteDTO(voter.NodeId, voter.CurrentTerm, false));
    }

}
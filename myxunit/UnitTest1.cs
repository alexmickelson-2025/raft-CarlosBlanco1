using System.Threading.Tasks;
using myclasslib;
using NSubstitute;

namespace myxunit;

public class UnitTest1
{
    //Test 3
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
        ServerNode follower = new ServerNode([leader], 1);
        leader.CurrentTerm = 1;

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received().ResponseAppendEntriesRPC(follower.NodeId, Arg.Any<bool>());
    }

    //Test 18
    [Fact]
    public void IfLeaderWithPreviousTermSendsEntryItGetsRejected()
    {
        var leader = Substitute.For<IServerNode>();
        ServerNode follower = new ServerNode([leader], 2);
        leader.CurrentTerm = 1;

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

        leader.Received().ResponseAppendEntriesRPC(follower.NodeId, isResponseRejecting:true);
    }

    //Test 7
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
        var futureLeader = new ServerNode([follower1, follower2, follower3], startTerm: leaderTerm, electionTimeout:150);

        Thread.Sleep(300);
        follower1.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);
        follower2.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);
        follower3.Received().RequestVoteRPC(futureLeader.NodeId, leaderTerm + 1);

    }

    //Test 5 
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
            if(serverNode.ElectionTimer == null) throw new Exception("timer was null");
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
        ServerNode sn2 = new([sn1], startTerm:initialTerm);

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

        follower.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);

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

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
        follower2.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
        follower3.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm);
    }

    //Test 11
    [Fact]
    public void GivenACandidateJustBecameACandidateItVotesForItself()
    {
        var futureCandidate = new ServerNode([]);

        futureCandidate.TransitionToCandidate();

        Assert.True(futureCandidate.IdToVotedForMe[futureCandidate.NodeId] == true);
    }

    //Test 8 part1
    [Fact]
    public void GivenAnElectionWhenANodeGetsMajorityOfVotesItBecomesTheLeader()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var futureLeader = new ServerNode([follower1], electionTimeout:150);

        follower1.When(f => f.RequestVoteRPC(Arg.Any<long>(), Arg.Any<int>())).Do(_ =>{futureLeader.ResponseRequestVoteRPC(follower1.NodeId, true);});

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

        futureLeader.ResponseRequestVoteRPC(follower1.NodeId, true);
        futureLeader.ResponseRequestVoteRPC(follower2.NodeId, true);
        futureLeader.ResponseRequestVoteRPC(follower3.NodeId, true);

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

        Thread.Sleep(300);
        
        futureLeader.ResponseRequestVoteRPC(follower1.NodeId, true);
        futureLeader.ResponseRequestVoteRPC(follower2.NodeId, true);
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

        follower.RequestVoteRPC(candidate.NodeId, candidate.CurrentTerm);

        candidate.Received().ResponseRequestVoteRPC(follower.NodeId, wasVoteGiven:true); 
    }

    //Test 12

    [Fact]
    public void GivenCandidateReceivesAppendEntriesMessageFromLaterTermThenBecomesFollower()
    {
        var nodeWithHigherTerm = Substitute.For<IServerNode>();
        nodeWithHigherTerm.CurrentTerm = 3;

        var candidate = new ServerNode([nodeWithHigherTerm], startTerm: 1);

        nodeWithHigherTerm.AppendEntriesRPC(nodeWithHigherTerm.NodeId, nodeWithHigherTerm.CurrentTerm);

        Assert.True(candidate.State == ServerState.Follower);
    }

    //Test 13

    [Fact]
    public async Task GivenCandidateReceivesAppendEntriesMessageFromEqualTermThenBecomesFollower()
    {
        var nodeWithEqualTerm = Substitute.For<IServerNode>();
        nodeWithEqualTerm.CurrentTerm = 2;

        var candidate = new ServerNode([nodeWithEqualTerm], startTerm: 1);

        Thread.Sleep(300);

        await candidate.AppendEntriesRPC(nodeWithEqualTerm.NodeId, nodeWithEqualTerm.CurrentTerm);

        Console.WriteLine(candidate.State);
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

        futureLeader.ResponseRequestVoteRPC(follower1.NodeId, true);
        futureLeader.ResponseRequestVoteRPC(follower2.NodeId, true);
        futureLeader.ResponseRequestVoteRPC(follower3.NodeId, true);

        Thread.Sleep(300);

        Assert.True(futureLeader.State == ServerState.Leader);
        follower1.Received().AppendEntriesRPC(futureLeader.NodeId, futureLeader.CurrentTerm);
        follower2.Received().AppendEntriesRPC(futureLeader.NodeId, futureLeader.CurrentTerm);
        follower3.Received().AppendEntriesRPC(futureLeader.NodeId, futureLeader.CurrentTerm);
    }

    //Test 15

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

        voter.RequestVoteRPC(candidate1.NodeId, candidate1.CurrentTerm);
        voter.RequestVoteRPC(candidate2.NodeId, candidate2.CurrentTerm);

        candidate1.Received().ResponseRequestVoteRPC(voter.NodeId, true);
        candidate2.Received().ResponseRequestVoteRPC(voter.NodeId, false);
    }

}
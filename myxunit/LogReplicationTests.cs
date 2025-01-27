using System.Security.Cryptography;
using System.Threading.Tasks;
using myclasslib;
using NSubstitute;

namespace myxunit;
public class LogReplicationTests
{
    // Test 3
    [Fact]
    public void WhenANewNodeIsInitializedItsLogIsEmpty()
    {
        var node = new ServerNode([]);
        Assert.True(node.Logs.Count == 0);
    }

    // Test 2
    [Fact]
    public void WhenALeaderReceivesCommandFromClientItAppendsItToItsLog()
    {
        var leader = new ServerNode([]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Assert.Contains(newLogEntry, leader.Logs);
        Assert.True(leader.Logs.Count == 1);
    }

    // Test 1
    [Fact]
    public void WhenLeaderReceivesnewLogEntryItSendsItInRPCToAllNodes()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
        follower2.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
        follower3.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
    }

    // Test 16
    [Fact]
    public void WhenALeaderSendsAHeartbeatWithTheLogButDoesntReceiveResponseEntryIsUncommited()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(50);

        Assert.True(leader.CommitIndex == 0);
    }

    // Test 17
    [Fact]
    public void IfLeaderDoesntReceiveResponseItContinuesToSendLogEntryInSubsequentHeartbeat()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var leader = new ServerNode([follower1]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(100);
        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
        follower1.ClearReceivedCalls();

        Thread.Sleep(100);
        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
    }

    // Test 6
    [Fact]
    public void HighestCommitedIndexFromLeaderIsIncludedInAppendEntriesRPCs()
    {
         var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var leader = new ServerNode([follower1]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(200);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex);
    }

    //Test 5
    [Fact]
    public void LeaderHasNextIndexForEachFollower()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);

        leader.TransitionToLeader();

        Assert.True(leader.IdToNextIndex.ContainsKey(follower1.NodeId));
        Assert.True(leader.IdToNextIndex.ContainsKey(follower2.NodeId));
        Assert.True(leader.IdToNextIndex.ContainsKey(follower3.NodeId));
    }

    //Test 4
    [Fact]
    public void WhenALeaderWinsAnElectionItInitializesNextIndexToEachFollowerJustAfterLastOneInLog()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);
        var newLogEntry = new LogEntry(_term: 1, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);
        leader.SendCommandToLeader(newLogEntry);
        leader.SendCommandToLeader(newLogEntry);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        Assert.True(leader.IdToNextIndex[follower1.NodeId] == 3);
        Assert.True(leader.IdToNextIndex[follower2.NodeId] == 3);
        Assert.True(leader.IdToNextIndex[follower3.NodeId] == 3);
    }

    // Test 10
    [Fact]
    public void GivenFollowerReceivesAppendEntriesWithLogsItAppendsThemToItsLog()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var follower = new ServerNode([fakeLeader]);
        
        var newLogEntry1 = new LogEntry(_term: 2, _command: "SET 8 -> XD");
        var newLogEntry2 = new LogEntry(_term: 2, _command: "SET 5 -> TAMAL");

        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, newLogEntry1, fakeLeader.CommitIndex);
        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, newLogEntry2, fakeLeader.CommitIndex);

        Assert.True(follower.Logs.Count == 2);
        Assert.Contains(newLogEntry1, follower.Logs);
        Assert.Contains(newLogEntry2, follower.Logs);
    }

    //Test 11
    [Fact]
    public void FollowersResponseToAppendEntriesIncludesTermNumberAndSucess()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var follower = new ServerNode([fakeLeader]);
        
        var newLogEntry1 = new LogEntry(_term: 2, _command: "SET 8 -> XD");

        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, newLogEntry1, fakeLeader.CommitIndex);

        fakeLeader.Received().ResponseAppendEntriesRPC(follower.NodeId, isResponseRejecting: false, follower.CurrentTerm, follower.CommitIndex);
    }

    //Test 9 
    [Fact]

    public void LeaderCommitsLogByIncrementingItsLogIndex()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var initialCommitIndex = leader.CommitIndex;

        var newLogEntry = new LogEntry(_term: 2, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(follower1.NodeId, isResponseRejecting: false, follower1.CurrentTerm, follower1.CommitIndex));

        Thread.Sleep(50);

        Assert.True(initialCommitIndex < leader.CommitIndex);
    }

    //Test 8 Q: How is this one different from test 9, aren't we just checking that the commit index increased in both of them?

    //Test 12
    [Fact]

    public void LeaderSendsConfirmationResponseToClientAfterCommit()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var initialCommitIndex = leader.CommitIndex;

        var newLogEntry = new LogEntry(_term: 2, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(follower1.NodeId, isResponseRejecting: false, follower1.CurrentTerm, follower1.CommitIndex));

        Thread.Sleep(50);

        Assert.True(initialCommitIndex < leader.CommitIndex);
        Assert.True(leader.wasResponseToClientSent == true);
    }

    //Test 18
    [Fact]
    public void IfLeaderCanotCommitItDoesntSendResponse()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var initialCommitIndex = leader.CommitIndex;

        var newLogEntry = new LogEntry(_term: 2, _command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, newLogEntry, leader.CommitIndex))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(follower1.NodeId, isResponseRejecting: true, follower1.CurrentTerm, follower1.CommitIndex));

        Thread.Sleep(50);

        Assert.True(leader.wasResponseToClientSent == false);
    }

    //Test 19 Q: What exactly does too far in the future mean, like how far away from its current term

    [Fact]

    public void WhenLeaderGetsPausedOtherNodesDontGetHeartbeatfor400ms()
    {
        var follower1 = Substitute.For<IServerNode>();

        var leader = new ServerNode([follower1]);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, null, leader.CommitIndex);
        leader.TransitionToPaused();

        follower1.ClearReceivedCalls();

        Thread.Sleep(400);

        follower1.DidNotReceive().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, null, leader.CommitIndex);
    }

    [Fact]
    public void WhenNodeIsLeaderThenTheyPauseThenNoEntriesThenUnpauseAndHeartbeatContinues()
    {
        var follower1 = Substitute.For<IServerNode>();

        var leader = new ServerNode([follower1]);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, null, leader.CommitIndex);
        leader.TransitionToPaused();

        follower1.ClearReceivedCalls();

        Thread.Sleep(400);

        follower1.DidNotReceive().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, null, leader.CommitIndex);
        leader.TransitionToLeader();

        follower1.ClearReceivedCalls();

        Thread.Sleep(50);
        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, null, leader.CommitIndex);
    }

    [Fact]
    public void WhenFollowerGetsPausedItDoesntTimeout()
    {
        var follower = new ServerNode([]);

        follower.TransitionToPaused();

        Thread.Sleep(300);

        Assert.False(follower.State == ServerState.Candidate);
    }

    [Fact]
    public async Task WhenFollowerGetsUnpausedItBecomesCandidate()
    {
        var follower = new ServerNode([]);

        await follower.TransitionToPaused();

        Thread.Sleep(300);

        Assert.False(follower.State == ServerState.Candidate);
        await follower.TransitionToFollower();

        Thread.Sleep(800);
        Console.WriteLine(follower.State);

        Assert.True(follower.State == ServerState.Candidate);
    }
}
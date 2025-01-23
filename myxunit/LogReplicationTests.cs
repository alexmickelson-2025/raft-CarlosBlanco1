using System.Security.Cryptography;
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

        var command = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(command);

        Assert.Contains(command, leader.Logs);
        Assert.True(leader.Logs.Count == 1);
    }

    // Test 1
    [Fact]
    public void WhenLeaderReceivesClientCommandItSendsItInRPCToAllNodes()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);
        leader.TransitionToLeader();

        var clientCommand = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(clientCommand);

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, clientCommand, leader.CommitIndex);
        follower2.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, clientCommand, leader.CommitIndex);
        follower3.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, clientCommand, leader.CommitIndex);
    }

    // Test 16
    [Fact]
    public void WhenALeaderSendsAHeartbeatWithTheLogButDoesntReceiveResponseEntryIsUncommited()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);
        leader.TransitionToLeader();

        var clientCommand = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(clientCommand);

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

        var clientCommand = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(clientCommand);

        Thread.Sleep(200);

        var receivedCalls = follower1.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(follower1.AppendEntriesRPC))
        .Count();

        Assert.True(receivedCalls >= 4);
    }

    // Test 6
    [Fact]
    public void HighestCommitedIndexFromLeaderIsIncludedInAppendEntriesRPCs()
    {
         var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var leader = new ServerNode([follower1]);
        leader.TransitionToLeader();

        var clientCommand = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(clientCommand);

        Thread.Sleep(200);

        follower1.Received().AppendEntriesRPC(leader.NodeId, leader.CurrentTerm, clientCommand, leader.CommitIndex);
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

    //Test 4 Q: last one in its log, not the last commited on
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
        var clientCommand = new LogEntry(_term: 1, _command: "SET 8");

        leader.SendCommandToLeader(clientCommand);
        leader.SendCommandToLeader(clientCommand);
        leader.SendCommandToLeader(clientCommand);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        Assert.True(leader.IdToNextIndex[follower1.NodeId] == 2);
        Assert.True(leader.IdToNextIndex[follower2.NodeId] == 2);
        Assert.True(leader.IdToNextIndex[follower3.NodeId] == 2);
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
        
        var clientCommand1 = new LogEntry(_term: 2, _command: "SET 8");
        var clientCommand2 = new LogEntry(_term: 2, _command: "SET 5");

        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, clientCommand1, fakeLeader.CommitIndex);
        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, clientCommand2, fakeLeader.CommitIndex);

        Assert.True(follower.Logs.Count == 2);
    }

    //Test 11
    [Fact]
    public void FollowersResponseToAppendEntriesIncludesTermNumberAndLogEntryIndex()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var follower = new ServerNode([fakeLeader]);
        
        var clientCommand1 = new LogEntry(_term: 2, _command: "SET 8");

        _ = follower.AppendEntriesRPC(fakeLeader.NodeId, fakeLeader.CurrentTerm, clientCommand1, fakeLeader.CommitIndex);

        fakeLeader.Received().ResponseAppendEntriesRPC(follower.NodeId, false, follower.CurrentTerm, follower.CommitIndex);
    }
}
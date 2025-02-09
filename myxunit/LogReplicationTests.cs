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

        var newLogEntry = new LogEntry(Term: 1, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Assert.Contains(newLogEntry, leader.Logs);
        Assert.True(leader.Logs.Count == 1);
    }

    // Test 1
    [Fact]
    public async Task WhenLeaderReceivesnewLogEntryItSendsItInRPCToAllNodes()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;
        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;
        var follower3 = Substitute.For<IServerNode>();
        follower3.NodeId = 3;

        var leader = new ServerNode([follower1, follower2, follower3]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(Term: 1, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(50);

        await follower1.Received().AppendEntriesRPC(
        Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
        )
        );

        await follower2.Received().AppendEntriesRPC(
        Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
        )
        );

        await follower3.Received().AppendEntriesRPC(
        Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
        )
        );
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

        var newLogEntry = new LogEntry(Term: 1, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(50);

        Assert.Equal(leader.CommitIndex, -1);
    }

    // Test 17
    [Fact]
    public async Task IfLeaderDoesntReceiveResponseItContinuesToSendLogEntryInSubsequentHeartbeat()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var leader = new ServerNode([follower1]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(Term: 1, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(100);
        await follower1.Received().AppendEntriesRPC(
            Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
            )
            );

        follower1.ClearReceivedCalls();

        Thread.Sleep(100);
        await follower1.Received().AppendEntriesRPC(
            Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
            )
            );
    }

    // Test 6
    [Fact]
    public async Task HighestCommitedIndexFromLeaderIsIncludedInAppendEntriesRPCs()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var leader = new ServerNode([follower1]);
        leader.TransitionToLeader();

        var newLogEntry = new LogEntry(Term: 1, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        Thread.Sleep(200);

        await follower1.Received().AppendEntriesRPC(
            Arg.Is<AppendEntriesDTO>(dto =>
            dto.senderId == leader.NodeId &&
            dto.senderTerm == leader.CurrentTerm &&
            dto.highestCommittedIndex == leader.CommitIndex &&
            dto.newEntries!.Contains(newLogEntry)
            )
            );
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

        leader.TransitionToLeader();

        Thread.Sleep(50);

        Assert.True(leader.IdToNextIndex[follower1.NodeId] == -1);
        Assert.True(leader.IdToNextIndex[follower2.NodeId] == -1);
        Assert.True(leader.IdToNextIndex[follower3.NodeId] == -1);
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

        var newLogEntry1 = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry1], 0, 0));

        Assert.True(follower.Logs.Count == 1);
        Assert.Contains(newLogEntry1, follower.Logs);
    }

    //Test 11
    [Fact]
    public async Task FollowersResponseToAppendEntriesIncludesTermNumberAndSucess()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var follower = new ServerNode([fakeLeader]);

        var newLogEntry1 = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        _ = follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry1], prevLogIndex: 0));

        await fakeLeader.Received().ResponseAppendEntriesRPC(
        Arg.Is<ResponseAppendEntriesDTO>(dto =>
        dto.senderId == follower.NodeId &&
        dto.isResponseRejecting == false &&
        dto.senderTerm == fakeLeader.CurrentTerm &&
        dto.commitIndex == -1
        )
        );
    }

    //Test 9 
    [Fact]

    public void LeaderCommitsLogByIncrementingItsLogIndex()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.CurrentTerm = -1;
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.CurrentTerm = -1;
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var initialCommitIndex = leader.CommitIndex;

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, false, follower1.CurrentTerm, follower1.CommitIndex, 0)));

        follower2.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower2.NodeId, false, follower2.CurrentTerm, follower2.CommitIndex, 0)));

        Thread.Sleep(50);

        Assert.True(initialCommitIndex < leader.CommitIndex);
    }

    //Test 8
    [Fact]

    public void WhenLeaderHasReceivedMajorityOfVotesItCommits()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.CurrentTerm = -1;
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.CurrentTerm = -1;
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        Assert.True(leader.CommitIndex == -1);

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, false, follower1.CurrentTerm, follower1.CommitIndex, 0)));

        follower2.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower2.NodeId, false, follower2.CurrentTerm, follower2.CommitIndex, 0)));

        Thread.Sleep(50);

        Assert.True(leader.CommitIndex == 0);
    }


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

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, false, follower1.CurrentTerm, follower1.CommitIndex, 0)));

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

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, true, follower1.CurrentTerm, follower1.CommitIndex, 0)));

        Thread.Sleep(50);

        Assert.True(leader.wasResponseToClientSent == false);
    }

    //Test 19

    [Fact]

    public void WhenLeaderGetsPausedOtherNodesDontGetHeartbeatfor400ms()
    {
        var follower1 = Substitute.For<IServerNode>();

        var leader = new ServerNode([follower1]);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(Arg.Any<AppendEntriesDTO>());
        leader.TransitionToPaused();

        follower1.ClearReceivedCalls();

        Thread.Sleep(400);

        follower1.DidNotReceive().AppendEntriesRPC(Arg.Any<AppendEntriesDTO>());
    }

    [Fact]
    public void WhenNodeIsLeaderThenTheyPauseThenNoEntriesThenUnpauseAndHeartbeatContinues()
    {
        var follower1 = Substitute.For<IServerNode>();

        var leader = new ServerNode([follower1]);

        leader.TransitionToLeader();

        Thread.Sleep(50);

        follower1.Received().AppendEntriesRPC(Arg.Any<AppendEntriesDTO>());
        leader.TransitionToPaused();

        follower1.ClearReceivedCalls();

        Thread.Sleep(400);

        follower1.DidNotReceive().AppendEntriesRPC(Arg.Any<AppendEntriesDTO>());
        leader.TransitionToLeader();

        follower1.ClearReceivedCalls();

        Thread.Sleep(50);
        follower1.Received().AppendEntriesRPC(Arg.Any<AppendEntriesDTO>());
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

        Assert.True(follower.State == ServerState.Candidate);
    }

    //Test 13 
    [Fact]

    public void GivenLeaderCommitsLogItAppliesToInternalStateMachine()
    {
        var follower1 = Substitute.For<IServerNode>();
        follower1.NodeId = 1;

        var follower2 = Substitute.For<IServerNode>();
        follower2.NodeId = 2;

        var leader = new ServerNode([follower1, follower2]);
        leader.TransitionToLeader();

        var initialCommitIndex = leader.CommitIndex;

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        leader.SendCommandToLeader(newLogEntry);

        follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
        .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, false, follower1.CurrentTerm, follower1.CommitIndex, 0)));

        Thread.Sleep(50);

        Assert.True(leader.InternalStateMachine[8] == "XD");
    }

    //Test 14 Part 1
    [Fact]

    public async Task WhenFollowerReceivesValidHeartbeatItIncreasesItsCommitIndexToMatch()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 3;
        fakeLeader.CommitIndex = 0;

        var follower = new ServerNode([fakeLeader]);

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");
        var newLogEntry2 = new LogEntry(Term: 3, Command: "SET 9 -> XD");

        await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry, newLogEntry2], 0));

        Assert.True(follower.CommitIndex == fakeLeader.CommitIndex);
    }

    // Test 7
    [Fact]

    public async Task WhenAFollowerLearnsLogEntryIsCommitedItAppliesTheEntryToItsLocalStateMachine()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        var follower = new ServerNode([fakeLeader]);
        await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], 0));

        Assert.True(follower.InternalStateMachine[8] == "XD");
    }

    //Test 19
    [Fact]
    public async Task WhenNodeReceivesLogsTooFarInTheFutureItRejectsAppendEntries()
    {
        var fakeLeader = Substitute.For<IServerNode>();
        fakeLeader.NodeId = 1;
        fakeLeader.CurrentTerm = 2;
        fakeLeader.CommitIndex = 0;

        var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

        var follower = new ServerNode([fakeLeader]);
        await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], prevLogIndex: 100, prevLogTerm: 2));

        await fakeLeader.Received().ResponseAppendEntriesRPC(
            Arg.Is<ResponseAppendEntriesDTO>(dto =>
            dto.senderId == follower.NodeId &&
            dto.isResponseRejecting == true
            )
        );
    }

        //Test 20
        [Fact]
        public async Task WhenNodeReceivesAppendEntriesWithUnmatchingTermAndIndexItRejectsUntilItGetsMatchingLog()
        {
            var fakeLeader = Substitute.For<IServerNode>();
            fakeLeader.NodeId = 1;
            fakeLeader.CurrentTerm = 2;
            fakeLeader.CommitIndex = 0;

            var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

            var follower = new ServerNode([fakeLeader]);
            await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], prevLogIndex: 100, prevLogTerm: 2));

            await fakeLeader.Received().ResponseAppendEntriesRPC(
                Arg.Is<ResponseAppendEntriesDTO>(dto =>
                dto.senderId == follower.NodeId &&
                dto.isResponseRejecting == true
            )
            );

            fakeLeader.ClearReceivedCalls();

            await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], prevLogIndex: 30, prevLogTerm: 2));

            await fakeLeader.Received().ResponseAppendEntriesRPC(
                Arg.Is<ResponseAppendEntriesDTO>(dto =>
                dto.senderId == follower.NodeId &&
                dto.isResponseRejecting == true
            )
            );

            await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], prevLogIndex: 0));

            await fakeLeader.Received().ResponseAppendEntriesRPC(
                Arg.Is<ResponseAppendEntriesDTO>(dto =>
                dto.senderId == follower.NodeId &&
                dto.isResponseRejecting == false
            )
            );
        }

        //Test 15
        [Fact]
        public async Task WhenSendingAppendEntriesRPCLeaderIncludesOndexAndTermOfEntryThatPrecedesNewEntries()
        {
            var follower1 = Substitute.For<IServerNode>();
            follower1.NodeId = 1;

            var leader = new ServerNode([follower1]);
            await leader.TransitionToLeader();

            var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

            leader.SendCommandToLeader(newLogEntry);

            Thread.Sleep(50);

            await follower1.Received().AppendEntriesRPC(
                Arg.Is<AppendEntriesDTO>(dto =>
                    dto.senderId == leader.NodeId &&
                    dto.senderTerm == leader.CurrentTerm &&
                    dto.highestCommittedIndex == leader.CommitIndex &&
                    dto.prevLogIndex == 0 &&
                    dto.prevLogTerm == 2 &&
                    dto.newEntries.SequenceEqual(new List<LogEntry> { newLogEntry })
                )
            );
        }

        //Test 15 part 2
        [Fact]
        public async Task WhenAppendEntriesGetsRejectedLeaderDecreasesNextIndexValue()
        {
            var follower1 = Substitute.For<IServerNode>();
            follower1.NodeId = 1;

            var leader = new ServerNode([follower1]);
            await leader.TransitionToLeader();

            var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

            leader.SendCommandToLeader(newLogEntry);

            follower1.When(x => x.AppendEntriesRPC(Arg.Any<AppendEntriesDTO>()))
            .Do(async _ => await leader.ResponseAppendEntriesRPC(new ResponseAppendEntriesDTO(follower1.NodeId, isResponseRejecting: true, follower1.CurrentTerm, follower1.CommitIndex, null)));

            Thread.Sleep(50);

            Assert.True(leader.IdToNextIndex[follower1.NodeId] < 0);
        }

        //Test 15 part 3
        [Fact]
        public async Task IfFollowerDoesntFindEntryWithSameIndexAndTermItRejects()
        {
            var fakeLeader = Substitute.For<IServerNode>();
            fakeLeader.NodeId = 1;
            fakeLeader.CurrentTerm = 2;
            fakeLeader.CommitIndex = 0;

            var newLogEntry = new LogEntry(Term: 2, Command: "SET 8 -> XD");

            var follower = new ServerNode([fakeLeader]);

            follower.Logs = new List<LogEntry>(){new LogEntry(Term: 3, Command: "SET 8 -> XD")};

            await follower.AppendEntriesRPC(new AppendEntriesDTO(fakeLeader.NodeId, fakeLeader.CurrentTerm, fakeLeader.CommitIndex, [newLogEntry], prevLogIndex: 0, prevLogTerm: 45));

            await fakeLeader.Received().ResponseAppendEntriesRPC(
                Arg.Is<ResponseAppendEntriesDTO>(dto =>
                dto.senderId == follower.NodeId &&
                dto.isResponseRejecting == true
            )
            );
        }

}
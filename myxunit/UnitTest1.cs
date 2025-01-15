using myclasslib;
using System.Timers;
using NSubstitute;

namespace myxunit;

public class UnitTest1
{
    // Test #3
    [Fact]
    public void Test3()
    {
        ServerNode sn1 = new ServerNode();

        Assert.True(sn1.state == ServerState.Follower);
    }

    // Test #17
    [Fact]
    public void Test17()
    {
        var follower = Substitute.For<IServerNode>();
        var leader = Substitute.For<IServerNode>();

        leader.GetCurrentTerm().Returns(2);
        follower.GetCurrentTerm().Returns(1);

        follower.When(x => x.AppendEntriesRPC(leader)).Do(x => follower.ResponseAppendEntriesRPC(leader.GetCurrentTerm()));
        follower.AppendEntriesRPC(leader);

        follower.Received().ResponseAppendEntriesRPC(leader.GetCurrentTerm());
    }

    // Test #18
    [Fact]
    public void Test18()
    {
        ServerNode sn1 = new ServerNode(2);

        Assert.True("rejected" == sn1.ResponseAppendEntriesRPC(1));
    }

    // Test #7
    [Fact]
    public void Test7()
    {
        var follower = Substitute.For<IServerNode>();
        var leader = Substitute.For<IServerNode>();

        follower.AppendEntriesRPC(leader);

        follower.DidNotReceive().StartNewElection();
    }

    // Test #4
    [Fact]
    public void Test4()
    {
        var sn1 = Substitute.For<IServerNode>();
        sn1.When(x => x.ResetElectionTimer()).Do(x => sn1.StartNewElection());

        sn1.ResetElectionTimer();

        sn1.Received(1).StartNewElection();
    }

    // Test #5
    [Fact]
    public void Test5()
    {
        var serverNodes = new List<ServerNode>();
        List<double> randomTimerValues = new List<double>();

        for (int i = 0; i < 5; i++)
        {
            serverNodes.Add(new ServerNode());
        }

        foreach (var serverNode in serverNodes)
        {
            double randomIntervalForNode = serverNode._electionTimer.Interval;

            Assert.InRange(randomIntervalForNode, 150, 300);
            Assert.DoesNotContain(randomIntervalForNode, randomTimerValues);
            randomTimerValues.Add(randomIntervalForNode);
        }
    }

    // Test #6
    [Fact]
    public async void Test6()
    {
        int initialTerm = 1;
        ServerNode sn1 = new ServerNode(initialTerm);

        await Task.Delay(300);

        Assert.True(sn1._currentTerm >= initialTerm + 1);
    }

    // Test #2
    [Fact]
    public void Test2()
    {
        int followerTerm = 1;
        int leaderTerm = 2;

        var follower = new ServerNode(followerTerm);
        var leader = new ServerNode(leaderTerm);

        follower.AppendEntriesRPC(leader);

        Assert.True(follower.GetLeaderId() == leader.GetID());
    }

    // Test #1
    [Fact]
    public async void Test1()
    {
        var leader = Substitute.For<IServerNode>();
        leader.When(x => x.TransitionToLeader()).Do(x => leader.SendAppendEntriesRPC());

        leader.TransitionToLeader();
        await Task.Delay(50);

        leader.Received(1).SendAppendEntriesRPC();
    }

}
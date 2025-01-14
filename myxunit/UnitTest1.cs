namespace myxunit;

public class UnitTest1
{
    // 15. Given a cluster with 2 uninitialized servers
    // When the servers are initialized and start running
    // Then they should both be in follower state by default.

    [Fact]
    public void Test1()
    {
        ServerNode sn1 = new ServerNode();
        ServerNode sn2 = new ServerNode();

        Assert.True(sn1.State == "Follower");
        Assert.True(sn2.State == "Follower");
    }

    [Fact]
    public void Test2()
    {
        Assert.True(1/2 == 0);
    }
}

internal class ServerNode
{
    public string State { get; set; }
}
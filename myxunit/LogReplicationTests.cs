namespace myxunit;
public class LogReplicationTests
{
    //Test 10
    [Fact]
    public void Leader_InitializesNextIndexValues_ToIndexAfterLastInLog_WhenFirstComesToPower()
    {
        var leader = new ServerNode([]);
        Assert.True(leader.NextIndex == 1);
    }
}
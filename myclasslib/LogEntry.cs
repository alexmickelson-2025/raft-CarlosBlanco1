namespace myclasslib;
public class LogEntry
{
    public int Term {get; set;}
    public string Command {get; set;}
    public LogEntry()
    {
        
    }
    public LogEntry(int Term = 0, string Command = "SET")
    {
        this.Term = Term;
        this.Command = Command;
    }
}
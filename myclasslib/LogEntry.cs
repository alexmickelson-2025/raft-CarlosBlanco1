namespace myclasslib;
public class LogEntry
{
    public int Term {get; set;}
    public string Command {get; set;}
    public LogEntry(int _term = 0, string _command = "SET")
    {
        Term = _term;
        Command = _command;
    }
}
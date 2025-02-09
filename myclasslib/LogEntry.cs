using System.Text.Json;

namespace myclasslib;
public class LogEntry
{
    public int Term { get; set; }
    public string Command { get; set; }
    public LogEntry()
    {

    }
    public LogEntry(int Term = 0, string Command = "SET")
    {
        this.Term = Term;
        this.Command = Command;
    }
}

public class LogStore
{
    public Dictionary<long, List<LogEntry>> Logs { get; set; } = new();
}

public class LogManager
{
    private const string FilePath = "logs.json";

    public static async Task SaveLogsAsync(long nodeId, List<LogEntry> logEntries)
    {
        LogStore logStore = new();

        if (File.Exists(FilePath))
        {
            string json = await File.ReadAllTextAsync(FilePath);
            logStore = JsonSerializer.Deserialize<LogStore>(json) ?? new LogStore();
        }

        logStore.Logs[nodeId] = logEntries;

        string updatedJson = JsonSerializer.Serialize(logStore, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, updatedJson);
    }

    public static async Task<List<LogEntry>> LoadLogsAsync(long nodeId)
    {
        if (!File.Exists(FilePath))
            return new List<LogEntry>();

        string json = await File.ReadAllTextAsync(FilePath);
        LogStore logStore = JsonSerializer.Deserialize<LogStore>(json) ?? new LogStore();

        return logStore.Logs.TryGetValue(nodeId, out var logs) ? logs : new List<LogEntry>();
    }

}
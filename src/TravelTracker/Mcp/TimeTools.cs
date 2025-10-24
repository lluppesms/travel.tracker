namespace TravelTracker.Mcp;

[AllowAnonymous]

[McpServerToolType]
public class TimeTools
{
    [McpServerTool, Description("Get the current time (UTC and local).")]
    public static TimeResult GetCurrentTime()
        => new(TimeUtc: DateTimeOffset.UtcNow, TimeLocal: DateTimeOffset.Now);
}

public record TimeResult(DateTimeOffset TimeUtc, DateTimeOffset TimeLocal);

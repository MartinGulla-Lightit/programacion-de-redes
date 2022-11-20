namespace LogsServidor.Filters;

public class LogsSearchCriteria
{
    public string? UserName { get; set; }
    public string? Event { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public bool Filter(Log log)
    {
        return FilterByUserName(log) && FilterByEvent(log) && FilterByFrom(log) && FilterByTo(log);
    }

    private bool FilterByUserName(Log log)
    {
        return string.IsNullOrWhiteSpace(UserName) || log.UserName.Contains(UserName);
    }

    private bool FilterByEvent(Log log)
    {
        return string.IsNullOrWhiteSpace(Event) || log.Event.Contains(Event);
    }

    private bool FilterByFrom(Log log)
    {
        return !From.HasValue || log.Time >= From;
    }

    private bool FilterByTo(Log log)
    {
        return !To.HasValue || log.Time <= To;
    }
}



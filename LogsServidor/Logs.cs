namespace LogsServidor;

public class Logs
{
    public string UserName { get; set; }

    public string Event { get; set; }

    public DateTime Time { get; set; }

    public Logs(string userName, string evento, DateTime time)
    {
        UserName = userName;
        Event = evento;
        Time = time;
    }
}

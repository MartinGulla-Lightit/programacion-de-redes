namespace LogsServidor;

public class Log
{
    public string UserName { get; set; }

    public string Event { get; set; }

    public DateTime Time { get; set; }

    public Log()
    {
    }
    
    public Log(string userName, string evento)
    {
        UserName = userName;
        Event = evento;
        Time = DateTime.Now;
    }

    
}

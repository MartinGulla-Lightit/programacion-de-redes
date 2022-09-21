namespace AppServidor.Clases
{
    public class Mensaje
    {
        public int Sender { get; set; }
        public int Receiver { get; set; }
        public string Message { get; set; }
        public bool Read { get; set; }
        public DateTime Creado { get; set; }

        public Mensaje(int sender, int receiver, string message)
        {
            Sender = sender;
            Receiver = receiver;
            Message = message;
            Read = false;
            Creado = DateTime.Now;
        }
    }
}
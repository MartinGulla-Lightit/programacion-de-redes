namespace AppServidor.Clases
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Trabajo { get; set; }
        public string? Email { get; set; }
        public List<Mensaje> Mensajes { get; set; }

        public User(string username, string password, int id)
        {
            Username = username;
            Password = password;
            Id = id;
        }

        public void AddMensaje(Mensaje mensaje)
        {
            Mensajes.Add(mensaje);
        }
    }
}
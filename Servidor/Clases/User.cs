namespace Servidor.Clases
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string descripcion { get; set; }
        public string[] habilidades { get; set; }
        public string pathFoto { get; set; }

        public User(string username, string password, int id)
        {
            Username = username;
            Password = password;
            Id = id;
        }

        public override string ToString()
        {
            return "ID: " + Id + ", Username: " + Username;
        }

        public string ToStringConPerfil()
        {
            return "ID: " + Id + ", Username: " + Username + ", Descripcion: " + (descripcion == null ? "No tiene descripcion" : descripcion) + ", Habilidades: " + (habilidades == null ? "Sin habilidades" : string.Join(", ", habilidades));
        }
    }
}
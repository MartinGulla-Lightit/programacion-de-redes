namespace AppServidor.Clases
{
    public class Sistema
    {
        public List<User> Usuarios { get; set; }
        public List<Mensaje> Mensajes { get; set; }

        public Sistema()
        {
            Usuarios = new List<User>();
            Mensajes = new List<Mensaje>();
        }

        public string RegistrarUser(string username, string password)
        {
            if (Usuarios.Any(u => u.Username == username))
            {
                return "Usuario ya existe";
            }
            else
            {
                Usuarios.Add(new User(username, password, Usuarios.Count));
                return "Registro exitoso!";
            }
        }

        public User BuscarUsuario(int id)
        {
            return Usuarios.FirstOrDefault(u => u.Id == id);
        }

        public void AgregarMensajeAAmbosUsuarios(Mensaje mensaje)
        {
            var user1 = BuscarUsuario(mensaje.Sender);
            var user2 = BuscarUsuario(mensaje.Receiver);
            user1.AddMensaje(mensaje);
            user2.AddMensaje(mensaje);
        }

        public void ImprimirMensajes(int id)
        {
            var user = BuscarUsuario(id);
            foreach (var mensaje in user.Mensajes)
            {
                User userSender = BuscarUsuario(mensaje.Sender);
                User userReceiver = BuscarUsuario(mensaje.Receiver);
                Console.WriteLine($"De: {userSender.Username} Para: {userReceiver.Username} Mensaje: {mensaje.Message}");
            }
        }

        public string LoginUser(string username, string password)
        {
            var user = Usuarios.FirstOrDefault(u => u.Username.Equals(username) && u.Password.Equals(password));
            if (user != null)
            {
                return $"{user.Id}|{user.Username}";
            }
            else
            {
                return "Usuario o contraseña incorrectos";
            }
        }

        public string LogoutUser(string username, string password)
        {
            var user = Usuarios.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                return $"Hasta luego {user.Username}";
            }
            else
            {
                return "Usuario o contraseña incorrectos";
            }
        }

        public string CrearPerfilDeTrabajo(string userId, string descripcion, string[] habilidades){
            var user = Usuarios.FirstOrDefault(u => u.Id == int.Parse(userId));
            if (user != null && user.descripcion == null)
            {
                user.descripcion = descripcion;
                user.habilidades = habilidades;
                return $"Perfil de trabajo creado";
            }
            return "";
        }

        public string ListarPerfilesDeTrabajoFiltrados(string filtro, string datoDelFiltro){
            var perfiles = Usuarios.Where(u => u.descripcion != null);
            if (filtro.Equals("1")){
                perfiles = perfiles.Where(u => u.Username.Contains(datoDelFiltro));
            }
            else if (filtro.Equals("2")){
                perfiles = perfiles.Where(u => u.descripcion.Contains(datoDelFiltro));
            }
            else if (filtro.Equals("3")){
                perfiles = perfiles.Where(u => u.habilidades.Contains(datoDelFiltro));
            }
            else if (!filtro.Equals("4")){
                return "";
            }
            return string.Join("|", perfiles.Select(u => $"{u.Id}#{u.Username}#{u.descripcion}#{string.Join(", ", (u.habilidades))}"));
        }
    }
}
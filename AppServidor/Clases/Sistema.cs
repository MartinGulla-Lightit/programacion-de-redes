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
            Usuarios.Add(new User("admin", "admin", 1));
            Usuarios.Add(new User("user", "user", 2));
        }

        public string GuardarPathFoto(int id)
        {
            lock (Usuarios)
            {
                User user = BuscarUsuario(id);
                if (user != null)
                {
                    string path = "Fotos\\" + user.Username + ".jpg";
                    user.pathFoto = Path.Combine(Directory.GetCurrentDirectory(), path);
                    return "Foto guardada";
                }
                return "No se encontro el usuario";
            } 
        }

        public string RegistrarUser(string username, string password)
        {
            lock(Usuarios)
            {
                if (Usuarios.Any(u => u.Username == username))
                {
                    return "Usuario ya existe";
                }
                else
                {
                    Usuarios.Add(new User(username, password, Usuarios.Count+1));
                    return "Registro exitoso!";
                }
            }
        }

        public User BuscarUsuario(int id)
        {
            return Usuarios.FirstOrDefault(u => u.Id == id);
        }

        public User BuscarUsuarioUserName(string username)
        {
            return Usuarios.FirstOrDefault(u => u.Username == username);
        }

        public void AgregarMensaje(Mensaje mensaje)
        {
            Mensajes.Add(mensaje);
        }

        public List<Mensaje> DevolverMensajesEntreUsuarios(int sender, int receiver)
        {
            var mensajes = Mensajes.Where(m => m.Sender == sender && m.Receiver == receiver).ToList();
            var mensajes2 = Mensajes.Where(m => m.Sender == receiver && m.Receiver == sender).ToList();
            mensajes.AddRange(mensajes2);
            mensajes = mensajes.OrderBy(m => m.Creado).ToList();
            return mensajes;
        }

        public string DevolverStringConMensajesEntreUsuarios(int sender, int receiver)
        {
            var mensajes = DevolverMensajesEntreUsuarios(sender, receiver);
            var stringMensajes = "";
            foreach (var mensaje in mensajes)
            {
                if(mensaje.Read == false && mensaje.Receiver == sender)
                {
                    stringMensajes += $"==> De: {BuscarUsuario(mensaje.Sender).Username} Para: {BuscarUsuario(mensaje.Receiver).Username} Mensaje: {mensaje.Message}";
                    mensaje.Read = true;
                } else {
                    stringMensajes += $"=== De: {BuscarUsuario(mensaje.Sender).Username} Para: {BuscarUsuario(mensaje.Receiver).Username} Mensaje: {mensaje.Message}";
                }
                stringMensajes += "|";
            }
            return stringMensajes;
        }

        public List<string> DevolverUserNameYCantidadMensajesSinLeer(int id)
        {
            var usuarios = Usuarios.Where(u => u.Id != id).ToList();
            var lista = new List<string>();
            foreach (var usuario in usuarios)
            {
                var cantidad = Mensajes.Count(m => m.Receiver == id && m.Sender == usuario.Id && m.Read == false);
                if (cantidad > 0)
                {
                    lista.Add($"{usuario.Username}#{cantidad}");
                }
            }
            if (lista.Count > 1)
            {
                lista = lista.OrderByDescending(l => int.Parse(l.Split('#')[1])).ToList();
            }
            return lista;
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

        public string ConsultarPerfilEspecifico(string userId){
            var usuario = BuscarUsuario(int.Parse(userId));
            if (usuario != null && usuario.descripcion != null){
                return $"{usuario.Id}#{usuario.Username}#{usuario.descripcion}#{string.Join(", ", (usuario.habilidades))}";
            }
            return "";
        }
    }
}
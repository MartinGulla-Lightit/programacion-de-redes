namespace Servidor.Clases
{
    public class Sistema
    {
        public List<User> Usuarios { get; set; }
        public List<Mensaje> Mensajes { get; set; }

        public Sistema()
        {
            Usuarios = new List<User>();
            Mensajes = new List<Mensaje>();
            Usuarios.Add(new User("admin", "admin", 1){descripcion = "Administrador", habilidades = new string[] {"Administrador"}});
            Usuarios.Add(new User("user", "user", 2){descripcion = "Usuario", habilidades = new string[] {"Usuario"}});
        }
        public string EliminarFotoDeUsuario(string id)
        {
            User user = Usuarios.Find(u => u.Id == int.Parse(id));
            if (user != null)
            {
                user.pathFoto = null;
                return "Foto eliminada";
            }
            return "No se encontro el usuario";
        }

        public string GuardarPathFoto(int id, string extension)
        {
            lock (Usuarios)
            {
                User user = BuscarUsuario(id);
                if (user != null)
                {
                    string path = "Fotos/" + user.Username + "." + extension;
                    user.pathFoto = Path.Combine(Directory.GetCurrentDirectory(), path);
                    return "Foto guardada";
                }
                return "No se encontro el usuario";
            }
        }

        public string RegistrarUser(string username, string password)
        {
            lock (Usuarios)
            {
                if (Usuarios.Any(u => u.Username == username))
                {
                    return "Usuario ya existe";
                }
                else
                {
                    Usuarios.Add(new User(username, password, Usuarios.Count + 1));
                    return "Registro exitoso!";
                }
            }
        }

        public string EditarUser(int id, string username, string password)
        {
            lock (Usuarios)
            {
                User user = BuscarUsuario(id);
                if (user != null)
                {
                    if(BuscarUsuarioUserName(username) != null)
                    {
                        return "Usuario ya existe con ese nombre";
                    }
                    user.Username = username;
                    user.Password = password;
                    return "Usuario editado";
                }
                return "No se encontro el usuario";
            }
        }

        public string EliminarUser(int id)
        {
            lock (Usuarios)
            {
                User user = BuscarUsuario(id);
                if (user != null)
                {
                    Usuarios.Remove(user);
                    return "Usuario eliminado";
                }
                return "No se encontro el usuario";
            }
        }

        public User BuscarUsuario(int id)
        {
            lock (Usuarios)
            {
                return Usuarios.FirstOrDefault(u => u.Id == id);
            }
        }

        public User BuscarUsuarioUserName(string username)
        {
            lock (Usuarios)
            {
                return Usuarios.FirstOrDefault(u => u.Username == username);
            }
        }

        public void AgregarMensaje(Mensaje mensaje)
        {
            lock (Mensajes)
            {
                Mensajes.Add(mensaje);
            }
        }

        public List<Mensaje> DevolverMensajesEntreUsuarios(int sender, int receiver)
        {
            lock (Mensajes)
            {
                var mensajes = Mensajes.Where(m => m.Sender == sender && m.Receiver == receiver).ToList();
                var mensajes2 = Mensajes.Where(m => m.Sender == receiver && m.Receiver == sender).ToList();
                mensajes.AddRange(mensajes2);
                mensajes = mensajes.OrderBy(m => m.Creado).ToList();
                return mensajes;
            }
        }

        public string DevolverStringConMensajesEntreUsuarios(int sender, int receiver)
        {
            lock (Mensajes)
            {
                var mensajes = DevolverMensajesEntreUsuarios(sender, receiver);
                var stringMensajes = "";
                foreach (var mensaje in mensajes)
                {
                    if (mensaje.Read == false && mensaje.Receiver == sender)
                    {
                        stringMensajes += $"==> De: {BuscarUsuario(mensaje.Sender).Username} Para: {BuscarUsuario(mensaje.Receiver).Username} Mensaje: {mensaje.Message}";
                        mensaje.Read = true;
                    }
                    else
                    {
                        stringMensajes += $"=== De: {BuscarUsuario(mensaje.Sender).Username} Para: {BuscarUsuario(mensaje.Receiver).Username} Mensaje: {mensaje.Message}";
                    }
                    stringMensajes += "|";
                }
                return stringMensajes;
            } 
        }

        public List<string> DevolverUserNameYCantidadMensajesSinLeer(int id)
        {
            lock (Mensajes) lock (Usuarios)
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
        }

        public string LoginUser(string username, string password)
        {
            lock (Usuarios)
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
        }

        public string LogoutUser(string username, string password)
        {
            lock (Usuarios)
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
        }

        public string CrearPerfilDeTrabajo(string userId, string descripcion, string[] habilidades)
        {
            lock (Usuarios)
            {
                var user = Usuarios.FirstOrDefault(u => u.Id == int.Parse(userId));
                if (user != null && user.descripcion == null)
                {
                    user.descripcion = descripcion;
                    user.habilidades = habilidades;
                    return $"Perfil de trabajo creado";
                }
                return "El usuario ya tiene un perfil de trabajo";
            }
        }

        public string EditarPerfilDeTrabajo(string userId, string descripcion, string[] habilidades)
        {
            lock (Usuarios)
            {
                var user = Usuarios.FirstOrDefault(u => u.Id == int.Parse(userId));
                if (user != null && user.descripcion != null)
                {
                    user.descripcion = descripcion;
                    user.habilidades = habilidades;
                    return $"Perfil de trabajo editado";
                }
                return "El usuario no tiene un perfil de trabajo";
            }
        }

        public string EliminarPerfilDeTrabajo(string userId)
        {
            lock (Usuarios)
            {
                var user = Usuarios.FirstOrDefault(u => u.Id == int.Parse(userId));
                if (user != null && user.descripcion != null)
                {
                    user.descripcion = null;
                    user.habilidades = null;
                    return $"Perfil de trabajo eliminado";
                }
                return "El usuario no tiene un perfil de trabajo";
            }
        }

        public string ListarPerfilesDeTrabajoFiltrados(string filtro, string datoDelFiltro)
        {
            lock (Usuarios)
            {
                var perfiles = Usuarios.Where(u => u.descripcion != null);
                if (filtro.Equals("1"))
                {
                    perfiles = perfiles.Where(u => u.Username.Contains(datoDelFiltro));
                }
                else if (filtro.Equals("2"))
                {
                    perfiles = perfiles.Where(u => u.descripcion.Contains(datoDelFiltro));
                }
                else if (filtro.Equals("3"))
                {
                    perfiles = perfiles.Where(u => u.habilidades.Contains(datoDelFiltro));
                }
                else if (!filtro.Equals("4"))
                {
                    return "";
                }
                return string.Join("|", perfiles.Select(u => $"{u.Id}#{u.Username}#{u.descripcion}#{string.Join(", ", (u.habilidades))}"));
            }            
        }

        public string ConsultarPerfilEspecifico(string userId)
        {
            lock (Usuarios)
            {
                var usuario = BuscarUsuario(int.Parse(userId));
                if (usuario != null && usuario.descripcion != null)
                {
                    return $"{usuario.Id}#{usuario.Username}#{usuario.descripcion}#{string.Join(", ", (usuario.habilidades))}";
                }
                return "";
            }
        }
    }
}
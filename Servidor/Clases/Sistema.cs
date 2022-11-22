using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

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
                AgregarLog(user.Username, "Eliminó su foto de perfil");
                return "Foto eliminada";
            }
            AgregarLog("Error", "No se encontró el usuario al que se le quiere eliminar la foto");
            return "No se encontro el usuario";
        }

        public string GuardarPathFoto(int id, string extension)
        {
            lock (Usuarios)
            {
                User user = BuscarUsuario(id);
                if (user != null)
                {
                    string path = "Fotos/" + user.Id.ToString() + "." + extension;
                    user.pathFoto = Path.Combine(Directory.GetCurrentDirectory(), path);
                    AgregarLog(user.Username, "Cambio su foto de perfil");
                    return "Foto guardada";
                }
                AgregarLog("Error", "No se encontró el usuario al que se le quiere guardar la foto");
                return "No se encontro el usuario";
            }
        }

        public string RegistrarUser(string username, string password)
        {
            lock (Usuarios)
            {
                if (Usuarios.Any(u => u.Username == username))
                {
                    AgregarLog("Error", "No se pudo registrar el usuario " + username + " porque ya existe");
                    return "Usuario ya existe";
                }
                else
                {
                    Usuarios.Add(new User(username, password, Usuarios.Count + 1));
                    AgregarLog(username, "Se registro");
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
                        AgregarLog("Error", "No se pudo editar el usuario " + username + " porque ya existe");
                        return "Usuario ya existe con ese nombre";
                    }
                    user.Username = username;
                    user.Password = password;
                    AgregarLog(username, "Edito su perfil");
                    return "Usuario editado";
                }
                AgregarLog("Error", "No se pudo editar el usuario " + username + " porque no existe");
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
                    AgregarLog(user.Username, "Elimino su perfil");
                    return "Usuario eliminado";
                }
                AgregarLog("Error", "No se pudo eliminar el usuario con id " + id + " porque no existe");
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
                AgregarLog(BuscarUsuario(mensaje.Sender).Username, "Envio un mensaje a " + BuscarUsuario(mensaje.Receiver).Username);
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
                    AgregarLog(username, "Inicio sesion");
                    return $"{user.Id}|{user.Username}";
                }
                else
                {
                    AgregarLog("Error", "No se pudo iniciar sesion con el usuario " + username);
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
                    AgregarLog(username, "Cerro sesion");
                    return $"Hasta luego {user.Username}";
                }
                else
                {
                    AgregarLog("Error", "No se pudo cerrar sesion con el usuario " + username);
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
                    AgregarLog(user.Username, "Creo su perfil de trabajo");
                    return $"Perfil de trabajo creado";
                }
                AgregarLog("Error", "No se pudo crear el perfil de trabajo del usuario con id " + userId + " porque no existe o ya tiene un perfil");
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
                    AgregarLog(user.Username, "Edito su perfil de trabajo");
                    return $"Perfil de trabajo editado";
                }
                AgregarLog("Error", "No se pudo editar el perfil de trabajo del usuario con id " + userId + " porque no existe o no tiene un perfil");
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
                    AgregarLog(user.Username, "Elimino su perfil de trabajo");
                    return $"Perfil de trabajo eliminado";
                }
                AgregarLog("Error", "No se pudo eliminar el perfil de trabajo del usuario con id " + userId + " porque no existe o no tiene un perfil");
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

        public void AgregarLog(string userName, string evento)
        {
            //1 - definimos un FACTORY para inicializar la conexion
            //2 - definir la connection
            //3 - definir el channel
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                //4 - Declaramos la cola de mensajes
                channel.QueueDeclare(queue: "logs",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var message = "";
                message = CrearMensajeLog(channel, userName, evento);
                Console.WriteLine(" [x] Sent {0}", message);
            }
        }

        public string CrearMensajeLog(IModel channel, string userName, string evento)
        {
            var log = new Log(userName, evento);
            
            string mensaje = JsonSerializer.Serialize(log);
            var body = Encoding.UTF8.GetBytes(mensaje);
            channel.BasicPublish(exchange: "",
                routingKey: "logs",
                basicProperties: null,
                body: body);
            return mensaje;
        }
    }
}
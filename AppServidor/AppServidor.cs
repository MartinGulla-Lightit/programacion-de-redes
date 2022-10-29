using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using Protocolo;
using AppServidor.Clases;
using Communication;


namespace AppServidor
{
    class MainClass
    {
        private static Sistema _sistema = new Sistema();
        static readonly SettingsManager settingsMngr = new SettingsManager();

        static async Task Main()
        {
            Console.WriteLine("Iniciando Aplicacion Servidor....!!!");

            var ipEndPoint = new IPEndPoint(
                IPAddress.Parse(settingsMngr.ReadSettings(ServerConfig.serverIPconfigkey)),
                int.Parse(settingsMngr.ReadSettings(ServerConfig.serverPortconfigkey)));
            var tcpListener = new TcpListener(ipEndPoint);

            tcpListener.Start(100);
            Console.WriteLine("Server started listening connections on {0}-{1}", settingsMngr.ReadSettings(ServerConfig.serverIPconfigkey),settingsMngr.ReadSettings(ServerConfig.serverPortconfigkey));

            Console.WriteLine("Server will start displaying messages from the clients");
            int clientes = 0;
            while (true)
            {
                clientes++;
                var tcpClientSocket = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                var task = Task.Run(async () => await ManejarCliente(tcpClientSocket, clientes).ConfigureAwait(false)); // Pedir un "hilo" del CLR prestado
            }
        }

        static async Task ManejarCliente(TcpClient tcpClientStocket, int nro)
        {
            try
            {
                using (var networkStream = tcpClientStocket.GetStream())
                {
                    Console.WriteLine("Cliente {0} conectado", nro);
                    bool clienteConectado = true;
                    while (clienteConectado)
                    {
                        await RecibirMensaje(networkStream);
                    }
                    Console.WriteLine("Cliente Desconectado");
                }
                tcpClientStocket.Close();
            }
            catch (SocketException)
            {
                tcpClientStocket.Close();
                Console.WriteLine("Cliente Desconectado!");
            }
        }

        static async Task RecibirMensaje(NetworkStream networkStreamCliente)
        {
            // Recibo el comando
            int offset = 0;
            int size = Constantes.Command;
            byte[] dataCommand = new byte[size];
            while (offset < size)
            {
                int recibidos = networkStreamCliente.ReadAsync(dataCommand, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();
                if (recibidos == 0)
                {
                    throw new SocketException();
                }
                offset += recibidos;
            }

            // Recibo el largo del mensaje en 4 bytes
            byte[] dataLength = new byte[Constantes.LargoFijo];
            offset = 0;
            size = Constantes.LargoFijo;
            while (offset < size)
            {
                int recibidos = networkStreamCliente.ReadAsync(dataLength, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();
                if (recibidos == 0)
                {
                    throw new SocketException();
                }
                offset += recibidos;
            }

            // Ahora recibo el mensaje 
            byte[] data = new byte[BitConverter.ToInt32(dataLength, 0)];
            // en Visual Studio no es necesario el parametro 0, solo con el buffer es suficiente
            offset = 0;
            size = BitConverter.ToInt32(dataLength, 0);
            while (offset < size)
            {
                int recibidos = networkStreamCliente.ReadAsync(data, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();
                if (recibidos == 0)
                {
                    throw new SocketException();
                }
                offset += recibidos;
            }
            string comando = Encoding.UTF8.GetString(dataCommand);
            string mensaje = Encoding.UTF8.GetString(data);
            if (comando[0] == '0')
            {
                comando = comando.Remove(0, 1);
            }
            DecidirRespuesta(comando, mensaje, networkStreamCliente);
        }

        public static void SendMessage(int command, string mensaje, NetworkStream networkStreamCliente)
        {
            byte[] data = Encoding.UTF8.GetBytes(mensaje);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            // Mando el comando
            int offset = 0;
            int size = Constantes.Command;
            string dataCommand = command > 9 ? command.ToString() : "0" + command.ToString();
            byte[] dataCommand2 = Encoding.UTF8.GetBytes(dataCommand);
            networkStreamCliente.WriteAsync(dataCommand2, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();

            // Mando el tamaño del mensaje
            offset = 0;
            size = Constantes.LargoFijo;
            networkStreamCliente.WriteAsync(dataLength, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();

            // Mando el mensaje
            offset = 0;
            size = data.Length;
            networkStreamCliente.WriteAsync(data, offset, size - offset).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static void DecidirRespuesta(string comando, string mensaje, NetworkStream networkStreamCliente)
        {
            switch (comando)
            {
                case "1":
                    Login(mensaje, networkStreamCliente);
                    break;
                case "2":
                    Registrarse(mensaje, networkStreamCliente);
                    break;
                case "3":
                    Logout(mensaje, networkStreamCliente);
                    break;
                case "4":
                    CrearPerfilDeTrabajo(mensaje, networkStreamCliente);
                    break;
                case "5":
                    ListarPerfilesDeTrabajoFiltrados(mensaje, networkStreamCliente);
                    break;
                case "6":
                    ConsultarPerfilEspecifico(mensaje, networkStreamCliente);
                    break;
                case "7":
                    CrearMensaje(mensaje, networkStreamCliente);
                    break;
                case "8":
                    ConsultarMensajeEspecifico(mensaje, networkStreamCliente);
                    break;
                case "9":
                    ListarMensajesNoLeidos(mensaje, networkStreamCliente);
                    break;
                case "10":
                    GuardarFoto(mensaje, networkStreamCliente);
                    break;
                case "11":
                    ConsultarFoto(mensaje, networkStreamCliente);
                    break;
                default:
                    break;
            }
        }

        public static void GuardarFoto(string mensaje, NetworkStream networkStreamCliente)
        {
            int id = Convert.ToInt32(mensaje);
            User user = _sistema.BuscarUsuario(id);
            if (user == null)
            {
                SendMessage(Constantes.RespuestaGuardarFotoPerfilFallido, "El usuario no existe", networkStreamCliente);
                return;
            }
            else
            {
                try
                {
                    SendMessage(Constantes.RespuestaGuardarFotoPerfilExitoso, "El usuario existe", networkStreamCliente);
                    Console.WriteLine("Antes de recibir el archivo");
                    var fileCommonHandler = new FileCommsHandler(networkStreamCliente);
                    string extension = fileCommonHandler.ReceiveFile(user.Username);
                    _sistema.GuardarPathFoto(id, extension);
                    Console.WriteLine("Archivo recibido!!");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void ConsultarFoto(string mensaje, NetworkStream networkStreamCliente)
        {
            int id = Convert.ToInt32(mensaje);
            User user = _sistema.BuscarUsuario(id);
            if (user.pathFoto == null)
            {
                SendMessage(Constantes.RespuestaConsultarFotoPerfilFallido, "El usuario no tiene foto", networkStreamCliente);
                return;
            }
            else
            {
                try
                {
                    SendMessage(Constantes.RespuestaConsultarFotoPerfilExitoso, "El usuario tiene foto", networkStreamCliente);
                    Console.WriteLine(user.pathFoto);
                    Console.WriteLine("Antes de enviar el archivo");
                    var fileCommonHandler = new FileCommsHandler(networkStreamCliente);
                    fileCommonHandler.SendFile(user.pathFoto);
                    Console.WriteLine("Archivo enviado!!");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void ConsultarMensajeEspecifico(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            int Sender = Convert.ToInt32(datos[0]);
            int Receiver = _sistema.BuscarUsuarioUserName(datos[1]) != null ? _sistema.BuscarUsuarioUserName(datos[1]).Id : 0;
            if (Receiver == 0)
            {
                SendMessage(Constantes.RespuestaListarMensajesFallido, "El usuario no existe", networkStreamCliente);
            }
            else
            {
                string respuesta = _sistema.DevolverStringConMensajesEntreUsuarios(Sender, Receiver);
                if (respuesta == null)
                {
                    SendMessage(Constantes.RespuestaListarMensajesFallido, "No hay mensajes con el usuario", networkStreamCliente);
                }
                else
                {
                    SendMessage(Constantes.RespuestaListarMensajesExitoso, respuesta, networkStreamCliente);
                }
            }

        }

        public static void ListarMensajesNoLeidos(string mensaje, NetworkStream networkStreamCliente)
        {
            int id = Convert.ToInt32(mensaje);
            List<string> userNameYCantidadMensajesSinLeer = _sistema.DevolverUserNameYCantidadMensajesSinLeer(id);
            string respuesta = string.Join("|", userNameYCantidadMensajesSinLeer);
            SendMessage(Constantes.RespuestaListarMensajesNoLeidosExitoso, respuesta, networkStreamCliente);
        }

        public static void CrearMensaje(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            int Sender = Convert.ToInt32(datos[0]);
            try
            {
                int Receiver = _sistema.BuscarUsuarioUserName(datos[1]).Id;
                if (Receiver == 0)
                {
                    SendMessage(Constantes.RespuestaEnviarMensajeFallido, "Usuario no existe", networkStreamCliente);
                }
                else
                {
                    string Texto = datos[2];
                    Mensaje mensajeNuevo = new Mensaje(Sender, Receiver, Texto);
                    _sistema.AgregarMensaje(mensajeNuevo);
                    SendMessage(Constantes.RespuestaEnviarMensajeExitoso, "Mensaje creado", networkStreamCliente);
                }
            }
            catch (Exception e)
            {
                SendMessage(Constantes.RespuestaEnviarMensajeFallido, "Usuario no existe", networkStreamCliente);
            }
        }

        public static void Login(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.LoginUser(usuario, password);
            int command = respuesta.Contains("|") ? Constantes.RespuestaLoginExistoso : Constantes.RespuestaLoginFallido;
            SendMessage(command, respuesta, networkStreamCliente);
        }

        public static void Registrarse(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.RegistrarUser(usuario, password);
            SendMessage(Constantes.RespuestaLoginExistoso, respuesta, networkStreamCliente);
        }

        public static void Logout(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.LogoutUser(usuario, password);
            SendMessage(Constantes.RespuestaLoginExistoso, respuesta, networkStreamCliente);
        }

        public static void CrearPerfilDeTrabajo(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuarioId = datos[0];
            string descripcion = datos[1];
            string[] habilidades = datos[2].Split('#');
            string respuesta = _sistema.CrearPerfilDeTrabajo(usuarioId, descripcion, habilidades);
            int command = respuesta.Length > 0 ? Constantes.RespuestaAltaPerfilTrabajoExistoso : Constantes.RespuestaAltaPerfilTrabajoFallido;
            SendMessage(command, respuesta, networkStreamCliente);
        }

        public static void ListarPerfilesDeTrabajoFiltrados(string mensaje, NetworkStream networkStreamCliente)
        {
            string[] datos = mensaje.Split('|');
            string filtro = datos[0];
            string datoDelFiltro = datos[1];
            string respuesta = _sistema.ListarPerfilesDeTrabajoFiltrados(filtro, datoDelFiltro);
            Console.WriteLine("Respuesta: {0}", respuesta);
            int command = respuesta.Length > 0 ? Constantes.RespuestaListarPerfilesTrabajoExitoso : Constantes.RespuestaListarPerfilesTrabajoFallido;
            SendMessage(command, respuesta, networkStreamCliente);
        }

        public static void ConsultarPerfilEspecifico(string mensaje, NetworkStream networkStreamCliente)
        {
            string respuesta = _sistema.ConsultarPerfilEspecifico(mensaje);
            int command = respuesta.Length > 0 ? Constantes.RespuestaConsultarPerfilEspecificoExitoso : Constantes.RespuestaConsultarPerfilEspecificoFallido;
            SendMessage(command, respuesta, networkStreamCliente);
        }
    }
}



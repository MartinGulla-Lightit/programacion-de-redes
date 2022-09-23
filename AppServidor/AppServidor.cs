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

        public static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Aplicacion Servidor....!!!");

            var socketServer = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            string ipServer = settingsMngr.ReadSettings(ServerConfig.serverIPconfigkey);
            int ipPort = int.Parse(settingsMngr.ReadSettings(ServerConfig.serverPortconfigkey));

            // localhost, puerto 5000
            var localEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), ipPort);

            socketServer.Bind(localEndpoint);
            socketServer.Listen(0);
            int clientes = 0;
            bool salir = false;


            while (!salir)
            {
                var socketClient = socketServer.Accept();
                clientes++;
                int nro = clientes;
                Console.WriteLine("Acepte un nuevo pedido de Conexion");
                new Thread(() => ManejarCliente(socketClient, nro)).Start();

            }
            Console.ReadLine();
            socketServer.Shutdown(SocketShutdown.Both);
            socketServer.Close();
        }

        static void ManejarCliente(Socket socketCliente, int nro)
        {
            try
            {
                Console.WriteLine("Cliente {0} conectado", nro);
                bool clienteConectado = true;
                while (clienteConectado)
                {
                    RecibirMensaje(socketCliente);
                }
                Console.WriteLine("Cliente Desconectado");
            }
            catch (SocketException)
            {
                Console.WriteLine("Cliente Desconectado!");
            }
        }

        public static void RecibirMensaje(Socket socketCliente)
        {
            // Recibo el comando
            int offset = 0;
            int size = Constantes.Command;
            byte[] dataCommand = new byte[size];
            while (offset < size)
            {
                int recibidos = socketCliente.Receive(dataCommand, offset, size - offset, SocketFlags.None);
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
                int recibidos = socketCliente.Receive(dataLength, offset, size - offset, SocketFlags.None);
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
                int recibidos = socketCliente.Receive(data, offset, size - offset, SocketFlags.None);
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
            DecidirRespuesta(comando, mensaje, socketCliente);
        }

        public static void SendMessage(int command, string mensaje, Socket socketCliente){
            byte[] data = Encoding.UTF8.GetBytes(mensaje);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            // Mando el comando
            int offset = 0;
            int size = Constantes.Command;
            string dataCommand = command > 9 ? command.ToString() : "0" + command.ToString();
            byte[] dataCommand2 = Encoding.UTF8.GetBytes(dataCommand);
            while (offset < size) 
            {
                int enviados = socketCliente.Send(dataCommand2, offset, size - offset, SocketFlags.None);
                if (enviados == 0) 
                {
                    throw new SocketException();   
                }
                offset += enviados;
            }

            // Mando el tamaño del mensaje
            offset = 0;
            size = Constantes.LargoFijo;
            while (offset < size) 
            {
                int enviados = socketCliente.Send(dataLength, offset, size - offset, SocketFlags.None);
                if (enviados == 0) 
                {
                    throw new SocketException();   
                }
                offset += enviados;
            }

            // Mando el mensaje
            offset = 0;
            size = data.Length;
            while (offset < size)
            {
                int enviados = socketCliente.Send(data, offset, size - offset, SocketFlags.None);
                if (enviados == 0)
                {
                    throw new SocketException();
                }
                offset += enviados;
            }
        }

        public static void DecidirRespuesta(string comando, string mensaje, Socket socketCliente)
        {
            switch (comando)
            {
                case "1":
                    Login(mensaje, socketCliente);
                    break;
                case "2":
                    Registrarse(mensaje, socketCliente);
                    break;
                case "3":
                    Logout(mensaje, socketCliente);
                    break;
                case "4":
                    CrearPerfilDeTrabajo(mensaje, socketCliente);
                    break;
                case "5":
                    ListarPerfilesDeTrabajoFiltrados(mensaje, socketCliente);
                    break;
                case "6":
                    ConsultarPerfilEspecifico(mensaje, socketCliente);
                    break;
                case "7":
                    CrearMensaje(mensaje, socketCliente);
                    break;
                case "8":
                    ConsultarMensajeEspecifico(mensaje, socketCliente);
                    break;
                case "9":
                    ListarMensajesNoLeidos(mensaje, socketCliente);
                    break;
                case "10":
                    GuardarFoto(mensaje, socketCliente);
                    break;
                case "11":
                    ConsultarFoto(mensaje, socketCliente);
                    break;
                default:
                    break;
            }
        }

        public static void GuardarFoto(string mensaje, Socket socketCliente)
        {
            int id = Convert.ToInt32(mensaje);
            User user = _sistema.BuscarUsuario(id);
            if (user == null)
            {
                SendMessage(Constantes.RespuestaGuardarFotoPerfilFallido, "El usuario no existe", socketCliente);
                return;
            } else {
                SendMessage(Constantes.RespuestaGuardarFotoPerfilExitoso, "El usuario existe", socketCliente);
                Console.WriteLine("Antes de recibir el archivo");
                var fileCommonHandler = new FileCommsHandler(socketCliente);
                fileCommonHandler.ReceiveFile(user.Username + ".jpg");
                _sistema.GuardarPathFoto(id);
                Console.WriteLine("Archivo recibido!!");
            }
        }

        public static void ConsultarFoto(string mensaje, Socket socketCliente)
        {
            int id = Convert.ToInt32(mensaje);
            User user = _sistema.BuscarUsuario(id);
            if (user.pathFoto == null)
            {
                SendMessage(Constantes.RespuestaConsultarFotoPerfilFallido, "El usuario no tiene foto", socketCliente);
                return;
            } else {
                SendMessage(Constantes.RespuestaConsultarFotoPerfilExitoso, "El usuario tiene foto", socketCliente);
                // Console.WriteLine(user.pathFoto);
                Console.WriteLine("Antes de enviar el archivo");
                var fileCommonHandler = new FileCommsHandler(socketCliente);
                fileCommonHandler.SendFile(user.pathFoto);
                Console.WriteLine("Archivo enviado!!");
            }
        }

        public static void ConsultarMensajeEspecifico(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            int Sender = Convert.ToInt32(datos[0]);
            int Receiver = _sistema.BuscarUsuarioUserName(datos[1]) !=null ? _sistema.BuscarUsuarioUserName(datos[1]).Id : 0;
            if(Receiver == 0)
            {
                SendMessage(Constantes.RespuestaListarMensajesFallido, "El usuario no existe", socketCliente);
            }
            else
            {
                string respuesta = _sistema.DevolverStringConMensajesEntreUsuarios(Sender, Receiver);
                if(respuesta == null)
                {
                    SendMessage(Constantes.RespuestaListarMensajesFallido, "No hay mensajes con el usuario", socketCliente);
                }
                else
                {
                    SendMessage(Constantes.RespuestaListarMensajesExitoso, respuesta, socketCliente);
                }
            }

        }

        public static void ListarMensajesNoLeidos(string mensaje, Socket socketCliente)
        {
            int id = Convert.ToInt32(mensaje);
            List<string> userNameYCantidadMensajesSinLeer = _sistema.DevolverUserNameYCantidadMensajesSinLeer(id);
            string respuesta = string.Join("|", userNameYCantidadMensajesSinLeer);
            SendMessage(Constantes.RespuestaListarMensajesNoLeidosExitoso, respuesta, socketCliente);
        }

        public static void CrearMensaje(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            int Sender = Convert.ToInt32(datos[0]);
            int Receiver = _sistema.BuscarUsuarioUserName(datos[1]).Id;
            if(Receiver == 0)
            {
                SendMessage(Constantes.RespuestaEnviarMensajeFallido, "Usuario no existe", socketCliente);
            }
            else
            {
                string Texto = datos[2];
                Mensaje mensajeNuevo = new Mensaje(Sender, Receiver, Texto);
                _sistema.AgregarMensaje(mensajeNuevo);
                SendMessage(Constantes.RespuestaEnviarMensajeExitoso, "Mensaje creado", socketCliente);
            }
        } 

        public static void Login(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.LoginUser(usuario, password);
            int command = respuesta.Contains("|") ? Constantes.RespuestaLoginExistoso : Constantes.RespuestaLoginFallido;
            SendMessage(command, respuesta, socketCliente);
        }

        public static void Registrarse(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.RegistrarUser(usuario, password);
            SendMessage(Constantes.RespuestaLoginExistoso, respuesta, socketCliente);
        }

        public static void Logout(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuario = datos[0];
            string password = datos[1];
            string respuesta = _sistema.LogoutUser(usuario, password);
            SendMessage(Constantes.RespuestaLoginExistoso, respuesta, socketCliente);
        }

        public static void CrearPerfilDeTrabajo(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            string usuarioId = datos[0];
            string descripcion = datos[1];
            string[] habilidades = datos[2].Split('#');
            string respuesta = _sistema.CrearPerfilDeTrabajo(usuarioId, descripcion, habilidades);
            int command = respuesta.Length > 0 ? Constantes.RespuestaAltaPerfilTrabajoExistoso : Constantes.RespuestaAltaPerfilTrabajoFallido;
            SendMessage(command, respuesta, socketCliente);
        }

        public static void ListarPerfilesDeTrabajoFiltrados(string mensaje, Socket socketCliente)
        {
            string[] datos = mensaje.Split('|');
            string filtro = datos[0];
            string datoDelFiltro = datos[1];
            string respuesta = _sistema.ListarPerfilesDeTrabajoFiltrados(filtro, datoDelFiltro);
            Console.WriteLine("Respuesta: {0}", respuesta);
            int command = respuesta.Length > 0 ? Constantes.RespuestaListarPerfilesTrabajoExitoso : Constantes.RespuestaListarPerfilesTrabajoFallido;
            SendMessage(command, respuesta, socketCliente);
        }

        public static void ConsultarPerfilEspecifico(string mensaje, Socket socketCliente){
            string respuesta = _sistema.ConsultarPerfilEspecifico(mensaje);
            int command = respuesta.Length > 0 ? Constantes.RespuestaConsultarPerfilEspecificoExitoso : Constantes.RespuestaConsultarPerfilEspecificoFallido;
            SendMessage(command, respuesta, socketCliente);
        }
    }
}



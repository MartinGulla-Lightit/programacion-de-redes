using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using Protocolo;
using AppServidor.Clases;


namespace AppServidor
{
    class MainClass
    {
        private static Sistema _sistema = new Sistema();

        public static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Aplicacion Servidor....!!!");

            var socketServer = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            // localhost, puerto 5000
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);

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


                    //// Envio una respuesta al cliente
                    //string respuesta = "OK";
                    //byte[] datarespuesta = Encoding.UTF8.GetBytes(respuesta);
                    //byte[] datarespuestaLength = BitConverter.GetBytes(data.Length);
                    //// Mando primero el tamaño
                    //socketCliente.Send(dataLength);
                    //// Mando el mensaje
                    //socketCliente.Send(datarespuesta);


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
            //Primero recibo el Header del mensaje
            int offset = 0;
            int size = Constantes.Header;
            byte[] dataHeader = new byte[size];
            while (offset < size)
            {
                int recibidos = socketCliente.Receive(dataHeader, offset, size - offset, SocketFlags.None);
                if (recibidos == 0)
                {
                    throw new SocketException();
                }
                offset += recibidos;
            }

            // Recibo el comando
            offset = 0;
            size = 1;
            byte[] dataCommand = new byte[1];
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
            string Header = Encoding.UTF8.GetString(dataHeader);
            string comando = Encoding.UTF8.GetString(dataCommand);
            string mensaje = Encoding.UTF8.GetString(data);
            Console.WriteLine("Header: {0} Comando: {1} Mensaje: {2}", Header, comando, mensaje);
            DecidirRespuesta(Header, comando, mensaje, socketCliente);

            // SendMessage(Constantes.RespuestaLoginExistoso, "Login existoso", socketCliente);
        }

        public static void SendMessage(int command, string mensaje, Socket socketCliente){
            byte[] data = Encoding.UTF8.GetBytes(mensaje);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            // Mando primero el Header
            int offset = 0;
            int size = Constantes.Header;
            byte[] dataHeader = Encoding.UTF8.GetBytes("RES");
            while (offset < size) 
            {
                int enviados = socketCliente.Send(dataHeader, offset, size - offset, SocketFlags.None);
                if (enviados == 0) 
                {
                    throw new SocketException();   
                }
                offset += enviados;
            }

            // Mando el comando
            offset = 0;
            size = Constantes.Command;
            string dataCommand = command.ToString();
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

        public static void DecidirRespuesta(string Header, string comando, string mensaje, Socket socketCliente)
        {
            if (Header == "REQ")
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
                    default:
                        break;
                }
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



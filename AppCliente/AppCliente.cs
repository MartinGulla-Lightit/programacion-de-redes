using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using AppCliente.Clases;


namespace AppCliente
{
    class MainClass
    {
        private static Sistema _sistema = new Sistema();
        public static void Main(string[] args)
        {
            Console.WriteLine("Iniciando Aplicacion Cliente....!!!");

            var socketCliente = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            var localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            socketCliente.Bind(localEndPoint);
            var serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            socketCliente.Connect(serverEndpoint);
            Console.WriteLine("Cliente Conectado al Servidor...!!!");

            bool parar = false;
            while (!parar)
            {
                ShowMainMenu();

                String mensaje = Console.ReadLine();

                if (mensaje.Equals("3"))
                {
                    parar = true;
                }
                else
                {
                    ExecuteMainAction(mensaje, socketCliente);
                }
            }
            Console.WriteLine("Cierro el Cliente");
            socketCliente.Shutdown(SocketShutdown.Both);
            socketCliente.Close();
        }

        // Seccion de mensajes
        public static void SendMessage(int command, string mensaje, Socket socketCliente){
            byte[] data = Encoding.UTF8.GetBytes(mensaje);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            // Mando primero el Header
            int offset = 0;
            int size = Constantes.Header;
            byte[] dataHeader = Encoding.UTF8.GetBytes("REQ");
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

        public static string[] RecibirMensaje(Socket socketCliente)
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
            return new string[] { Header, comando, mensaje };
        }

        // Seccion Alta de usuario
        public static void ShowMainMenu(){
            Console.WriteLine("Menu");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Registrarse");
            Console.WriteLine("3. Logout");
        }

        public static void ExecuteMainAction(string mensaje, Socket socketCliente){
            string user;
            string password;
            string message;
            string[] response = new string[3];
            switch (mensaje)
            {
                case "1":
                    Console.WriteLine("Ingrese su usuario");
                    user = Console.ReadLine();
                    Console.WriteLine("Ingrese su contraseña");
                    password = Console.ReadLine();
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Login, message, socketCliente);
                    response = RecibirMensaje(socketCliente);
                    TryLogin(response, socketCliente);
                    break;
                case "2":
                    Console.WriteLine("Ingrese nuevo nombre de usuario");
                    user = Console.ReadLine();
                    Console.WriteLine("Ingrese contraseña");
                    password = Console.ReadLine();
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Registrarse, message, socketCliente);
                    response = RecibirMensaje(socketCliente);
                    break;
                default:
                    Console.WriteLine("Opcion incorrecta");
                    break;
            }
        }

        public static void TryLogin(string[] response, Socket socketCliente){
            if(response[1].Equals(Constantes.RespuestaLoginExistoso.ToString())){
                string[] data = response[2].Split('|');
                int id = int.Parse(data[0]);
                string username = data[1];
                _sistema.Usuario = new User(id, username);
                Console.WriteLine("Login exitoso, bienvenido {0}", username);
                EnterLoggedInStatus(socketCliente);
            } else {
                Console.WriteLine("Login fallido");
            }
        }

        // Seccion de funcionalidades

        public static void EnterLoggedInStatus(Socket socketCliente){
            bool loggedIn = true;
            while(loggedIn){
                ShowLoggedInMenu();
                string option = Console.ReadLine();
                if (option.Equals("7", StringComparison.InvariantCultureIgnoreCase))
                {
                    _sistema.Usuario = null;
                    loggedIn = false;
                }
                else
                {
                    ExecuteLoggedInAction(option, socketCliente);
                }
            }
        }
        public static void ShowLoggedInMenu(){
            Console.WriteLine("Menu");
            Console.WriteLine("1. Alta perfil de trabajo");
            Console.WriteLine("2. Asociar foto al perfil de trabajo");
            Console.WriteLine("3. Listar perfiles de trabajo");
            Console.WriteLine("4. Consultar perfil especifico");
            Console.WriteLine("5. Enviar mensaje");
            Console.WriteLine("6. Listar mensajes");
            Console.WriteLine("7. Cerrar sesion");
            // Console.WriteLine("8. Cambiar Puerto????");
        }

        public static void ExecuteLoggedInAction(string option, Socket socketCliente){
            switch (option)
            {
                case "1":
                    AltaPerfilTrabajo(socketCliente);
                    break;
                case "2":
                    Console.WriteLine("AsociarFotoPerfilTrabajo()");
                    break;
                case "3":
                    Console.WriteLine("ListarPerfilesTrabajo()");
                    break;
                case "4":
                    Console.WriteLine("ConsultarPerfilEspecifico()");
                    break;
                case "5":
                    Console.WriteLine("EnviarMensaje()");
                    break;
                case "6":
                    Console.WriteLine("ListarMensajes()");
                    break;
                case "7":
                    Console.WriteLine("CerrarSesion()");
                    break;
                default:
                    Console.WriteLine("Opcion incorrecta");
                    break;
            }
        }

        public static void AltaPerfilTrabajo(Socket socketCliente)
        {
            Console.WriteLine("Ingrese descripcion del perfil");
            string descripcion = Console.ReadLine();
            List<string> listaHabilidades = new List<string>();
            bool continuar = true;
            while (continuar)
            {
                Console.WriteLine("Ingrese habilidad");
                string habilidad = Console.ReadLine();
                listaHabilidades.Add(habilidad);
                Console.WriteLine("Desea agregar otra habilidad? (s/n)");
                string respuesta = Console.ReadLine();
                if (respuesta.Equals("n", StringComparison.InvariantCultureIgnoreCase))
                {
                    continuar = false;
                }
            }
            string[] habilidadesArray = listaHabilidades.ToArray();
            string habilidades = string.Join("#", habilidadesArray);
            string message = $"{_sistema.Usuario.Id}|{descripcion}|{habilidades}";
            SendMessage(Constantes.AltaPerfilTrabajo, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[1].Equals(Constantes.RespuestaAltaPerfilTrabajoExistoso.ToString())){
                Console.WriteLine("Alta de perfil de trabajo exitosa");
            } else {
                Console.WriteLine("Alta de perfil de trabajo fallida");
            }
        }
    }
}

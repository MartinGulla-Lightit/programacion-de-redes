using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using AppCliente.Clases;
using Communication;

namespace AppCliente
{
    class MainClass
    {
        private static Sistema _sistema = new Sistema();
        static readonly SettingsManager settingsMngr = new SettingsManager();
        public static void Main(string[] args)
        {
            Console.ForegroundColor
            = ConsoleColor.Gray;
            Console.WriteLine("Iniciando Aplicacion Cliente....!!!");
            IniciarCliente();
        }
        public static void IniciarCliente()
        {
            try
            {
                var socketCliente = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                string ipServer = settingsMngr.ReadSettings(ClientConfig.serverIPconfigkey);
                string ipClient = settingsMngr.ReadSettings(ClientConfig.clientIPconfigkey);
                int serverPort = int.Parse(settingsMngr.ReadSettings(ClientConfig.serverPortconfigkey));

                var localEndPoint = new IPEndPoint(IPAddress.Parse(ipClient), 0);
                socketCliente.Bind(localEndPoint);
                var serverEndpoint = new IPEndPoint(IPAddress.Parse(ipServer), serverPort);
                socketCliente.Connect(serverEndpoint);
                Console.WriteLine("Cliente Conectado al Servidor...!!!");

                bool parar = false;
                while (!parar)
                {
                    ShowMainMenu();

                    String mensaje = ReadLine("Blue");

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
            catch (Exception ex)
            {
                Console.WriteLine("Intentando reconectar al servidor...!!!");
                System.Threading.Thread.Sleep(2000);
                IniciarCliente();
            }
        }

        public static string ReadLine(string color)
        {
            Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), color);
            string input = Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            return input;
        }

        public static string WriteLine(string text, string color)
        {
            Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), color);
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
            return text;
        }


        // Seccion de mensajes
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

        public static string[] RecibirMensaje(Socket socketCliente)
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
            return new string[] { comando, mensaje };
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
            string[] response = new string[2];
            switch (mensaje)
            {
                case "1":
                    Console.WriteLine("Ingrese su usuario");
                    user = ReadLine("Blue");
                    Console.WriteLine("Ingrese su contraseña");
                    password = ReadLine("Blue");
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Login, message, socketCliente);
                    response = RecibirMensaje(socketCliente);
                    TryLogin(response, socketCliente);
                    break;
                case "2":
                    Console.WriteLine("Ingrese nuevo nombre de usuario");
                    user = ReadLine("Blue");
                    Console.WriteLine("Ingrese contraseña");
                    password = ReadLine("Blue");
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Registrarse, message, socketCliente);
                    response = RecibirMensaje(socketCliente);
                    break;
                default:
                    WriteLine("Opcion incorrecta", "Red");
                    break;
            }
        }

        public static void TryLogin(string[] response, Socket socketCliente){
            if(response[0].Equals(Constantes.RespuestaLoginExistoso.ToString())){
                string[] data = response[1].Split('|');
                int id = int.Parse(data[0]);
                string username = data[1];
                _sistema.Usuario = new User(id, username);
                Console.WriteLine("Login exitoso, bienvenido {0}", username);
                EnterLoggedInStatus(socketCliente);
            } else {
                WriteLine(response[1], "Red");
            }
        }

        // Seccion de funcionalidades

        public static void EnterLoggedInStatus(Socket socketCliente){
            bool loggedIn = true;
            while(loggedIn){
                ShowLoggedInMenu();
                string option = ReadLine("Blue");
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
        }

        public static void ExecuteLoggedInAction(string option, Socket socketCliente){
            switch (option)
            {
                case "1":
                    AltaPerfilTrabajo(socketCliente);
                    break;
                case "2":
                    AsociarFotoPerfilTrabajo(socketCliente);
                    break;
                case "3":
                    ListarPerfilesTrabajo(socketCliente);
                    break;
                case "4":
                    ConsultarPerfilEspecifico(socketCliente);
                    break;
                case "5":
                    EnviarMensaje(socketCliente);
                    break;
                case "6":
                    ListarMensajes(socketCliente);
                    break;
                case "7":
                    Console.WriteLine("CerrarSesion()");
                    break;
                default:
                    WriteLine("Opcion incorrecta", "Red");
                    break;
            }
        }

        public static void AsociarFotoPerfilTrabajo(Socket socketCliente){
            SendMessage(Constantes.GuardarFotoPerfil, $"{_sistema.Usuario.Id}", socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals("0")){
                WriteLine(response[1], "Red");
            } else {
                try{
                    Console.WriteLine("Ingrese la ruta completa al archivo: ");
                    String abspath = ReadLine("Blue");
                    var fileCommonHandler = new FileCommsHandler(socketCliente);
                    fileCommonHandler.SendFile(abspath);
                    Console.WriteLine("Se envio el archivo al Servidor");
                } catch (Exception e){
                    Console.WriteLine("Error al enviar el archivo al Servidor");
                } 
            }
        }

        public static void EnviarMensaje(Socket socketCliente){
            Console.WriteLine("Ingrese el nombre de usuario al que desea enviar el mensaje");
            string nombreUsuario = ReadLine("Blue");
            Console.WriteLine("Ingrese el mensaje");
            string mensaje = ReadLine("Blue");
            string message = $"{_sistema.Usuario.Id}|{nombreUsuario}|{mensaje}";
            SendMessage(Constantes.EnviarMensaje, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals(Constantes.RespuestaEnviarMensajeExitoso.ToString())){
                Console.WriteLine("Mensaje enviado");
            } else {
                WriteLine("Error al enviar mensaje: Verifique que el usuario exista", "Red");
            }
        }

        public static void ListarMensajes(Socket socketCliente){
            bool error = ListarMensajesNoLeidos(socketCliente);
            if(!error){
                ListarMensajesDeUsuarioEspecifico(socketCliente);
            }
        }

        public static void ListarMensajesDeUsuarioEspecifico(Socket socketCliente){
            Console.WriteLine("Ingrese el nombre de usuario del cual desea ver los mensajes");
            string nombreUsuario = ReadLine("Blue");
            string message = $"{_sistema.Usuario.Id}|{nombreUsuario}";
            SendMessage(Constantes.ListarMensajes, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals(Constantes.RespuestaListarMensajesExitoso.ToString())){
                string[] mensajes = response[1].Split('|');
                foreach (string mensaje in mensajes)
                {
                    WriteLine(mensaje, "DarkYellow");
                }
            } else {
                WriteLine(response[1], "Red");
            }
        }

        public static bool ListarMensajesNoLeidos(Socket socketCliente){
            string message = $"{_sistema.Usuario.Id}";
            SendMessage(Constantes.ListarMeensajesNoLeidos, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals(Constantes.RespuestaListarMensajesNoLeidosExitoso.ToString())){
                if(response[1].Equals("")){
                    Console.WriteLine("No tiene mensajes no leidos");
                } else {
                     Console.WriteLine("Mensajes no leidos:");
                    string[] mensajes = response[1].Split('|');
                    foreach (string mensaje in mensajes)
                    {
                        string[] UserNameCantidad = mensaje.Split('#');
                        WriteLine($"De: {UserNameCantidad[0]} - Cantidad sin leer: {UserNameCantidad[1]}", "DarkYellow");
                    }
                }
                return false;
            } else {
                WriteLine(response[1], "Red");
                return true;
            }
        }

        public static void AltaPerfilTrabajo(Socket socketCliente)
        {
            Console.WriteLine("Ingrese descripcion del perfil");
            string descripcion = ReadLine("Blue");
            List<string> listaHabilidades = new List<string>();
            bool continuar = true;
            while (continuar)
            {
                Console.WriteLine("Ingrese habilidad");
                string habilidad = ReadLine("Blue");
                listaHabilidades.Add(habilidad);
                Console.WriteLine("Desea agregar otra habilidad? (s/n)");
                string respuesta = ReadLine("Blue");
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
            if(response[0].Equals(Constantes.RespuestaAltaPerfilTrabajoExistoso.ToString())){
                Console.WriteLine("Alta de perfil de trabajo exitosa");
            } else {
                WriteLine("Alta de perfil de trabajo fallida", "Red");
            }
        }

        public static void ListarPerfilesTrabajo(Socket socketCliente){
            Console.WriteLine("Filtrar por:");
            Console.WriteLine("1. Nombre");
            Console.WriteLine("2. Descripcion");
            Console.WriteLine("3. Habilidades");
            Console.WriteLine("4. Ninguno");
            string filtro = ReadLine("Blue");
            string valor = "";
            if (!filtro.Equals("4", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("Ingrese el valor del filtro");
                valor = ReadLine("Blue");
            }
            string message = $"{filtro}|{valor}";
            SendMessage(Constantes.ListarPerfilesTrabajo, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals(Constantes.RespuestaListarPerfilesTrabajoExitoso.ToString())){
                Console.WriteLine("Perfiles encontrados:");
                string[] perfiles = response[1].Split('|');
                foreach (string perfil in perfiles)
                {
                    string[] data = perfil.Split('#');
                    int id = int.Parse(data[0]);
                    string username = data[1];
                    string descripcion = data[2];
                    string habilidades = data[3];
                    WriteLine("<====== Inicio de perfil ======>", "DarkYellow");
                    WriteLine($"Id: {id}", "DarkYellow");
                    WriteLine($"Nombre: {username}", "DarkYellow");
                    WriteLine($"Descripcion: {descripcion}", "DarkYellow");
                    WriteLine($"Habilidades: {habilidades}", "DarkYellow");
                    WriteLine("<======= Fin del perfil =======>", "DarkYellow");
                }
            } else {
                WriteLine("Listar perfiles de trabajo fallido, puede que no haya perfiles de trabajo con los filtros aplicados", "Red");
            }
        }

        public static void ConsultarPerfilEspecifico(Socket socketCliente){
            Console.WriteLine("Ingrese el id del perfil");
            string id = ReadLine("Blue");
            string message = $"{id}";
            SendMessage(Constantes.ConsultarPerfilEspecifico, message, socketCliente);
            string[] response = RecibirMensaje(socketCliente);
            if(response[0].Equals(Constantes.RespuestaConsultarPerfilEspecificoExitoso.ToString())){
                Console.WriteLine("Perfil encontrado:");
                string[] data = response[1].Split('#');
                int idPerfil = int.Parse(data[0]);
                string username = data[1];
                string descripcion = data[2];
                string habilidades = data[3];
                WriteLine("<====== Inicio de perfil ======>", "DarkYellow");
                WriteLine($"Id: {idPerfil}", "DarkYellow");
                WriteLine($"Nombre: {username}", "DarkYellow");
                WriteLine($"Descripcion: {descripcion}", "DarkYellow");
                WriteLine($"Habilidades: {habilidades}", "DarkYellow");
                WriteLine("<======= Fin del perfil =======>", "DarkYellow");
                WriteLine("Desea descargar la foto de perfil? (s/n)", "Gray");
                string respuesta = ReadLine("Blue");
                if (respuesta.Equals("s", StringComparison.InvariantCultureIgnoreCase))
                {
                    SendMessage(Constantes.ConsultarFotoPerfil, idPerfil.ToString(), socketCliente);
                    string[] responseFoto = RecibirMensaje(socketCliente);
                    if(responseFoto[0].Equals(Constantes.RespuestaConsultarFotoPerfilExitoso.ToString())){
                        var fileCommonHandler = new FileCommsHandler(socketCliente);
                        fileCommonHandler.ReceiveFile(username + ".jpg");
                        WriteLine("Foto de perfil descargada", "Green");
                    } else {
                        WriteLine(responseFoto[1], "Red");
                    } 
                }
            } else {
                WriteLine("Consultar perfil especifico fallido, puede que no exista el usuario, o no haya dado de alta su perfil de trabajo", "Red");
            }
        }
    }
}

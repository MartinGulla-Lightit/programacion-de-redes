using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using AppCliente.Clases;
using Communication;
using System.Threading.Tasks;

namespace AppCliente
{
    class MainClass
    {
        private static Sistema _sistema = new Sistema();
        static readonly SettingsManager settingsMngr = new SettingsManager();
        static async Task Main(string[] args)
        {
            Console.ForegroundColor
            = ConsoleColor.Gray;
            Console.WriteLine("Iniciando Aplicacion Cliente....!!!");
            await IniciarCliente();
        }
        static async Task IniciarCliente()
        {
            try
            {

                var clientIpEndPoint = new IPEndPoint(
                    IPAddress.Parse(settingsMngr.ReadSettings(ClientConfig.clientIPconfigkey)),
                    int.Parse(settingsMngr.ReadSettings(ClientConfig.clientPortconfigkey)));
                var tcpClient = new TcpClient(clientIpEndPoint);
                Console.WriteLine("Trying to connect to server");

                await tcpClient.ConnectAsync(
                    IPAddress.Parse(settingsMngr.ReadSettings(ClientConfig.serverIPconfigkey)),
                    int.Parse(settingsMngr.ReadSettings(ClientConfig.serverPortconfigkey))).ConfigureAwait(false);
                var keepConnection = true;
                Console.WriteLine("Cliente Conectado al Servidor...!!!");



                await using (var networkStream = tcpClient.GetStream())
                {
                    while (keepConnection)
                    {
                        await ShowMainMenu();

                        String mensaje = ReadLine("Blue");

                        if (mensaje.Equals("3"))
                        {
                            keepConnection = false;
                        }
                        else
                        {
                            await ExecuteMainAction(mensaje, networkStream);
                        }
                    }
                }

                Console.WriteLine("Cierro el Cliente");
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Intentando reconectar al servidor...!!!");
                System.Threading.Thread.Sleep(2000);
                await IniciarCliente();
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
        static async Task SendMessage(int command, string mensaje, NetworkStream networkStream){
            byte[] data = Encoding.UTF8.GetBytes(mensaje);
            byte[] dataLength = BitConverter.GetBytes(data.Length);

            // Mando el comando
            int offset = 0;
            int size = Constantes.Command;
            string dataCommand = command > 9 ? command.ToString() : "0" + command.ToString();
            byte[] dataCommand2 = Encoding.UTF8.GetBytes(dataCommand);
            await networkStream.WriteAsync(dataCommand2, offset, size - offset).ConfigureAwait(false);    

            // Mando el tamaño del mensaje
            offset = 0;
            size = Constantes.LargoFijo;
            await networkStream.WriteAsync(dataLength, offset, size - offset).ConfigureAwait(false);    

            // Mando el mensaje
            offset = 0;
            size = data.Length;
            await networkStream.WriteAsync(data, offset, size - offset).ConfigureAwait(false);    
        }

        public static async Task<string[]> RecibirMensaje(NetworkStream networkStream)
        {
            // Recibo el comando
            int offset = 0;
            int size = Constantes.Command;
            byte[] dataCommand = new byte[size];
            while (offset < size)
            {
                var recibidos = await networkStream.ReadAsync(dataCommand, offset, size - offset).ConfigureAwait(false);
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
                int recibidos = await networkStream.ReadAsync(dataLength, offset, size - offset).ConfigureAwait(false);
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
                int recibidos = await networkStream.ReadAsync(data, offset, size - offset).ConfigureAwait(false);
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
        public static async Task ShowMainMenu(){
            Console.WriteLine("Menu");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Registrarse");
            Console.WriteLine("3. Desconectarse");
        }

        public static async Task ExecuteMainAction(string mensaje, NetworkStream networkStream){
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
                    await SendMessage(Constantes.Login, message, networkStream);
                    response = await RecibirMensaje(networkStream);
                    await TryLogin(response, networkStream);
                    break;
                case "2":
                    Console.WriteLine("Ingrese nuevo nombre de usuario");
                    user = ReadLine("Blue");
                    Console.WriteLine("Ingrese contraseña");
                    password = ReadLine("Blue");
                    message = $"{user}|{password}";
                    await SendMessage(Constantes.Registrarse, message, networkStream);
                    response = await RecibirMensaje(networkStream);
                    break;
                default:
                    WriteLine("Opcion incorrecta", "Red");
                    break;
            }
        }

        public static async Task TryLogin(string[] response, NetworkStream networkStream){
            if(response[0].Equals(Constantes.RespuestaLoginExistoso.ToString())){
                string[] data = response[1].Split('|');
                int id = int.Parse(data[0]);
                string username = data[1];
                _sistema.Usuario = new User(id, username);
                Console.WriteLine("Login exitoso, bienvenido {0}", username);
                await EnterLoggedInStatus(networkStream);
            } else {
                WriteLine(response[1], "Red");
            }
        }

        // Seccion de funcionalidades

        public static async Task EnterLoggedInStatus(NetworkStream networkStream){
            bool loggedIn = true;
            while(loggedIn){
                await ShowLoggedInMenu();
                string option = ReadLine("Blue");
                if (option.Equals("7", StringComparison.InvariantCultureIgnoreCase))
                {
                    _sistema.Usuario = null;
                    loggedIn = false;
                }
                else
                {
                    await ExecuteLoggedInAction(option, networkStream);
                }
            }
        }
        public static async Task ShowLoggedInMenu(){
            Console.WriteLine("Menu");
            Console.WriteLine("1. Alta perfil de trabajo");
            Console.WriteLine("2. Asociar foto al perfil de trabajo");
            Console.WriteLine("3. Listar perfiles de trabajo");
            Console.WriteLine("4. Consultar perfil especifico");
            Console.WriteLine("5. Enviar mensaje");
            Console.WriteLine("6. Listar mensajes");
            Console.WriteLine("7. Cerrar sesion");
        }

        public static async Task ExecuteLoggedInAction(string option, NetworkStream networkStream){
            switch (option)
            {
                case "1":
                    await AltaPerfilTrabajo(networkStream);
                    break;
                case "2":
                    await AsociarFotoPerfilTrabajo(networkStream);
                    break;
                case "3":
                    await ListarPerfilesTrabajo(networkStream);
                    break;
                case "4":
                    await ConsultarPerfilEspecifico(networkStream);
                    break;
                case "5":
                    await EnviarMensaje(networkStream);
                    break;
                case "6":
                    await ListarMensajes(networkStream);
                    break;
                case "7":
                    Console.WriteLine("CerrarSesion()");
                    break;
                default:
                    WriteLine("Opcion incorrecta", "Red");
                    break;
            }
        }

        public static async Task AsociarFotoPerfilTrabajo(NetworkStream networkStream){
            try{
                Console.WriteLine("Ingrese la ruta completa al archivo: ");
                String abspath = ReadLine("Blue");
                var fileCommonHandler = new FileCommsHandler(networkStream);
                if(!fileCommonHandler._fileHandler.FileExists(abspath))
                {
                    throw new FileNotFoundException();
                }
                await SendMessage(Constantes.GuardarFotoPerfil, $"{_sistema.Usuario.Id}", networkStream);
                string[] response = await RecibirMensaje(networkStream);
                await fileCommonHandler.SendFile(abspath);
                Console.WriteLine("Se envio el archivo al Servidor");
            } catch (Exception e){
                Console.WriteLine(e.Message);
            } 
        }

        public static async Task EnviarMensaje(NetworkStream networkStream){
            Console.WriteLine("Ingrese el nombre de usuario al que desea enviar el mensaje");
            string nombreUsuario = ReadLine("Blue");
            Console.WriteLine("Ingrese el mensaje");
            string mensaje = ReadLine("Blue");
            string message = $"{_sistema.Usuario.Id}|{nombreUsuario}|{mensaje}";
            await SendMessage(Constantes.EnviarMensaje, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
            if(response[0].Equals(Constantes.RespuestaEnviarMensajeExitoso.ToString())){
                Console.WriteLine("Mensaje enviado");
            } else {
                WriteLine("Error al enviar mensaje: Verifique que el usuario exista", "Red");
            }
        }

        public static async Task ListarMensajes(NetworkStream networkStream){
            bool error = await ListarMensajesNoLeidos(networkStream);
            if(!error){
                await ListarMensajesDeUsuarioEspecifico(networkStream);
            }
        }

        public static async Task ListarMensajesDeUsuarioEspecifico(NetworkStream networkStream){
            Console.WriteLine("Ingrese el nombre de usuario del cual desea ver los mensajes");
            string nombreUsuario = ReadLine("Blue");
            string message = $"{_sistema.Usuario.Id}|{nombreUsuario}";
            await SendMessage(Constantes.ListarMensajes, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
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

        public static async Task<bool> ListarMensajesNoLeidos(NetworkStream networkStream){
            string message = $"{_sistema.Usuario.Id}";
            await SendMessage(Constantes.ListarMeensajesNoLeidos, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
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

        public static async Task AltaPerfilTrabajo(NetworkStream networkStream)
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
            await SendMessage(Constantes.AltaPerfilTrabajo, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
            if(response[0].Equals(Constantes.RespuestaAltaPerfilTrabajoExistoso.ToString())){
                Console.WriteLine("Alta de perfil de trabajo exitosa");
            } else {
                WriteLine("Alta de perfil de trabajo fallida", "Red");
            }
        }

        public static async Task ListarPerfilesTrabajo(NetworkStream networkStream){
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
            await SendMessage(Constantes.ListarPerfilesTrabajo, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
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

        public static async Task ConsultarPerfilEspecifico(NetworkStream networkStream){
            Console.WriteLine("Ingrese el id del perfil");
            string id = ReadLine("Blue");
            string message = $"{id}";
            await SendMessage(Constantes.ConsultarPerfilEspecifico, message, networkStream);
            string[] response = await RecibirMensaje(networkStream);
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
                    await SendMessage(Constantes.ConsultarFotoPerfil, idPerfil.ToString(), networkStream);
                    string[] responseFoto = await RecibirMensaje(networkStream);
                    if(responseFoto[0].Equals(Constantes.RespuestaConsultarFotoPerfilExitoso.ToString())){
                        var fileCommonHandler = new FileCommsHandler(networkStream);
                        await fileCommonHandler.ReceiveFile(username);
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

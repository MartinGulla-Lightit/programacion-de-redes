using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Protocolo;
using AppCliente.Classes;


namespace AppCliente
{
    class MainClass
    {
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
                ShowMenu();

                String mensaje = Console.ReadLine();

                ExecuteAction(mensaje, socketCliente);
                // if (mensaje.Equals("Exit", StringComparison.InvariantCultureIgnoreCase))
                // {
                //     parar = true;
                // }
                // else
                // {
                    // try
                    // {
                    //     Response response = SendMessage(mensaje, socketCliente);
                    //     //Logica con la response
                    // }
                    // catch (Exception ex)
                    // {
                    //     Console.WriteLine("Servidor desconectado");
                    //     parar = true;
                    // }
            }
            Console.WriteLine("Cierro el Cliente");
            socketCliente.Shutdown(SocketShutdown.Both);
            socketCliente.Close();

        }

        public static Response SendMessage(int command, string mensaje, Socket socketCliente){
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

            RecibirMensaje(socketCliente);
            // // El cliente recibe la respuesta
            // byte[] datarespuestaLength = new byte[Constantes.LargoFijo];
            // int recibido = socketCliente.Receive(datarespuestaLength);
            // if (recibido == 0)
            // {
            //    throw new SocketException();
            // }

            // byte[] datarespuesta = new byte[BitConverter.ToInt32(dataLength, 0)];
            // // en Visual Studio no es necesario el parametro 0, solo con el buffer es suficiente
            // recibido = socketCliente.Receive(datarespuesta);
            // if (recibido == 0)
            // {
            //    throw new SocketException();
            // }
            // string respuesta = Encoding.UTF8.GetString(datarespuesta);
            // Console.WriteLine("El servidor respondio: {0}", respuesta);
            return new Response() { Mensaje = "OK", Error = false };
        }

        public static void ShowMenu(){
            Console.WriteLine("Menu");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Registrarse");
            Console.WriteLine("3. Logout");
        }

        public static void ExecuteAction(string mensaje, Socket socketCliente){
            string user;
            string password;
            string message;
            switch (mensaje)
            {
                case "1":
                    Console.WriteLine("Ingrese su usuario");
                    user = Console.ReadLine();
                    Console.WriteLine("Ingrese su contraseña");
                    password = Console.ReadLine();
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Login, message, socketCliente);
                    break;
                case "2":
                    Console.WriteLine("Ingrese nuevo nombre de usuario");
                    user = Console.ReadLine();
                    Console.WriteLine("Ingrese contraseña");
                    password = Console.ReadLine();
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Registrarse, message, socketCliente);
                    break;
                case "3":
                    Console.WriteLine("Ingrese usuario a salir");
                    user = Console.ReadLine();
                    Console.WriteLine("Ingrese contraseña");
                    password = Console.ReadLine();
                    message = $"{user}|{password}";
                    SendMessage(Constantes.Logout, message, socketCliente);
                    break;
                    break;
                default:
                    Console.WriteLine("Opcion incorrecta");
                    break;
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
        }
    }
}



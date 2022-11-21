using Grpc.Core;
using Servidor;
using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using Servidor.Clases;
using Communication;
using Protocolo;
using Servidor.Services;

namespace Servidor.Services
{
    public class ServicioFotos : Fotos.FotosBase
    {
        public override Task<FotoResponse> DeleteFoto(DeleteFotoRequest request, ServerCallContext context)
        {
            string response = Servidor._sistema.EliminarFotoDeUsuario(request.Id.ToString());
            return Task.FromResult(new FotoResponse { Message = response });
        }
    }
}
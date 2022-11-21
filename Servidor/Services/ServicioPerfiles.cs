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
    public class ServicioPerfiles : Profiles.ProfilesBase
    {
        public override Task<ProfileResponse> GetAllProfiles(GetAllProfilesRequest request, ServerCallContext context)
        {
            string response = "";
            Servidor._sistema.Usuarios.ForEach(user => response += user.ToStringConPerfil() + "\n");
            return Task.FromResult(new ProfileResponse { Message = response });
        }

        public override Task<ProfileResponse> CreateProfile(CreateProfileRequest request, ServerCallContext context)
        {
            string[] habilidades = request.Habilidades.Split('|');
            string response = Servidor._sistema.CrearPerfilDeTrabajo(request.Id.ToString(), request.Descripcion, habilidades);
            return Task.FromResult(new ProfileResponse { Message = response });
        }

        public override Task<ProfileResponse> EditProfile(EditProfileRequest request, ServerCallContext context)
        {
            string[] habilidades = request.Habilidades.Split('|');
            string response = Servidor._sistema.EditarPerfilDeTrabajo(request.Id.ToString(), request.Descripcion, habilidades);
            return Task.FromResult(new ProfileResponse { Message = response });
        }

        public override Task<ProfileResponse> DeleteProfile(DeleteProfileRequest request, ServerCallContext context)
        {
            string response = Servidor._sistema.EliminarPerfilDeTrabajo(request.Id.ToString());
            return Task.FromResult(new ProfileResponse { Message = response });
        }
    }
}
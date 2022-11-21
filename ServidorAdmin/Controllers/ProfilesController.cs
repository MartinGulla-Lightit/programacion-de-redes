using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ServidorAdmin;
using ServidorAdmin.Models;

namespace ServidorAdmin.Controllers;

[ApiController]
[Route("profiles")]
public class ProfilesController : ControllerBase
{
    [HttpGet]
    public async Task<string> Get()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Profiles.ProfilesClient(channel);
        var reply = await client.GetAllProfilesAsync(new GetAllProfilesRequest());
        return reply.Message;
    }

    [HttpPost("{id}")]
    public async Task<string> Post([FromBody] CreateProfileModel model, int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Profiles.ProfilesClient(channel);
        var reply = await client.CreateProfileAsync(new CreateProfileRequest(){ Id = id, Descripcion = model.Descripcion, Habilidades = model.Habilidades });
        return reply.Message;
    }

    [HttpPut("{id}")]
    public async Task<string> Put([FromBody] CreateProfileModel model, int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Profiles.ProfilesClient(channel);
        var reply = await client.EditProfileAsync(new EditProfileRequest(){ Id = id, Descripcion = model.Descripcion, Habilidades = model.Habilidades });
        return reply.Message;
    }

    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Profiles.ProfilesClient(channel);
        var reply = await client.DeleteProfileAsync(new DeleteProfileRequest(){ Id = id });
        return reply.Message;
    }
}

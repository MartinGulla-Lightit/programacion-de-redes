using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ServidorAdmin;
using ServidorAdmin.Models;

namespace ServidorAdmin.Controllers;

[ApiController]
[Route("fotos")]
public class FotosController : ControllerBase
{
    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Fotos.FotosClient(channel);
        var reply = await client.DeleteFotoAsync(new DeleteFotoRequest(){ Id = id });
        return reply.Message;
    }
}

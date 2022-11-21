using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Grpc.Net.Client;
using ServidorAdmin;
using ServidorAdmin.Models;

namespace ServidorAdmin.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<string> Get()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Users.UsersClient(channel);
        var reply = await client.GetAllUsersAsync(new GetAllUsersRequest());
        return reply.Message;
    }

    [HttpPost]
    public async Task<string> Post([FromBody] CreateUserModel model)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Users.UsersClient(channel);
        var reply = await client.CreateUserAsync(new CreateUserRequest(){ Username = model.UserName, Password = model.Password });
        return reply.Message;
    }

    [HttpPut("{id}")]
    public async Task<string> Put([FromBody] CreateUserModel model, int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Users.UsersClient(channel);
        var reply = await client.EditUserAsync(new EditUserRequest(){ Id = id, Username = model.UserName, Password = model.Password });
        return reply.Message;
    }

    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:6001");
        var client = new Users.UsersClient(channel);
        var reply = await client.DeleteUserAsync(new DeleteUserRequest(){ Id = id });
        return reply.Message;
    }
}

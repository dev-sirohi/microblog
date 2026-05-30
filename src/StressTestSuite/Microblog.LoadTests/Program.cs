using System.Net;
using System.Net.Http.Json;
using NBomber.Contracts;
using NBomber.CSharp;

var responseList = new List<string>();
var httpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5182"),
};

var s_register = Scenario.Create("api/Auth/register", async context =>
{
    Console.WriteLine("Testing PING");

    long userId = context.InvocationNumber;
    var dto = new DTO.UserRegisterDto
    {
        Username = $"user_{DateTime.UtcNow.Ticks}_{userId}",
        Email = $"email_{DateTime.UtcNow.Ticks}_{userId}@yopmail.com",
        Password = "TestPassword123!",
    };

    var response = await httpClient.PostAsJsonAsync("api/Auth/register", dto);
    Console.WriteLine("Response: " + await response.Content.ReadAsStringAsync());
    return Response.Ok();
})
.WithLoadSimulations(Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

NBomberRunner.RegisterScenarios(s_register).Run();

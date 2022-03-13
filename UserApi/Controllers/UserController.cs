using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using RvmsModels;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    private IConfiguration _configuration { get; }

    private CosmosClient _cosmosClient;

    private Database _database;

    private string _databaseName;

    private Container _container;

    private string _containerName;

    private string _partitionKey;

    public UserController(ILogger<UserController> logger, IConfiguration configuration, CosmosClient cosmosClient)
    {
        _logger = logger;
        _configuration = configuration;
        _cosmosClient = cosmosClient;

        _databaseName = _configuration["CosmosDb:DatabaseName"];
        _database = _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName).Result;
        _partitionKey = _configuration["CosmosDb:PartitionKey"];
        _containerName = _configuration["CosmosDb:ContainerName"];
        _container = _database.DefineContainer(_containerName, _partitionKey)
            .WithUniqueKey()
            .Path("/Email")
            .Attach()
            .CreateIfNotExistsAsync()
            .Result;
    }

    /// <summary>
    /// Get a registered patient.
    /// </summary>
    /// <param name="email">User email</param>
    [HttpGet("patient/{email}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RvmsModels.User))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string email)
    {
        var getUserByEmailQuery =
            new QueryDefinition("SELECT * from c WHERE c.Email = @email").WithParameter("@email", email);
        var queryResults = _container.GetItemQueryIterator<RvmsModels.User>(getUserByEmailQuery);

        // Get first user with the given email (there should only be one)
        try
        {
            while (queryResults.HasMoreResults)
            {
                FeedResponse<RvmsModels.User> users = await queryResults.ReadNextAsync();
                return Ok(users.First());
            }
        }
        catch (Exception ce)
        {
            _logger.LogError(ce, null);
        }

        return NotFound();
    }

    /// <summary>
    /// Register a new patient.
    /// </summary>
    /// <param name="registerUser">Request body containing user data.</param>
    /// <returns>User object and URL endpoint.</returns>
    [HttpPost("auth/patient/register")]
    public async Task<IActionResult> Post([FromBody] RegisterUser registerUser)
    {
        var salt = Security.GenerateSalt();
        RvmsModels.User userToRegister = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = registerUser.Name,
            Email = registerUser.Email,
            Role = Role.PATIENT,
            Salt = salt,
            Password = Security.HashPassword(salt, registerUser.Password)
        };
        try
        {
            RvmsModels.User newUser =
                await _container.CreateItemAsync(userToRegister, new PartitionKey((double) userToRegister.Role));
            return Created($"/api/user/patient/{newUser.Email}", newUser);
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, null);
            return ce.StatusCode == HttpStatusCode.Conflict ? Conflict() : StatusCode((int) ce.StatusCode);
        }
    }

    /// <summary>
    /// User Login
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns></returns>
    /// <response code="200">Returns a JWT for the user</response>
    /// <response code="401">Invalid login credentials</response>
    // [HttpPost("auth/login")]
    // [Consumes(MediaTypeNames.Application.Json)]
    // [Produces(MediaTypeNames.Application.Json)]
    // [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserLogin))]
    // [ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized)]
    // public UserLogin Post([FromBody] UserLogin userLogin)
    // {
    //     return new UserLogin() {Id = userLogin.Id, Password = userLogin.Password};
    // }
}
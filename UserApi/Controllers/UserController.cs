using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

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
        _container = _database.CreateContainerIfNotExistsAsync(_containerName, _partitionKey)
            .Result;
    }

    /// <summary>
    /// Get User document
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("{userId}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RvmsModels.User))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get(string userId)
    {
        try
        {
            RvmsModels.User user = await _container.ReadItemAsync<RvmsModels.User>(userId, new PartitionKey(userId));
            return Ok(user);
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, "Error while trying to get user {userId}", userId);
            return NotFound();
        }
    }

    // [HttpPut("{userId}")]
    // [Consumes(MediaTypeNames.Application.Json)]
    // [ProducesResponseType(StatusCodes.Status204NoContent)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> Put(string userId, [FromBody] Vitals vitals)
    // {
    //     RvmsModels.User user;
    //     PartitionKey partitionKey = new(userId);
    //     try
    //     {
    //         user = await _container.ReadItemAsync<RvmsModels.User>(userId, partitionKey);
    //     }
    //     catch (CosmosException)
    //     {
    //         return NotFound();
    //     }
    //
    //     user.Vitals.Ecg.AddRange(vitals.Ecg);
    //     user.Vitals.SkinTemperature.AddRange(vitals.SkinTemperature);
    //     user.Vitals.SpO2.AddRange(vitals.SpO2);
    //     try
    //     {
    //         await _container.UpsertItemAsync(user, partitionKey);
    //     }
    //     catch (CosmosException ce)
    //     {
    //         _logger.LogError(ce, "Error while trying to put new vitals for user {userId}", userId);
    //         return StatusCode(ce.SubStatusCode, ce.Message);
    //     }
    //
    //     return NoContent();
    // }

    /// <summary>
    /// User Login
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns></returns>
    /// <response code="200">Returns a JWT for the user</response>
    /// <response code="401">Invalid login credentials</response>
    [HttpPost("auth/login")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserLogin))]
    [ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized)]
    public UserLogin Post([FromBody] UserLogin userLogin)
    {
        return new UserLogin() {Id = userLogin.Id, Password = userLogin.Password};
    }
}
using System.ComponentModel.DataAnnotations;
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RvmsModels.Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([EmailAddress] string email)
    {
        var user = await GetPatientByEmail(email);
        if (user != null)
        {
            return Ok(user);
        }

        return NotFound();
    }

    /// <summary>
    /// Register a new patient.
    /// </summary>
    /// <param name="registerUser">Request body containing user data.</param>
    [HttpPost("auth/patient/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] RegisterUser registerUser)
    {
        var salt = Security.GenerateSalt();
        RvmsModels.Patient patientToRegister = new()
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
            Patient newPatient =
                await _container.CreateItemAsync(patientToRegister, new PartitionKey((double) patientToRegister.Role));
            return Created($"/api/user/patient/{newPatient.Email}", newPatient);
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, null);
            return ce.StatusCode == HttpStatusCode.Conflict ? Conflict() : StatusCode((int) ce.StatusCode);
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Patient Login
    /// </summary>
    /// <param name="logInUser">Email and password</param>
    /// <returns></returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid login credentials</response>
    /// <response code="404">User not found</response>
    [HttpPost("auth/patient/login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RvmsModels.Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Post([FromBody] RvmsModels.LogInUser logInUser)
    {
        var patient = await GetPatientByEmail(logInUser.Email);
        if (patient == null)
        {
            return NotFound();
        }

        if (Security.ValidatePassword(patient.Salt, patient.Password, logInUser.Password))
        {
            return Ok(patient);
        }

        return Unauthorized();
    }

    private async Task<RvmsModels.Patient?> GetPatientByEmail(string email)
    {
        var getPatientByEmailQuery =
            new QueryDefinition("SELECT * from c WHERE c.Email = @email").WithParameter("@email", email);
        var queryResults = _container.GetItemQueryIterator<RvmsModels.Patient>(getPatientByEmailQuery);

        // Get first user with the given email (there should only be one)
        try
        {
            while (queryResults.HasMoreResults)
            {
                var users = await queryResults.ReadNextAsync();
                return users.First();
            }
        }
        catch (Exception ce)
        {
            _logger.LogError(ce, "Could not find user with email {}", email);
        }

        return null;
    }
}
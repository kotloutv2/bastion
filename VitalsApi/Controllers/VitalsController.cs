using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using RvmsModels;

namespace VitalsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VitalsController : ControllerBase
{
    private readonly ILogger<VitalsController> _logger;

    private readonly CosmosDbService _cosmosDbService;
    // private IConfiguration _configuration { get; }
    //
    // private CosmosClient _cosmosClient;
    //
    // private Database _database;
    //
    // private string _databaseName;
    //
    // private Container _container;
    //
    // private string _containerName;
    //
    // private string _partitionKey;

    public VitalsController(ILogger<VitalsController> logger, CosmosDbService cosmosDbService)
    {
        // _logger = logger;
        // _configuration = configuration;
        // _cosmosClient = cosmosClient;
        //
        // _databaseName = _configuration["CosmosDb:DatabaseName"];
        // _database = _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName).Result;
        //
        // _partitionKey = _configuration["CosmosDb:PartitionKey"];
        //
        // _containerName = _configuration["CosmosDb:ContainerName"];
        // _container = _database.CreateContainerIfNotExistsAsync(_containerName, _partitionKey)
        //     .Result;
        _logger = logger;
        _cosmosDbService = cosmosDbService;
    }

    /// <summary>
    /// Get User Vitals
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns></returns>
    [HttpGet("{email}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Vitals))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get(string email)
    {
        var patient =
            await _cosmosDbService
                .GetPatientByEmail(email); // await _container.ReadItemAsync<Patient>(email, new PartitionKey(email));
        if (patient != null)
        {
            return Ok(patient.Vitals);
        }

        return NotFound();
    }

    [HttpPut("{email}")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(string email, [FromBody] Vitals vitals)
    {
        var patient = await _cosmosDbService.GetPatientByEmail(email);
        if (patient == null)
        {
            return NotFound();
        }

        var updatedPatient = await _cosmosDbService.AddVitals(patient, vitals);
        if (updatedPatient != null)
        {
            return Ok(updatedPatient);
        }

        return BadRequest(updatedPatient);
    }
}
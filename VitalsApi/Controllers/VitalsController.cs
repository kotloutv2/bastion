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

    public VitalsController(ILogger<VitalsController> logger, CosmosDbService cosmosDbService)
    {
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
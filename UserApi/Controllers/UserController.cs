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
    private CosmosDbService _cosmosDbService;

    public UserController(ILogger<UserController> logger, CosmosDbService cosmosDbService)
    {
        _logger = logger;
        _cosmosDbService = cosmosDbService;
    }

    /// <summary>
    /// Get a registered patient.
    /// </summary>
    /// <param name="email">User email</param>
    [HttpGet("patient/{email}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([EmailAddress] string email)
    {
        var user = await _cosmosDbService.GetPatientByEmail(email);
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
        try
        {
            Patient newPatient =
                await _cosmosDbService.RegisterPatient(registerUser);
            return Created($"/api/user/patient/{newPatient.Email}", newPatient);
        }
        catch (CosmosException ce)
        {
            if (ce.StatusCode == HttpStatusCode.Conflict)
            {
                return Conflict();
            }

            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during patient registration");
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Patient))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Post([FromBody] LogInUser logInUser)
    {
        var patient = await _cosmosDbService.GetPatientByEmail(logInUser.Email);
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
}
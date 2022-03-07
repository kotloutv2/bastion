using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace UserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;

    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }
    
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
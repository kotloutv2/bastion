using System.ComponentModel.DataAnnotations;

namespace UserApi;

public class UserLogin
{
    [Required] public string Id { get; set; }

    [Required] public string Password { get; set; }
}
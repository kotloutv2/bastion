using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace RvmsModels;

public enum Role
{
    PATIENT,
    ADMIN
}

public interface IUser
{
    [JsonProperty(PropertyName = "id")] public string Id { get; set; }

    [MinLength(Security.MinimumPasswordLength)]
    public string Password { get; set; }

    public string Salt { get; set; }
    public string Name { get; set; }

    public Role Role { get; }
}

public class Patient : IUser
{
    [JsonProperty(PropertyName = "id")] public string Id { get; set; }

    [MinLength(Security.MinimumPasswordLength)]
    public string Password { get; set; }

    public string Salt { get; set; }
    public string Name { get; set; }
    [EmailAddress] public string Email { get; set; }
    public Vitals Vitals { get; set; } = new();
    public Role Role => Role.PATIENT;
}

public class HealthcarePractitioner : IUser
{
    [JsonProperty(PropertyName = "id")] public string Id { get; set; }

    [MinLength(Security.MinimumPasswordLength)]
    public string Password { get; set; }

    public string Salt { get; set; }
    public string Name { get; set; }
    [EmailAddress] public string Email { get; set; }
    public IEnumerable<string> Patients { get; set; } = Enumerable.Empty<string>();
    public Role Role => Role.ADMIN;
}

public class RegisterUser
{
    public string Name { get; set; }
    [EmailAddress] public string Email { get; set; }

    [MinLength(Security.MinimumPasswordLength)]
    public string Password { get; set; }
}

public class LogInUser
{
    [EmailAddress] public string Email { get; set; }

    [MinLength(Security.MinimumPasswordLength)]
    public string Password { get; set; }
}

public class Vitals
{
    public List<TimestampedVital> Ecg { get; set; } = new();
    public List<TimestampedVital> SkinTemperature { get; set; } = new();
    public List<TimestampedVital> SpO2 { get; set; } = new();
}

public struct TimestampedVital
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
using Newtonsoft.Json;

namespace RvmsModels;

public enum Role
{
    PATIENT,
    ADMIN
}

public class User
{
    [JsonProperty(PropertyName = "id")] string Id { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public Role Role { get; set; }
    public Vitals Vitals { get; set; } = new();
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
namespace RvmsModels;

using Newtonsoft.Json;

public enum Role
{
    PATIENT,
    ADMIN
}

public class User
{
    [JsonProperty(PropertyName = "id")] public string Id { get; set; }
    [JsonProperty(PropertyName = "name")] public string Name { get; set; }
    [JsonProperty(PropertyName = "email")] public string Email { get; set; }
    [JsonProperty(PropertyName = "role")] public Role Role { get; set; }
    [JsonProperty(PropertyName = "vitals")]
    public Vitals Vitals { get; set; } = new();
}

public class Vitals
{
    [JsonProperty(PropertyName = "ECG")] public List<TimestampedVital> Ecg { get; set; } = new();
    [JsonProperty(PropertyName = "HR")] public List<TimestampedVital> HeartRate { get; set; } = new();
    [JsonProperty(PropertyName = "SpO2")] public List<TimestampedVital> SpO2 { get; set; } = new();
}

public struct TimestampedVital
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
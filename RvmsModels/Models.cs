namespace RvmsModels;

using Newtonsoft.Json;

public enum Role
{
    PATIENT,
    ADMIN
}

public class User
{
    [JsonProperty(PropertyName = "email")] public string Id => Email;

    [JsonProperty(PropertyName = "name")] public string Name { get; set; }
    [JsonProperty(PropertyName = "email")] public string Email { get; set; }

    [JsonProperty(PropertyName = "role")] public Role Role { get; set; }
}

public class Vitals
{
    [JsonProperty(PropertyName = "ECG")] public IEnumerable<TimestampedVital> Ecg { get; set; }
    [JsonProperty(PropertyName = "HR")] public IEnumerable<TimestampedVital> HeartRate { get; set; }
    [JsonProperty(PropertyName = "SpO2")] public IEnumerable<TimestampedVital> SpO2 { get; set; }
}

public struct TimestampedVital
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
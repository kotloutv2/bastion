using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace RvmsModels;

public class CosmosDbService
{
    private readonly ILogger<CosmosDbService> _logger;
    private readonly Container _container;

    public CosmosDbService(CosmosClient client, string databaseName, string containerName)
    {
        _logger = new Logger<CosmosDbService>(new LoggerFactory());
        _container = client.GetContainer(databaseName, containerName);
        var databaseResponse = client.GetDatabase(databaseName);
        _container = databaseResponse.GetContainer(containerName);
    }

    public async Task<ItemResponse<Patient>> RegisterPatient(RegisterUser registerUser)
    {
        var salt = Security.GenerateSalt();
        Patient patientToRegister = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = registerUser.Name,
            Email = registerUser.Email,
            Role = Role.PATIENT,
            Salt = salt,
            Password = Security.HashPassword(salt, registerUser.Password)
        };

        return await _container.CreateItemAsync(patientToRegister);
    }

    public async Task<Patient?> GetPatientByEmail(string email)
    {
        var getPatientByEmailQuery =
            new QueryDefinition("SELECT * from c WHERE c.Email = @email").WithParameter("@email", email);
        var queryResults = _container.GetItemQueryIterator<Patient>(getPatientByEmailQuery);

        // Get first user with the given email (there should only be one)
        try
        {
            while (queryResults.HasMoreResults)
            {
                var users = await queryResults.ReadNextAsync();
                return users.First();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not find user with email {}", email);
        }

        return null;
    }
}
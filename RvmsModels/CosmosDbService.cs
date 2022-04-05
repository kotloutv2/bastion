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

    public async Task<Patient> RegisterPatient(RegisterUser registerUser)
    {
        var salt = Security.GenerateSalt();
        Patient patientToRegister = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = registerUser.Name,
            Email = registerUser.Email,
            Salt = salt,
            Password = Security.HashPassword(salt, registerUser.Password)
        };

        try
        {
            return await _container.CreateItemAsync(patientToRegister);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during patient registration");
            throw;
        }
    }

    public async Task<HealthcarePractitioner> RegisterHealthcarePractitioner(RegisterUser registerHcp)
    {
        var salt = Security.GenerateSalt();
        HealthcarePractitioner hcpToRegister = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = registerHcp.Name,
            Email = registerHcp.Email,
            Salt = salt,
            Password = Security.HashPassword(salt, registerHcp.Password)
        };

        try
        {
            return await _container.CreateItemAsync(hcpToRegister);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during healthcare practitioner registration");
            throw;
        }
    }

    public async Task<Patient?> GetPatientByEmail(string email)
    {
        return await GetUser<Patient>(email, Role.PATIENT);
    }

    public async Task<HealthcarePractitioner?> GetHealthcarePractitionerByEmail(string email)
    {
        return await GetUser<HealthcarePractitioner>(email, Role.ADMIN);
    }

    public async Task<Patient?> AddVitals(Patient patient, Vitals vitals)
    {
        patient.Vitals.Ppg.AddRange(vitals.Ppg);
        patient.Vitals.SkinTemperature1.AddRange(vitals.SkinTemperature1);
        patient.Vitals.SkinTemperature2.AddRange(vitals.SkinTemperature2);
        var vitalsUpdate = new List<PatchOperation>
        {
            PatchOperation.Replace("/Vitals", patient.Vitals)
        };
        try
        {
            var updatedPatient = await _container.PatchItemAsync<Patient>(
                patient.Id,
                new PartitionKey((double) Role.PATIENT),
                vitalsUpdate
            );
            return updatedPatient;
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, "Error while trying to put new vitals for user {email}", patient.Email);
            return null;
        }
    }

    private async Task<T?> GetUser<T>(string email, Role role) where T : IUser
    {
        var getPatientByEmailQuery =
            new QueryDefinition("SELECT * from c WHERE c.Email = @email and c.Role = @role")
                .WithParameter("@email", email)
                .WithParameter("@role", role);
        var queryResults = _container.GetItemQueryIterator<T>(getPatientByEmailQuery);

        // Get first user with the given email (there should only be one)
        try
        {
            var users = await queryResults.ReadNextAsync();
            return users.First();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not find user with email {} and role {}", email, role.ToString());
        }

        return default;
    }

    public async Task<HealthcarePractitioner?> AddPatientToHcp(HealthcarePractitioner hcp, string newPatientEmail)
    {
        var addNewPatient = new List<PatchOperation>
        {
            PatchOperation.Add("/Patients/-", newPatientEmail),
        };
        try
        {
            var updatedHcp = await _container.PatchItemAsync<HealthcarePractitioner>(
                hcp.Id,
                new PartitionKey((double) Role.ADMIN),
                addNewPatient
            );
            return updatedHcp;
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, "Error while trying to add patient {} for healthcare practitioner {}", newPatientEmail,
                hcp.Email);
            return null;
        }
    }

    public async Task<HealthcarePractitioner?> RemovePatientFromHcp(HealthcarePractitioner hcp, string patientEmail)
    {
        var prevNumPatients = hcp.Patients.Count();
        var modifiedPatients = hcp.Patients.Except(new[] {patientEmail}).ToList();

        // skip the patch if patient was never registered with HCP
        if (prevNumPatients == modifiedPatients.Count)
        {
            return hcp;
        }

        var removePatientOperation = new List<PatchOperation>
        {
            PatchOperation.Replace("/Patients", modifiedPatients),
        };
        try
        {
            var updatedHcp = await _container.PatchItemAsync<HealthcarePractitioner>(
                hcp.Id,
                new PartitionKey((double) Role.ADMIN),
                removePatientOperation
            );
            return updatedHcp;
        }
        catch (CosmosException ce)
        {
            _logger.LogError(ce, "Error while trying to remove patient {} from healthcare practitioner {}",
                patientEmail, hcp.Email);
            return null;
        }
    }
}
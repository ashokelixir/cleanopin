using System.Net.Http.Json;
using System.Text.Json;

namespace CleanArchTemplate.Examples;

/// <summary>
/// Example demonstrating resilient user creation
/// This example shows how to interact with the resilient user creation endpoint
/// </summary>
public class ResilientUserCreationExample
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ResilientUserCreationExample(string baseUrl = "https://localhost:7001")
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Demonstrates successful user creation with resilience patterns
    /// </summary>
    public async Task DemonstrateSuccessfulUserCreation()
    {
        Console.WriteLine("=== Resilient User Creation Example ===\n");

        var createUserRequest = new
        {
            Email = "resilient.user@example.com",
            FirstName = "Resilient",
            LastName = "User",
            Password = "SecurePassword123!"
        };

        try
        {
            Console.WriteLine("Creating user with resilience patterns...");
            Console.WriteLine($"Request: {JsonSerializer.Serialize(createUserRequest, new JsonSerializerOptions { WriteIndented = true })}");

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users", createUserRequest);

            Console.WriteLine($"\nResponse Status: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Body: {responseContent}");
                Console.WriteLine("\n✅ User created successfully with resilience patterns!");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error Response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates conflict handling when creating duplicate users
    /// </summary>
    public async Task DemonstrateConflictHandling()
    {
        Console.WriteLine("\n=== Conflict Handling Example ===\n");

        var duplicateUserRequest = new
        {
            Email = "duplicate@example.com",
            FirstName = "Duplicate",
            LastName = "User",
            Password = "SecurePassword123!"
        };

        try
        {
            // Create user first time
            Console.WriteLine("Creating user for the first time...");
            var firstResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users", duplicateUserRequest);
            Console.WriteLine($"First creation status: {firstResponse.StatusCode}");

            // Try to create the same user again
            Console.WriteLine("\nAttempting to create the same user again...");
            var secondResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users", duplicateUserRequest);
            Console.WriteLine($"Second creation status: {secondResponse.StatusCode}");

            if (secondResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var errorContent = await secondResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Conflict response: {errorContent}");
                Console.WriteLine("\n✅ Conflict handling working correctly!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates validation error handling
    /// </summary>
    public async Task DemonstrateValidationHandling()
    {
        Console.WriteLine("\n=== Validation Error Handling Example ===\n");

        var invalidUserRequest = new
        {
            Email = "invalid-email-format",
            FirstName = "",
            LastName = "",
            Password = "weak"
        };

        try
        {
            Console.WriteLine("Creating user with invalid data...");
            Console.WriteLine($"Request: {JsonSerializer.Serialize(invalidUserRequest, new JsonSerializerOptions { WriteIndented = true })}");

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users", invalidUserRequest);
            Console.WriteLine($"\nResponse Status: {response.StatusCode}");

            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Validation error response: {errorContent}");
                Console.WriteLine("\n✅ Validation error handling working correctly!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates retrieving a created user
    /// </summary>
    public async Task DemonstrateUserRetrieval()
    {
        Console.WriteLine("\n=== User Retrieval Example ===\n");

        var createUserRequest = new
        {
            Email = "retrieve.test@example.com",
            FirstName = "Retrieve",
            LastName = "Test",
            Password = "SecurePassword123!"
        };

        try
        {
            // Create user first
            Console.WriteLine("Creating user for retrieval test...");
            var createResponse = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/users", createUserRequest);

            if (createResponse.IsSuccessStatusCode)
            {
                var createdUserJson = await createResponse.Content.ReadAsStringAsync();
                var createdUser = JsonSerializer.Deserialize<JsonElement>(createdUserJson);
                var userId = createdUser.GetProperty("id").GetString();

                Console.WriteLine($"User created with ID: {userId}");

                // Retrieve the user
                Console.WriteLine("\nRetrieving user by ID...");
                var getResponse = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
                Console.WriteLine($"Retrieval status: {getResponse.StatusCode}");

                if (getResponse.IsSuccessStatusCode)
                {
                    var retrievedUserJson = await getResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Retrieved user: {retrievedUserJson}");
                    Console.WriteLine("\n✅ User retrieval working correctly!");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs all demonstration examples
    /// </summary>
    public async Task RunAllExamples()
    {
        await DemonstrateSuccessfulUserCreation();
        await DemonstrateConflictHandling();
        await DemonstrateValidationHandling();
        await DemonstrateUserRetrieval();

        Console.WriteLine("\n=== All Examples Completed ===");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Program entry point for running the examples
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var example = new ResilientUserCreationExample();

        try
        {
            await example.RunAllExamples();
        }
        finally
        {
            example.Dispose();
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
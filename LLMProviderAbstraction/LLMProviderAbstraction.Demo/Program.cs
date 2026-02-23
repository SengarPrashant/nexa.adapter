using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Session;
using System.Text.Json;

namespace LLMProviderAbstraction.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LLMProviderAbstraction Demo ===\n");

        // Demo 3: Bedrock Provider - Context-based Analysis (Default)
        // await RunBedrockProviderContextAnalysisDemo();
        
        // Uncomment other demos as needed:
        // Demo 1: Local Provider (Ollama) - Context-based Analysis
        await RunLocalProviderContextAnalysisDemo();
        
        // Demo 2: Local Provider (Ollama) - Chatbot Session
        await RunLocalProviderChatbotDemo();
        
        // Demo 4: Bedrock Provider - Chatbot Session
        // await RunBedrockProviderChatbotDemo();

        Console.WriteLine("\n=== Demo Complete ===");
    }

    /// <summary>
    /// Helper method to get Bedrock configuration from multiple sources
    /// Priority: 1) Environment Variables, 2) appsettings.json, 3) Hardcoded fallback
    /// </summary>
    static BedrockConfig GetBedrockConfiguration()
    {
        // Try environment variables first (most secure)
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var region = Environment.GetEnvironmentVariable("AWS_REGION");

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            Console.WriteLine("✓ Using AWS credentials from environment variables\n");
            return new BedrockConfig
            {
                AccessKey = accessKey,
                SecretKey = secretKey,
                Region = region ?? "us-east-1"
            };
        }

        // Try appsettings.json second
        try
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (settings?.Aws != null && 
                    !string.IsNullOrEmpty(settings.Aws.AccessKeyId) && 
                    !string.IsNullOrEmpty(settings.Aws.SecretAccessKey))
                {
                    Console.WriteLine("✓ Using AWS credentials from appsettings.json\n");
                    return new BedrockConfig
                    {
                        AccessKey = settings.Aws.AccessKeyId,
                        SecretKey = settings.Aws.SecretAccessKey,
                        Region = settings.Aws.Region ?? "us-east-1"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: Could not read appsettings.json: {ex.Message}");
        }

        // Fallback to empty values with warning
        Console.WriteLine("⚠ WARNING: No AWS credentials found!");
        Console.WriteLine("Please configure credentials using one of these methods:");
        Console.WriteLine("  1. Environment variables: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION");
        Console.WriteLine("  2. appsettings.json file (see appsettings.example.json)");
        Console.WriteLine("  3. Hardcode values in Program.cs (not recommended for production)\n");

        return new BedrockConfig
        {
            AccessKey = "",
            SecretKey = "",
            Region = "us-east-1"
        };
    }

    // Helper classes for configuration
    class BedrockConfig
    {
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string Region { get; set; } = "us-east-1";
    }

    class AppSettings
    {
        public AwsSettings? Aws { get; set; }
    }

    class AwsSettings
    {
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
        public string? Region { get; set; }
    }

    /// <summary>
    /// Demo 1: Using Local Provider (Ollama) for context-based analysis
    /// This demonstrates analyzing a piece of text with a specific question
    /// </summary>
    static async Task RunLocalProviderContextAnalysisDemo()
    {
        Console.WriteLine("--- Demo 1: Local Provider - Context Analysis ---\n");

        // Step 1: Configure the Local Provider
        // Make sure Ollama is running locally on http://localhost:11434
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            Endpoint = "http://localhost:11434",  // Default Ollama endpoint
            ModelIdentifier = "tinyllama",         // Lightweight model (3.8GB) - fast and efficient for personal PCs
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 2: Validate configuration
        var validationResult = config.Validate();
        if (!validationResult.Success)
        {
            Console.WriteLine("Configuration validation failed:");
            foreach (var error in validationResult.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        // Step 3: Create the provider instance
        using var httpClient = new HttpClient();
        ILLMProvider provider = new LocalProvider(config, httpClient);

        // Step 4: Validate provider connectivity
        Console.WriteLine("Validating provider connectivity...");
        var providerValidation = await provider.ValidateAsync();
        if (!providerValidation.Success)
        {
            Console.WriteLine("Provider validation failed:");
            foreach (var error in providerValidation.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }
        Console.WriteLine("Provider is ready!\n");

        // Step 5: Perform context-based analysis
        var context = @"
            The quick brown fox jumps over the lazy dog. 
            This sentence is famous for containing every letter of the English alphabet.
            It is commonly used for testing fonts and keyboards.
        ";
        
        var prompt = "What makes this sentence special?";

        Console.WriteLine($"Context: {context.Trim()}");
        Console.WriteLine($"Prompt: {prompt}\n");
        Console.WriteLine("Sending request to LLM...\n");

        var response = await provider.AnalyzeAsync(context, prompt);

        // Step 6: Handle the response
        if (response.Success)
        {
            Console.WriteLine("Response:");
            Console.WriteLine(response.Content);
            Console.WriteLine("\nMetadata:");
            foreach (var kvp in response.Metadata)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            Console.WriteLine("Error occurred:");
            Console.WriteLine($"  Type: {response.Error?.Type}");
            Console.WriteLine($"  Message: {response.Error?.Message}");
        }
    }

    /// <summary>
    /// Demo 2: Using Local Provider (Ollama) for a chatbot session
    /// This demonstrates maintaining conversation context across multiple messages
    /// </summary>
    static async Task RunLocalProviderChatbotDemo()
    {
        Console.WriteLine("--- Demo 2: Local Provider - Chatbot Session ---\n");

        // Step 1: Configure the Local Provider
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            Endpoint = "http://localhost:11434",
            ModelIdentifier = "tinyllama",         
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 2: Create provider and session manager
        using var httpClient = new HttpClient();
        ILLMProvider provider = new LocalProvider(config, httpClient);
        ISessionManager sessionManager = new SessionManager();

        // Step 3: Create a new conversation session
        var session = sessionManager.CreateSession();
        Console.WriteLine($"Created session: {session.SessionId}\n");

        // Step 4: Send multiple messages in the same session
        var messages = new[]
        {
            "My bank balance is 100 dollars",
            "What is my bank balance?",
            "What will my total bank balance be if I deposit 10 dollars?"
        };

        foreach (var message in messages)
        {
            Console.WriteLine($"User: {message}");
            
            var response = await provider.SendMessageAsync(session, message);
            
            if (response.Success)
            {
                Console.WriteLine($"Assistant: {response.Content}\n");
            }
            else
            {
                Console.WriteLine($"Error: {response.Error?.Message}\n");
                break;
            }
        }

        // Step 5: Display session history
        Console.WriteLine("--- Session History ---");
        var history = sessionManager.GetSessionHistory(session.SessionId);
        foreach (var msg in history)
        {
            Console.WriteLine($"{msg.Role}: {msg.Content}");
        }
    }

    /// <summary>
    /// Demo 3: Using Bedrock Provider for context-based analysis
    /// This demonstrates using AWS Bedrock with Claude or other models
    /// </summary>
    static async Task RunBedrockProviderContextAnalysisDemo()
    {
        Console.WriteLine("--- Demo 3: Bedrock Provider - Context Analysis ---\n");

        // Step 1: Get AWS credentials from secure sources
        var bedrockConfig = GetBedrockConfiguration();

        // Step 2: Configure the Bedrock Provider
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            AccessKey = bedrockConfig.AccessKey,
            SecretKey = bedrockConfig.SecretKey,
            Region = bedrockConfig.Region,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",  // Claude 3 Sonnet
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 2: Validate configuration
        var validationResult = config.Validate();
        if (!validationResult.Success)
        {
            Console.WriteLine("Configuration validation failed:");
            foreach (var error in validationResult.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }

        // Step 4: Create the provider instance
        ILLMProvider provider = new BedrockProvider(config);

        // Step 5: Validate provider connectivity
        Console.WriteLine("Validating provider connectivity...");
        var providerValidation = await provider.ValidateAsync();
        if (!providerValidation.Success)
        {
            Console.WriteLine("Provider validation failed:");
            foreach (var error in providerValidation.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
            return;
        }
        Console.WriteLine("Provider is ready!\n");

        // Step 5: Perform context-based analysis
        var context = @"
            Product: SmartWatch Pro
            Price: $299
            Features: Heart rate monitor, GPS, 7-day battery life, waterproof
            Customer Rating: 4.5/5 stars
        ";
        
        var prompt = "Summarize the key selling points of this product.";

        Console.WriteLine($"Context: {context.Trim()}");
        Console.WriteLine($"Prompt: {prompt}\n");
        Console.WriteLine("Sending request to Bedrock...\n");

        var response = await provider.AnalyzeAsync(context, prompt);

        // Step 6: Handle the response
        if (response.Success)
        {
            Console.WriteLine("Response:");
            Console.WriteLine(response.Content);
            Console.WriteLine("\nMetadata:");
            foreach (var kvp in response.Metadata)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
        else
        {
            Console.WriteLine("Error occurred:");
            Console.WriteLine($"  Type: {response.Error?.Type}");
            Console.WriteLine($"  Message: {response.Error?.Message}");
        }
    }

    /// <summary>
    /// Demo 4: Using Bedrock Provider for a chatbot session
    /// This demonstrates maintaining conversation context with AWS Bedrock
    /// </summary>
    static async Task RunBedrockProviderChatbotDemo()
    {
        Console.WriteLine("--- Demo 4: Bedrock Provider - Chatbot Session ---\n");

        // Step 1: Get AWS credentials from secure sources
        var bedrockConfig = GetBedrockConfiguration();

        // Step 2: Configure the Bedrock Provider
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            AccessKey = bedrockConfig.AccessKey,
            SecretKey = bedrockConfig.SecretKey,
            Region = bedrockConfig.Region,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 3: Create provider and session manager
        ILLMProvider provider = new BedrockProvider(config);
        ISessionManager sessionManager = new SessionManager();

        // Step 3: Create a new conversation session
        var session = sessionManager.CreateSession();
        Console.WriteLine($"Created session: {session.SessionId}\n");

        // Step 4: Send multiple messages in the same session
        var messages = new[]
        {
            "I'm planning a trip to Japan. What are the must-visit cities?",
            "How many days should I spend in Tokyo?",
            "What's the best time of year to visit?"
        };

        foreach (var message in messages)
        {
            Console.WriteLine($"User: {message}");
            
            var response = await provider.SendMessageAsync(session, message);
            
            if (response.Success)
            {
                Console.WriteLine($"Assistant: {response.Content}\n");
            }
            else
            {
                Console.WriteLine($"Error: {response.Error?.Message}\n");
                break;
            }
        }

        // Step 5: Display session history
        Console.WriteLine("--- Session History ---");
        var history = sessionManager.GetSessionHistory(session.SessionId);
        foreach (var msg in history)
        {
            Console.WriteLine($"{msg.Role}: {msg.Content}");
        }
    }
}


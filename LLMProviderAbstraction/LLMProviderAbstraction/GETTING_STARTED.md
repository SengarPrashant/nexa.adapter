# Getting Started with LLMProviderAbstraction

This guide will walk you through integrating the LLMProviderAbstraction library into your C# application from scratch.

## Prerequisites

Before you begin, ensure you have:

- **.NET 6.0 or later** installed
- **Visual Studio 2022** or **VS Code** with C# extension
- **Basic C# knowledge** (async/await, interfaces)

### For AWS Bedrock Provider:
- AWS account with Bedrock access
- AWS Access Key ID and Secret Access Key
- IAM permissions for Bedrock

### For Local Provider:
- Ollama installed and running
- At least one model downloaded

## Installation

### Step 1: Add the Library to Your Project

```bash
# Navigate to your project directory
cd YourProject

# Add reference to the library
dotnet add reference path/to/LLMProviderAbstraction/LLMProviderAbstraction.csproj

# Or add to your .csproj file manually:
```

```xml
<ItemGroup>
  <ProjectReference Include="..\LLMProviderAbstraction\LLMProviderAbstraction.csproj" />
</ItemGroup>
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

## Your First Integration

Let's build a simple console application that uses the library.

### Option A: Local Provider (Easiest to Start)

#### 1. Install and Setup Ollama

```bash
# macOS
brew install ollama

# Linux
curl -fsSL https://ollama.ai/install.sh | sh

# Windows: Download from https://ollama.ai
```

#### 2. Start Ollama and Pull a Model

```bash
# Start Ollama service
ollama serve

# In a new terminal, pull a lightweight model
ollama pull tinyllama
```

#### 3. Create Your First Program

Create a new file `Program.cs`:

```csharp
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;

namespace MyFirstLLMApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("My First LLM Application\n");

        // Step 1: Configure the provider
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Local,
            Endpoint = "http://localhost:11434",
            ModelIdentifier = "tinyllama",
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 2: Validate configuration
        var validation = config.Validate();
        if (!validation.Success)
        {
            Console.WriteLine("Configuration errors:");
            foreach (var error in validation.Errors)
                Console.WriteLine($"  - {error}");
            return;
        }

        // Step 3: Create the provider
        using var httpClient = new HttpClient();
        ILLMProvider provider = new LocalProvider(config, httpClient);

        // Step 4: Validate connectivity
        Console.WriteLine("Connecting to LLM...");
        var providerValidation = await provider.ValidateAsync();
        if (!providerValidation.Success)
        {
            Console.WriteLine("Connection failed:");
            foreach (var error in providerValidation.Errors)
                Console.WriteLine($"  - {error}");
            return;
        }
        Console.WriteLine("Connected!\n");

        // Step 5: Ask a question
        var context = "The Eiffel Tower is located in Paris, France. It was built in 1889.";
        var prompt = "When was the Eiffel Tower built?";

        Console.WriteLine($"Context: {context}");
        Console.WriteLine($"Question: {prompt}\n");

        var response = await provider.AnalyzeAsync(context, prompt);

        // Step 6: Display the result
        if (response.Success)
        {
            Console.WriteLine($"Answer: {response.Content}");
        }
        else
        {
            Console.WriteLine($"Error: {response.Error?.Message}");
        }
    }
}
```

#### 4. Run Your Application

```bash
dotnet run
```

**Expected Output:**
```
My First LLM Application

Connecting to LLM...
Connected!

Context: The Eiffel Tower is located in Paris, France. It was built in 1889.
Question: When was the Eiffel Tower built?

Answer: The Eiffel Tower was built in 1889.
```

Congratulations! You've successfully integrated the library! 🎉

### Option B: AWS Bedrock Provider

#### 1. Setup AWS Credentials

First, get your AWS credentials:

1. Log into AWS Console
2. Go to IAM → Users → Your User → Security Credentials
3. Create Access Key → Save the Access Key ID and Secret Access Key

#### 2. Enable Bedrock Model Access

1. Go to AWS Bedrock Console
2. Click "Model access" in the left sidebar
3. Click "Request model access"
4. Select "Claude 3 Sonnet" (or another model)
5. Submit request (usually approved instantly)

#### 3. Set Environment Variables

```bash
# Linux/macOS
export AWS_ACCESS_KEY_ID="your_access_key_id"
export AWS_SECRET_ACCESS_KEY="your_secret_access_key"
export AWS_REGION="us-east-1"

# Windows PowerShell
$env:AWS_ACCESS_KEY_ID="your_access_key_id"
$env:AWS_SECRET_ACCESS_KEY="your_secret_access_key"
$env:AWS_REGION="us-east-1"
```

#### 4. Create Your Program

```csharp
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;

namespace MyFirstLLMApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("My First Bedrock Application\n");

        // Step 1: Get credentials from environment
        var accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            Console.WriteLine("Error: AWS credentials not found in environment variables");
            return;
        }

        // Step 2: Configure the provider
        var config = new LLMConfiguration
        {
            ProviderType = ProviderType.Bedrock,
            ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
            AccessKey = accessKey,
            SecretKey = secretKey,
            Region = region,
            TimeoutSeconds = 60,
            MaxRetries = 3
        };

        // Step 3: Validate configuration
        var validation = config.Validate();
        if (!validation.Success)
        {
            Console.WriteLine("Configuration errors:");
            foreach (var error in validation.Errors)
                Console.WriteLine($"  - {error}");
            return;
        }

        // Step 4: Create the provider
        ILLMProvider provider = new BedrockProvider(config);

        // Step 5: Validate connectivity
        Console.WriteLine("Connecting to AWS Bedrock...");
        var providerValidation = await provider.ValidateAsync();
        if (!providerValidation.Success)
        {
            Console.WriteLine("Connection failed:");
            foreach (var error in providerValidation.Errors)
                Console.WriteLine($"  - {error}");
            return;
        }
        Console.WriteLine("Connected!\n");

        // Step 6: Ask a question
        var context = "The Eiffel Tower is located in Paris, France. It was built in 1889.";
        var prompt = "When was the Eiffel Tower built?";

        Console.WriteLine($"Context: {context}");
        Console.WriteLine($"Question: {prompt}\n");

        var response = await provider.AnalyzeAsync(context, prompt);

        // Step 7: Display the result
        if (response.Success)
        {
            Console.WriteLine($"Answer: {response.Content}");
        }
        else
        {
            Console.WriteLine($"Error: {response.Error?.Message}");
        }
    }
}
```

#### 5. Run Your Application

```bash
dotnet run
```

## Configuration Options Explained

### Required for All Providers

```csharp
var config = new LLMConfiguration
{
    // Required: Type of provider
    ProviderType = ProviderType.Local,  // or ProviderType.Bedrock
    
    // Required: Model to use
    ModelIdentifier = "tinyllama",
    
    // Optional: Request timeout (default: 30 seconds)
    TimeoutSeconds = 60,
    
    // Optional: Retry attempts for transient errors (default: 3)
    MaxRetries = 3
};
```

### Bedrock-Specific Options

```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Bedrock,
    
    // Required for Bedrock
    AccessKey = "AKIAIOSFODNN7EXAMPLE",
    SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    
    // Optional: AWS region (default: us-east-1)
    Region = "us-east-1",
    
    // Model identifier format: provider.model-version
    ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0"
};
```

### Local Provider Options

```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    
    // Required for Local: Ollama endpoint
    Endpoint = "http://localhost:11434",
    
    // Model name from Ollama
    ModelIdentifier = "tinyllama"
};
```

## Testing Your Integration

### Test 1: Configuration Validation

```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama"
};

var validation = config.Validate();
Console.WriteLine($"Configuration valid: {validation.Success}");

if (!validation.Success)
{
    foreach (var error in validation.Errors)
        Console.WriteLine($"  - {error}");
}
```

### Test 2: Provider Connectivity

```csharp
using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);

var validation = await provider.ValidateAsync();
Console.WriteLine($"Provider connected: {validation.Success}");

if (!validation.Success)
{
    foreach (var error in validation.Errors)
        Console.WriteLine($"  - {error}");
}
```

### Test 3: Simple Query

```csharp
var response = await provider.AnalyzeAsync(
    "2 + 2 = 4",
    "What is the result of the calculation?"
);

Console.WriteLine($"Success: {response.Success}");
Console.WriteLine($"Content: {response.Content}");
```

### Test 4: Session Management

```csharp
using LLMProviderAbstraction.Session;

var sessionManager = new SessionManager();
var session = sessionManager.CreateSession();

var response1 = await provider.SendMessageAsync(session, "Hello!");
Console.WriteLine($"Response 1: {response1.Content}");

var response2 = await provider.SendMessageAsync(session, "What did I just say?");
Console.WriteLine($"Response 2: {response2.Content}");

// Should remember "Hello!"
```

## Common Integration Patterns

### Pattern 1: Dependency Injection

```csharp
// Startup.cs or Program.cs
services.AddSingleton<ISessionManager, SessionManager>();

services.AddHttpClient<ILLMProvider, LocalProvider>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var llmConfig = new LLMConfiguration
    {
        ProviderType = ProviderType.Local,
        Endpoint = config["LLM:Endpoint"],
        ModelIdentifier = config["LLM:Model"]
    };
    
    return new LocalProvider(llmConfig, client);
});
```

### Pattern 2: Configuration from appsettings.json

```json
{
  "LLM": {
    "ProviderType": "Local",
    "Endpoint": "http://localhost:11434",
    "ModelIdentifier": "tinyllama",
    "TimeoutSeconds": 60,
    "MaxRetries": 3
  }
}
```

```csharp
var llmSettings = configuration.GetSection("LLM");
var config = new LLMConfiguration
{
    ProviderType = Enum.Parse<ProviderType>(llmSettings["ProviderType"]),
    Endpoint = llmSettings["Endpoint"],
    ModelIdentifier = llmSettings["ModelIdentifier"],
    TimeoutSeconds = int.Parse(llmSettings["TimeoutSeconds"]),
    MaxRetries = int.Parse(llmSettings["MaxRetries"])
};
```

### Pattern 3: Factory Pattern

```csharp
public interface ILLMProviderFactory
{
    ILLMProvider CreateProvider(ProviderType type);
}

public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public LLMProviderFactory(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public ILLMProvider CreateProvider(ProviderType type)
    {
        var config = new LLMConfiguration
        {
            ProviderType = type,
            // Load from configuration...
        };

        return type switch
        {
            ProviderType.Local => new LocalProvider(
                config,
                _httpClientFactory.CreateClient()
            ),
            ProviderType.Bedrock => new BedrockProvider(config),
            _ => throw new ArgumentException($"Unknown provider type: {type}")
        };
    }
}
```

## Next Steps

Now that you have the basics working:

1. **Explore Different Use Cases**: Check out [USAGE_GUIDE.md](USAGE_GUIDE.md) for detailed examples
2. **Learn the Full API**: Read [API_REFERENCE.md](API_REFERENCE.md) for complete documentation
3. **Try Different Models**: Experiment with various models for different tasks
4. **Build Something**: Create a chatbot, document analyzer, or code assistant
5. **Handle Errors**: Implement robust error handling for production use

## Troubleshooting

### "Configuration validation failed"

Check that all required fields are set:
- Local: `Endpoint` and `ModelIdentifier`
- Bedrock: `AccessKey`, `SecretKey`, and `ModelIdentifier`

### "Provider validation failed"

**For Local Provider:**
- Ensure Ollama is running: `ollama serve`
- Check endpoint is accessible: `curl http://localhost:11434/api/tags`
- Verify model is downloaded: `ollama list`

**For Bedrock:**
- Verify AWS credentials are correct
- Check IAM permissions
- Ensure model access is enabled in Bedrock console

### "Timeout" errors

Increase the timeout:
```csharp
config.TimeoutSeconds = 120;  // 2 minutes
```

### "Model not found"

**For Local Provider:**
```bash
ollama pull tinyllama
```

**For Bedrock:**
- Go to Bedrock Console → Model access
- Request access to the model

## Getting Help

- Review the [README.md](README.md) for overview and concepts
- Check [USAGE_GUIDE.md](USAGE_GUIDE.md) for detailed patterns
- Consult [API_REFERENCE.md](API_REFERENCE.md) for API details
- Look at the demo project in `LLMProviderAbstraction.Demo/`

## Security Best Practices

1. **Never hardcode credentials** in source code
2. **Use environment variables** for sensitive data
3. **Add appsettings.json to .gitignore** if storing credentials there
4. **Rotate AWS keys regularly**
5. **Use IAM roles** in production environments when possible
6. **Validate all inputs** before sending to LLM
7. **Implement rate limiting** to prevent abuse

Happy coding! 🚀

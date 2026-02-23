# LLMProviderAbstraction

A flexible, provider-agnostic C# library for integrating Large Language Models (LLMs) into your applications. Switch seamlessly between AWS Bedrock and local LLM providers (like Ollama) with a unified API.

## Features

- **Provider Agnostic**: Single API for multiple LLM providers (AWS Bedrock, Local/Ollama)
- **Session Management**: Built-in conversation history tracking for chatbot scenarios
- **Context Analysis**: Simple API for one-shot question-answering with context
- **Type-Safe**: Strongly-typed models and interfaces
- **Error Handling**: Comprehensive error types and validation
- **Async/Await**: Modern async patterns throughout
- **Extensible**: Easy to add new providers
- **Production Ready**: Includes retry logic, timeouts, and validation

## Supported Providers

| Provider | Description | Use Case |
|----------|-------------|----------|
| **AWS Bedrock** | Cloud-based LLMs (Claude, Llama, etc.) | Production apps, powerful models, scalable |
| **Local (Ollama)** | Self-hosted LLMs on your machine | Development, privacy, offline, cost-effective |

## Quick Start

### Installation

```bash
# Clone or add the library to your solution
dotnet add reference path/to/LLMProviderAbstraction/LLMProviderAbstraction.csproj
```

### Basic Usage - Context Analysis

```csharp
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Providers;

// Configure the provider
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama",
    TimeoutSeconds = 60
};

// Create provider
using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);

// Analyze context
var context = "The quick brown fox jumps over the lazy dog.";
var prompt = "What makes this sentence special?";
var response = await provider.AnalyzeAsync(context, prompt);

if (response.Success)
{
    Console.WriteLine(response.Content);
}
```

### Basic Usage - Chatbot Session

```csharp
using LLMProviderAbstraction.Session;

// Create session manager
ISessionManager sessionManager = new SessionManager();
var session = sessionManager.CreateSession();

// Send messages
var response1 = await provider.SendMessageAsync(session, "My name is Alice");
var response2 = await provider.SendMessageAsync(session, "What's my name?");

// The LLM remembers: "Your name is Alice"
Console.WriteLine(response2.Content);
```

## Core Concepts

### 1. Provider Types

The library supports two provider types:

- **Bedrock**: AWS cloud-based LLMs requiring AWS credentials
- **Local**: Self-hosted LLMs (Ollama) running on your machine

### 2. Two Usage Patterns

#### Context-Based Analysis
Use `AnalyzeAsync()` for single-shot questions with context:
- Document analysis
- Text summarization
- Q&A without conversation history
- One-time queries

#### Session-Based Chat
Use `SendMessageAsync()` with sessions for conversations:
- Chatbots
- Multi-turn dialogues
- Context-aware conversations
- Interactive assistants

### 3. Configuration

All providers use `LLMConfiguration`:

```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Bedrock,  // or ProviderType.Local
    ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
    AccessKey = "your-aws-key",           // Bedrock only
    SecretKey = "your-aws-secret",        // Bedrock only
    Region = "us-east-1",                 // Bedrock only
    Endpoint = "http://localhost:11434",  // Local only
    TimeoutSeconds = 60,
    MaxRetries = 3
};

// Always validate
var validation = config.Validate();
if (!validation.Success)
{
    foreach (var error in validation.Errors)
        Console.WriteLine(error);
}
```

### 4. Response Handling

All operations return `LLMResponse`:

```csharp
var response = await provider.AnalyzeAsync(context, prompt);

if (response.Success)
{
    // Success path
    Console.WriteLine(response.Content);
    
    // Optional metadata
    foreach (var kvp in response.Metadata)
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
else
{
    // Error path
    Console.WriteLine($"Error: {response.Error.Type}");
    Console.WriteLine($"Message: {response.Error.Message}");
    
    if (response.Error.InnerException != null)
        Console.WriteLine($"Details: {response.Error.InnerException}");
}
```

## Common Use Cases

### Use Case 1: Document Q&A System

```csharp
// Load document
var document = File.ReadAllText("terms-of-service.txt");

// Ask questions
var questions = new[]
{
    "What is the refund policy?",
    "How long is the trial period?",
    "Can I cancel anytime?"
};

foreach (var question in questions)
{
    var response = await provider.AnalyzeAsync(document, question);
    Console.WriteLine($"Q: {question}");
    Console.WriteLine($"A: {response.Content}\n");
}
```

### Use Case 2: Customer Support Chatbot

```csharp
var sessionManager = new SessionManager();
var session = sessionManager.CreateSession();

// Add system context
session.AddMessage(new Message(
    "You are a helpful customer support agent for Acme Corp.",
    MessageRole.System
));

// Handle customer messages
while (true)
{
    Console.Write("Customer: ");
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input)) break;
    
    var response = await provider.SendMessageAsync(session, input);
    Console.WriteLine($"Agent: {response.Content}\n");
}
```

### Use Case 3: Code Review Assistant

```csharp
var code = @"
public void ProcessData(List<string> items)
{
    for (int i = 0; i < items.Count; i++)
    {
        Console.WriteLine(items[i]);
    }
}
";

var prompts = new[]
{
    "Review this code for potential issues",
    "Suggest performance improvements",
    "Rewrite using modern C# patterns"
};

foreach (var prompt in prompts)
{
    var response = await provider.AnalyzeAsync(code, prompt);
    Console.WriteLine($"\n{prompt}:");
    Console.WriteLine(response.Content);
}
```

### Use Case 4: Multi-Provider Fallback

```csharp
async Task<LLMResponse> GetResponseWithFallback(string context, string prompt)
{
    // Try Bedrock first
    var bedrockConfig = new LLMConfiguration
    {
        ProviderType = ProviderType.Bedrock,
        ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
        AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
        SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
        Region = "us-east-1"
    };
    
    ILLMProvider provider = new BedrockProvider(bedrockConfig);
    var response = await provider.AnalyzeAsync(context, prompt);
    
    if (response.Success)
        return response;
    
    // Fallback to local
    Console.WriteLine("Bedrock failed, trying local provider...");
    var localConfig = new LLMConfiguration
    {
        ProviderType = ProviderType.Local,
        Endpoint = "http://localhost:11434",
        ModelIdentifier = "tinyllama"
    };
    
    using var httpClient = new HttpClient();
    provider = new LocalProvider(localConfig, httpClient);
    return await provider.AnalyzeAsync(context, prompt);
}
```

## Provider-Specific Guides

### AWS Bedrock Setup

1. **Prerequisites**:
   - AWS account with Bedrock access
   - IAM user with Bedrock permissions
   - Model access enabled in Bedrock console

2. **Enable Model Access**:
   ```
   AWS Console → Bedrock → Model access → Request access
   Select models (e.g., Claude 3 Sonnet) → Submit
   ```

3. **Get Credentials**:
   ```
   AWS Console → IAM → Users → Security credentials
   Create access key → Save Access Key ID and Secret Access Key
   ```

4. **Configuration**:
   ```csharp
   var config = new LLMConfiguration
   {
       ProviderType = ProviderType.Bedrock,
       ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
       AccessKey = "AKIAIOSFODNN7EXAMPLE",
       SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
       Region = "us-east-1"
   };
   ```

### Local Provider (Ollama) Setup

1. **Install Ollama**:
   ```bash
   # macOS
   brew install ollama
   
   # Linux
   curl -fsSL https://ollama.ai/install.sh | sh
   
   # Windows: Download from https://ollama.ai
   ```

2. **Start Ollama**:
   ```bash
   ollama serve
   ```

3. **Pull a Model**:
   ```bash
   # Lightweight models for personal PCs
   ollama pull tinyllama      # 637MB - ultra fast
   ollama pull phi3:mini      # 3.8GB - balanced
   ollama pull qwen2:1.5b     # 934MB - efficient
   
   # Larger models for better quality
   ollama pull llama2         # 3.8GB
   ollama pull mistral        # 4.1GB
   ```

4. **Configuration**:
   ```csharp
   var config = new LLMConfiguration
   {
       ProviderType = ProviderType.Local,
       Endpoint = "http://localhost:11434",
       ModelIdentifier = "tinyllama"
   };
   ```

## Error Handling

### Error Types

```csharp
public enum ErrorType
{
    ValidationError,      // Config or input validation failed
    ConnectionError,      // Network connectivity issues
    AuthenticationError,  // Invalid credentials
    RateLimitError,       // Quota exceeded
    ProviderError,        // Provider-specific error
    UnknownError         // Unexpected error
}
```

### Handling Errors

```csharp
var response = await provider.AnalyzeAsync(context, prompt);

if (!response.Success)
{
    switch (response.Error.Type)
    {
        case ErrorType.ValidationError:
            Console.WriteLine("Invalid input or configuration");
            break;
            
        case ErrorType.ConnectionError:
            Console.WriteLine("Cannot connect to provider");
            Console.WriteLine("Check network and endpoint URL");
            break;
            
        case ErrorType.AuthenticationError:
            Console.WriteLine("Invalid credentials");
            Console.WriteLine("Verify AWS keys or API tokens");
            break;
            
        case ErrorType.RateLimitError:
            Console.WriteLine("Rate limit exceeded");
            Console.WriteLine("Wait before retrying");
            break;
            
        case ErrorType.ProviderError:
            Console.WriteLine("Provider-specific error");
            Console.WriteLine(response.Error.Message);
            break;
            
        default:
            Console.WriteLine("Unexpected error occurred");
            break;
    }
}
```

## Best Practices

### 1. Configuration Management

```csharp
// ✅ DO: Use environment variables
var config = new LLMConfiguration
{
    AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
    SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
};

// ❌ DON'T: Hardcode credentials
var config = new LLMConfiguration
{
    AccessKey = "AKIAIOSFODNN7EXAMPLE",  // Never do this!
    SecretKey = "wJalrXUtnFEMI..."       // Never do this!
};
```

### 2. Always Validate

```csharp
// ✅ DO: Validate configuration
var validation = config.Validate();
if (!validation.Success)
{
    // Handle validation errors
    return;
}

// ✅ DO: Validate provider connectivity
var providerValidation = await provider.ValidateAsync();
if (!providerValidation.Success)
{
    // Handle connectivity errors
    return;
}
```

### 3. Use Cancellation Tokens

```csharp
// ✅ DO: Support cancellation
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await provider.AnalyzeAsync(
    context, 
    prompt, 
    cts.Token
);
```

### 4. Dispose Resources

```csharp
// ✅ DO: Dispose HttpClient properly
using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);

// Or use dependency injection
services.AddHttpClient<ILLMProvider, LocalProvider>();
```

### 5. Handle Long Contexts

```csharp
// ✅ DO: Check context length
const int MaxContextLength = 100000;  // Model-specific limit

if (context.Length > MaxContextLength)
{
    // Truncate or chunk the context
    context = context.Substring(0, MaxContextLength);
}
```

## Troubleshooting

### Issue: "Connection refused" (Local Provider)

**Solution**:
```bash
# Ensure Ollama is running
ollama serve

# Check if it's accessible
curl http://localhost:11434/api/tags
```

### Issue: "Model not found" (Local Provider)

**Solution**:
```bash
# List available models
ollama list

# Pull the model
ollama pull tinyllama
```

### Issue: "Authentication failed" (Bedrock)

**Solution**:
- Verify AWS credentials are correct
- Check IAM permissions include Bedrock access
- Ensure credentials haven't expired
- Try credentials with AWS CLI: `aws bedrock list-foundation-models`

### Issue: "Model access denied" (Bedrock)

**Solution**:
- Go to AWS Bedrock Console → Model access
- Request access to the model
- Wait for approval (usually instant)

### Issue: "Timeout" errors

**Solution**:
```csharp
// Increase timeout
var config = new LLMConfiguration
{
    TimeoutSeconds = 120,  // Increase from default 30
    MaxRetries = 5         // Increase retries
};
```

### Issue: Session not maintaining context

**Solution**:
```csharp
// ✅ DO: Reuse the same session object
var session = sessionManager.CreateSession();
await provider.SendMessageAsync(session, "Message 1");
await provider.SendMessageAsync(session, "Message 2");  // Same session

// ❌ DON'T: Create new sessions
var session1 = sessionManager.CreateSession();
await provider.SendMessageAsync(session1, "Message 1");
var session2 = sessionManager.CreateSession();  // New session loses context!
await provider.SendMessageAsync(session2, "Message 2");
```

## Architecture

```mermaid
graph TD
    A[Your Application] --> B[ILLMProvider Interface]
    B --> C[BedrockProvider]
    B --> D[LocalProvider]
    C --> E[AWS Bedrock]
    D --> F[Ollama/Local LLM]
    
    A --> G[ISessionManager]
    G --> H[SessionManager]
    H --> I[Session Storage]
    
    B --> J[LLMConfiguration]
    B --> K[LLMResponse]
    K --> L[LLMError]
```

## API Overview

### Interfaces
- `ILLMProvider` - Main provider interface
- `ISessionManager` - Session management interface

### Classes
- `LLMConfiguration` - Provider configuration
- `BedrockProvider` - AWS Bedrock implementation
- `LocalProvider` - Local LLM implementation
- `SessionManager` - Session management implementation
- `Session` - Conversation session

### Models
- `LLMResponse` - Response wrapper
- `LLMError` - Error information
- `Message` - Chat message
- `ValidationResult` - Validation result

### Enums
- `ProviderType` - Provider types (Bedrock, Local)
- `ErrorType` - Error categories
- `MessageRole` - Message roles (User, Assistant, System)

## Further Reading

- [Getting Started Guide](GETTING_STARTED.md) - Step-by-step integration
- [Usage Guide](USAGE_GUIDE.md) - Detailed usage patterns
- [API Reference](API_REFERENCE.md) - Complete API documentation

## Contributing

Contributions are welcome! To add a new provider:

1. Implement `ILLMProvider` interface
2. Add provider type to `ProviderType` enum
3. Update `LLMConfiguration.Validate()` for provider-specific validation
4. Add tests and documentation

## License

[Your License Here]

## Support

For issues, questions, or contributions, please [open an issue](link-to-issues) or contact the maintainers.

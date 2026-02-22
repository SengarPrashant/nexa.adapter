# API Reference

Complete API documentation for the LLMProviderAbstraction library.

## Table of Contents

- [Interfaces](#interfaces)
  - [ILLMProvider](#illmprovider)
  - [ISessionManager](#isessionmanager)
- [Classes](#classes)
  - [LLMConfiguration](#llmconfiguration)
  - [BedrockProvider](#bedrockprovider)
  - [LocalProvider](#localprovider)
  - [SessionManager](#sessionmanager)
  - [Session](#session)
- [Models](#models)
  - [LLMResponse](#llmresponse)
  - [LLMError](#llmerror)
  - [Message](#message)
  - [ValidationResult](#validationresult)
- [Enums](#enums)
  - [ProviderType](#providertype)
  - [ErrorType](#errortype)
  - [MessageRole](#messagerole)

---

## Interfaces

### ILLMProvider

Defines the contract for LLM provider implementations.

**Namespace:** `LLMProviderAbstraction.Interfaces`

#### Methods

##### AnalyzeAsync

Sends a context-based analysis request to the LLM.

```csharp
Task<LLMResponse> AnalyzeAsync(
    string context,
    string prompt,
    CancellationToken cancellationToken = default
)
```

**Parameters:**
- `context` (string): Input data or information provided to the LLM for analysis
- `prompt` (string): The prompt or question to ask about the context
- `cancellationToken` (CancellationToken, optional): Token to cancel the operation

**Returns:** `Task<LLMResponse>` - The LLM response

**Example:**
```csharp
var context = "The Earth orbits the Sun.";
var prompt = "What does the Earth orbit?";
var response = await provider.AnalyzeAsync(context, prompt);

if (response.Success)
{
    Console.WriteLine(response.Content); // "The Sun"
}
```

**Use Cases:**
- Document analysis
- Question answering
- Text summarization
- Data extraction
- One-shot queries without conversation history

---

##### SendMessageAsync

Sends a message within a session context, maintaining conversation history.

```csharp
Task<LLMResponse> SendMessageAsync(
    Session session,
    string message,
    CancellationToken cancellationToken = default
)
```

**Parameters:**
- `session` (Session): The conversation session containing message history
- `message` (string): The message to send
- `cancellationToken` (CancellationToken, optional): Token to cancel the operation

**Returns:** `Task<LLMResponse>` - The LLM response

**Example:**
```csharp
var sessionManager = new SessionManager();
var session = sessionManager.CreateSession();

var response1 = await provider.SendMessageAsync(session, "My name is Alice");
var response2 = await provider.SendMessageAsync(session, "What's my name?");

Console.WriteLine(response2.Content); // "Your name is Alice"
```

**Use Cases:**
- Chatbots
- Interactive assistants
- Multi-turn conversations
- Context-aware dialogues

---

##### ValidateAsync

Validates the provider configuration and connectivity.

```csharp
Task<ValidationResult> ValidateAsync(
    CancellationToken cancellationToken = default
)
```

**Parameters:**
- `cancellationToken` (CancellationToken, optional): Token to cancel the operation

**Returns:** `Task<ValidationResult>` - Validation result indicating success or failure

**Example:**
```csharp
var validation = await provider.ValidateAsync();

if (validation.Success)
{
    Console.WriteLine("Provider is ready");
}
else
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

**Validation Checks:**
- Network connectivity
- Authentication credentials
- Model availability
- Endpoint accessibility

---

### ISessionManager

Manages conversation sessions and their message history.

**Namespace:** `LLMProviderAbstraction.Interfaces`

#### Methods

##### CreateSession

Creates a new conversation session.

```csharp
Session CreateSession(string? sessionId = null)
```

**Parameters:**
- `sessionId` (string?, optional): Optional session identifier. If null, a new GUID will be generated.

**Returns:** `Session` - A new Session instance

**Example:**
```csharp
var sessionManager = new SessionManager();

// Auto-generated session ID
var session1 = sessionManager.CreateSession();

// Custom session ID
var session2 = sessionManager.CreateSession("user-123-session");
```

---

##### GetSession

Retrieves an existing session by ID.

```csharp
Session? GetSession(string sessionId)
```

**Parameters:**
- `sessionId` (string): The unique identifier of the session to retrieve

**Returns:** `Session?` - The Session if found, otherwise null

**Example:**
```csharp
var session = sessionManager.GetSession("user-123-session");

if (session != null)
{
    Console.WriteLine($"Found session: {session.SessionId}");
}
else
{
    Console.WriteLine("Session not found");
}
```

---

##### GetSessionHistory

Retrieves all messages in a session.

```csharp
IReadOnlyList<Message> GetSessionHistory(string sessionId)
```

**Parameters:**
- `sessionId` (string): The unique identifier of the session

**Returns:** `IReadOnlyList<Message>` - Read-only list of messages, or empty list if session not found

**Example:**
```csharp
var history = sessionManager.GetSessionHistory("user-123-session");

foreach (var message in history)
{
    Console.WriteLine($"{message.Role}: {message.Content}");
}
```

---

## Classes

### LLMConfiguration

Configuration settings for LLM provider initialization.

**Namespace:** `LLMProviderAbstraction.Configuration`

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ProviderType` | `ProviderType` | - | The type of LLM provider (Bedrock or Local) |
| `ModelIdentifier` | `string` | `""` | Model identifier (e.g., "anthropic.claude-3-sonnet-20240229-v1:0" or "tinyllama") |
| `AccessKey` | `string?` | `null` | Access key for cloud providers (required for Bedrock) |
| `SecretKey` | `string?` | `null` | Secret key for cloud providers (required for Bedrock) |
| `Region` | `string?` | `null` | AWS region (optional, defaults to us-east-1 for Bedrock) |
| `Endpoint` | `string?` | `null` | Endpoint URL for local providers (required for Local) |
| `TimeoutSeconds` | `int` | `30` | Request timeout in seconds |
| `MaxRetries` | `int` | `3` | Maximum retry attempts for transient errors |

#### Methods

##### Validate

Validates the configuration and returns a result indicating success or failure.

```csharp
ValidationResult Validate()
```

**Returns:** `ValidationResult` - Validation result with success status and error messages

**Example:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama"
};

var validation = config.Validate();

if (!validation.Success)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine(error);
    }
}
```

**Validation Rules:**
- `ModelIdentifier` must not be empty
- **Bedrock**: `AccessKey` and `SecretKey` are required
- **Local**: `Endpoint` must be a valid absolute URI
- `TimeoutSeconds` must be greater than 0
- `MaxRetries` must be greater than or equal to 0

#### Configuration Examples

**Local Provider:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama",
    TimeoutSeconds = 60,
    MaxRetries = 3
};
```

**Bedrock Provider:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Bedrock,
    ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
    AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
    SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
    Region = "us-east-1",
    TimeoutSeconds = 60,
    MaxRetries = 3
};
```

---

### BedrockProvider

AWS Bedrock LLM provider implementation.

**Namespace:** `LLMProviderAbstraction.Providers`

**Implements:** `ILLMProvider`

#### Constructor

```csharp
public BedrockProvider(LLMConfiguration configuration)
```

**Parameters:**
- `configuration` (LLMConfiguration): Provider configuration with Bedrock-specific settings

**Example:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Bedrock,
    ModelIdentifier = "anthropic.claude-3-sonnet-20240229-v1:0",
    AccessKey = "your-access-key",
    SecretKey = "your-secret-key",
    Region = "us-east-1"
};

ILLMProvider provider = new BedrockProvider(config);
```

#### Supported Models

| Model ID | Description | Context Window |
|----------|-------------|----------------|
| `anthropic.claude-3-sonnet-20240229-v1:0` | Claude 3 Sonnet | 200K tokens |
| `anthropic.claude-3-haiku-20240307-v1:0` | Claude 3 Haiku | 200K tokens |
| `anthropic.claude-3-opus-20240229-v1:0` | Claude 3 Opus | 200K tokens |
| `meta.llama2-70b-chat-v1` | Llama 2 70B Chat | 4K tokens |
| `meta.llama2-13b-chat-v1` | Llama 2 13B Chat | 4K tokens |

**Note:** Model availability varies by AWS region. Check AWS Bedrock console for available models.

---

### LocalProvider

Local LLM provider implementation (Ollama).

**Namespace:** `LLMProviderAbstraction.Providers`

**Implements:** `ILLMProvider`

#### Constructor

```csharp
public LocalProvider(
    LLMConfiguration configuration,
    HttpClient httpClient
)
```

**Parameters:**
- `configuration` (LLMConfiguration): Provider configuration with Local-specific settings
- `httpClient` (HttpClient): HTTP client for making requests to Ollama

**Example:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama"
};

using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);
```

#### Recommended Models

| Model | Size | Description | Use Case |
|-------|------|-------------|----------|
| `tinyllama` | 637MB | Ultra lightweight | Quick testing, low-spec PCs |
| `phi3:mini` | 3.8GB | Microsoft's efficient model | Balanced performance |
| `qwen2:1.5b` | 934MB | Alibaba's efficient model | General tasks |
| `llama2` | 3.8GB | Meta's popular model | General purpose |
| `mistral` | 4.1GB | High quality | Complex tasks |
| `codellama` | 3.8GB | Code-specialized | Code generation |

**Installation:**
```bash
ollama pull tinyllama
```

---

### SessionManager

Manages conversation sessions with thread-safe storage.

**Namespace:** `LLMProviderAbstraction.Session`

**Implements:** `ISessionManager`

#### Constructor

```csharp
public SessionManager()
```

**Example:**
```csharp
ISessionManager sessionManager = new SessionManager();
```

#### Thread Safety

SessionManager uses `ConcurrentDictionary` internally, making it thread-safe for concurrent access.

```csharp
// Safe to use from multiple threads
var sessionManager = new SessionManager();

await Task.WhenAll(
    Task.Run(() => sessionManager.CreateSession("session-1")),
    Task.Run(() => sessionManager.CreateSession("session-2"))
);
```

---

### Session

Represents a conversation session that maintains message history.

**Namespace:** `LLMProviderAbstraction.Session`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SessionId` | `string` | Unique identifier for the session |
| `CreatedAt` | `DateTime` | Timestamp when the session was created (UTC) |
| `LastAccessedAt` | `DateTime` | Timestamp when the session was last accessed (UTC) |
| `Messages` | `IReadOnlyList<Message>` | Read-only collection of messages in the session |

#### Constructor

```csharp
public Session(string sessionId)
```

**Parameters:**
- `sessionId` (string): Unique identifier for the session

**Note:** Typically created via `SessionManager.CreateSession()` rather than directly.

#### Methods

##### AddMessage

Adds a message to the session and updates the last accessed timestamp.

```csharp
public void AddMessage(Message message)
```

**Parameters:**
- `message` (Message): The message to add

**Example:**
```csharp
var session = sessionManager.CreateSession();

// Add system message
session.AddMessage(new Message(
    "You are a helpful assistant",
    MessageRole.System
));

// Add user message
session.AddMessage(new Message(
    "Hello!",
    MessageRole.User
));
```

---

## Models

### LLMResponse

Represents the response from an LLM operation.

**Namespace:** `LLMProviderAbstraction.Models`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Indicates whether the operation was successful |
| `Content` | `string?` | The response content (null if operation failed) |
| `Error` | `LLMError?` | Error information (null if operation succeeded) |
| `Metadata` | `Dictionary<string, object>` | Additional metadata about the response |

#### Static Methods

##### CreateSuccess

Creates a successful response.

```csharp
public static LLMResponse CreateSuccess(
    string content,
    Dictionary<string, object>? metadata = null
)
```

**Parameters:**
- `content` (string): The response content
- `metadata` (Dictionary<string, object>?, optional): Optional metadata

**Returns:** `LLMResponse` - A successful response

**Example:**
```csharp
var response = LLMResponse.CreateSuccess(
    "The answer is 42",
    new Dictionary<string, object>
    {
        { "model", "tinyllama" },
        { "tokens", 15 }
    }
);
```

##### CreateError

Creates an error response.

```csharp
public static LLMResponse CreateError(LLMError error)
```

**Parameters:**
- `error` (LLMError): The error information

**Returns:** `LLMResponse` - An error response

**Example:**
```csharp
var error = new LLMError(
    ErrorType.ConnectionError,
    "Failed to connect to provider"
);
var response = LLMResponse.CreateError(error);
```

#### Usage Example

```csharp
var response = await provider.AnalyzeAsync(context, prompt);

if (response.Success)
{
    Console.WriteLine($"Content: {response.Content}");
    
    // Access metadata
    if (response.Metadata.TryGetValue("tokens", out var tokens))
    {
        Console.WriteLine($"Tokens used: {tokens}");
    }
}
else
{
    Console.WriteLine($"Error: {response.Error.Type}");
    Console.WriteLine($"Message: {response.Error.Message}");
}
```

---

### LLMError

Represents an error that occurred during LLM operations.

**Namespace:** `LLMProviderAbstraction.Models`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `ErrorType` | The type of error |
| `Message` | `string` | A descriptive error message |
| `InnerException` | `Exception?` | The underlying exception, if any |

#### Constructor

```csharp
public LLMError(
    ErrorType type,
    string message,
    Exception? innerException = null
)
```

**Parameters:**
- `type` (ErrorType): The error type
- `message` (string): A descriptive error message
- `innerException` (Exception?, optional): The underlying exception

**Example:**
```csharp
try
{
    // Some operation
}
catch (HttpRequestException ex)
{
    var error = new LLMError(
        ErrorType.ConnectionError,
        "Failed to connect to LLM provider",
        ex
    );
    return LLMResponse.CreateError(error);
}
```

---

### Message

Represents a single message in a conversation.

**Namespace:** `LLMProviderAbstraction.Models`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Content` | `string` | The content of the message |
| `Role` | `MessageRole` | The role of the message sender |
| `Timestamp` | `DateTime` | The timestamp when the message was created (UTC) |

#### Constructor

```csharp
public Message(string content, MessageRole role)
```

**Parameters:**
- `content` (string): The message content
- `role` (MessageRole): The role of the message sender

**Example:**
```csharp
// User message
var userMessage = new Message("Hello!", MessageRole.User);

// Assistant message
var assistantMessage = new Message("Hi there!", MessageRole.Assistant);

// System message
var systemMessage = new Message(
    "You are a helpful assistant",
    MessageRole.System
);
```

---

### ValidationResult

Represents the result of a validation operation.

**Namespace:** `LLMProviderAbstraction.Models`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Indicates whether the validation succeeded |
| `Errors` | `IReadOnlyList<string>` | Collection of error messages from validation failures |

#### Static Methods

##### CreateSuccess

Creates a successful validation result.

```csharp
public static ValidationResult CreateSuccess()
```

**Returns:** `ValidationResult` - A successful validation result

**Example:**
```csharp
var result = ValidationResult.CreateSuccess();
Console.WriteLine(result.Success); // true
Console.WriteLine(result.Errors.Count); // 0
```

##### CreateFailure

Creates a failed validation result with error messages.

```csharp
public static ValidationResult CreateFailure(IEnumerable<string> errors)
```

**Parameters:**
- `errors` (IEnumerable<string>): Collection of validation error messages

**Returns:** `ValidationResult` - A failed validation result

**Example:**
```csharp
var errors = new List<string>
{
    "ModelIdentifier is required",
    "Endpoint must be a valid URI"
};

var result = ValidationResult.CreateFailure(errors);
Console.WriteLine(result.Success); // false

foreach (var error in result.Errors)
{
    Console.WriteLine(error);
}
```

---

## Enums

### ProviderType

Represents the type of LLM provider.

**Namespace:** `LLMProviderAbstraction.Models`

#### Values

| Value | Description |
|-------|-------------|
| `Bedrock` | Amazon Bedrock cloud-based provider |
| `Local` | Locally hosted LLM provider (Ollama) |

**Example:**
```csharp
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Bedrock
};

if (config.ProviderType == ProviderType.Local)
{
    Console.WriteLine("Using local provider");
}
```

---

### ErrorType

Represents the type of error that occurred.

**Namespace:** `LLMProviderAbstraction.Models`

#### Values

| Value | Description | Common Causes |
|-------|-------------|---------------|
| `ValidationError` | Configuration or input validation error | Missing required fields, invalid format |
| `ConnectionError` | Network connectivity error | Network down, wrong endpoint, firewall |
| `AuthenticationError` | Authentication or authorization error | Invalid credentials, expired tokens |
| `RateLimitError` | Rate limit or quota exceeded error | Too many requests, quota exhausted |
| `ProviderError` | Provider-specific error | Model not found, malformed request |
| `UnknownError` | Unknown or unexpected error | Unexpected exceptions |

**Example:**
```csharp
var response = await provider.AnalyzeAsync(context, prompt);

if (!response.Success)
{
    switch (response.Error.Type)
    {
        case ErrorType.ValidationError:
            Console.WriteLine("Check your input");
            break;
        case ErrorType.ConnectionError:
            Console.WriteLine("Check network connectivity");
            break;
        case ErrorType.AuthenticationError:
            Console.WriteLine("Check credentials");
            break;
        case ErrorType.RateLimitError:
            Console.WriteLine("Wait before retrying");
            break;
        case ErrorType.ProviderError:
            Console.WriteLine("Provider-specific issue");
            break;
        case ErrorType.UnknownError:
            Console.WriteLine("Unexpected error");
            break;
    }
}
```

---

### MessageRole

Represents the role of a message in a conversation.

**Namespace:** `LLMProviderAbstraction.Models`

#### Values

| Value | Description | Usage |
|-------|-------------|-------|
| `User` | Message from the user | User input, questions |
| `Assistant` | Message from the AI assistant | LLM responses |
| `System` | System message for context or instructions | Behavior instructions, context setting |

**Example:**
```csharp
var session = sessionManager.CreateSession();

// System message - sets behavior
session.AddMessage(new Message(
    "You are a helpful coding assistant",
    MessageRole.System
));

// User message - user input
session.AddMessage(new Message(
    "How do I reverse a string in C#?",
    MessageRole.User
));

// Assistant message - LLM response
session.AddMessage(new Message(
    "You can use Array.Reverse() or LINQ...",
    MessageRole.Assistant
));
```

---

## Complete Usage Example

Here's a complete example demonstrating the full API:

```csharp
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Session;

// 1. Configure provider
var config = new LLMConfiguration
{
    ProviderType = ProviderType.Local,
    Endpoint = "http://localhost:11434",
    ModelIdentifier = "tinyllama",
    TimeoutSeconds = 60,
    MaxRetries = 3
};

// 2. Validate configuration
var configValidation = config.Validate();
if (!configValidation.Success)
{
    foreach (var error in configValidation.Errors)
        Console.WriteLine($"Config error: {error}");
    return;
}

// 3. Create provider
using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);

// 4. Validate provider
var providerValidation = await provider.ValidateAsync();
if (!providerValidation.Success)
{
    foreach (var error in providerValidation.Errors)
        Console.WriteLine($"Provider error: {error}");
    return;
}

// 5. Context-based analysis
var analysisResponse = await provider.AnalyzeAsync(
    "The capital of France is Paris.",
    "What is the capital of France?"
);

if (analysisResponse.Success)
{
    Console.WriteLine($"Analysis: {analysisResponse.Content}");
}

// 6. Session-based chat
ISessionManager sessionManager = new SessionManager();
var session = sessionManager.CreateSession();

// Add system context
session.AddMessage(new Message(
    "You are a helpful assistant",
    MessageRole.System
));

// Send messages
var chatResponse1 = await provider.SendMessageAsync(session, "My name is Alice");
var chatResponse2 = await provider.SendMessageAsync(session, "What's my name?");

if (chatResponse2.Success)
{
    Console.WriteLine($"Chat: {chatResponse2.Content}");
}

// 7. View session history
var history = sessionManager.GetSessionHistory(session.SessionId);
foreach (var message in history)
{
    Console.WriteLine($"{message.Role} ({message.Timestamp}): {message.Content}");
}
```

---

## See Also

- [README.md](README.md) - Library overview and features
- [GETTING_STARTED.md](GETTING_STARTED.md) - Step-by-step integration guide
- [USAGE_GUIDE.md](USAGE_GUIDE.md) - Detailed usage patterns and examples

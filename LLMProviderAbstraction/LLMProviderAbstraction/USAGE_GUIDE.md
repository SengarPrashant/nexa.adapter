# Usage Guide

This guide provides detailed usage patterns, best practices, and real-world examples for the LLMProviderAbstraction library.

## Table of Contents

- [Context-Based Analysis](#context-based-analysis)
- [Session-Based Chatbots](#session-based-chatbots)
- [Error Handling Patterns](#error-handling-patterns)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)
- [Performance Optimization](#performance-optimization)
- [Common Pitfalls](#common-pitfalls)

## Context-Based Analysis

Context-based analysis is ideal for one-shot questions where you provide context and ask a specific question.

### Basic Context Analysis

```csharp
var context = @"
Product: SmartWatch Pro
Price: $299
Features: Heart rate monitor, GPS, 7-day battery life, waterproof
Rating: 4.5/5 stars (1,234 reviews)
";

var prompt = "What are the main features of this product?";

var response = await provider.AnalyzeAsync(context, prompt);

if (response.Success)
{
    Console.WriteLine(response.Content);
}
```

### Document Summarization

```csharp
public async Task<string> SummarizeDocument(string documentPath)
{
    // Read the document
    var content = await File.ReadAllTextAsync(documentPath);
    
    // Summarize
    var response = await provider.AnalyzeAsync(
        content,
        "Provide a concise summary of this document in 3-5 sentences."
    );
    
    return response.Success ? response.Content : "Summarization failed";
}

// Usage
var summary = await SummarizeDocument("report.txt");
Console.WriteLine(summary);
```

### Sentiment Analysis

```csharp
public async Task<string> AnalyzeSentiment(string text)
{
    var response = await provider.AnalyzeAsync(
        text,
        "Analyze the sentiment of this text. Respond with: Positive, Negative, or Neutral."
    );
    
    return response.Success ? response.Content.Trim() : "Unknown";
}

// Usage
var reviews = new[]
{
    "This product is amazing! Best purchase ever!",
    "Terrible quality. Broke after one day.",
    "It's okay, nothing special."
};

foreach (var review in reviews)
{
    var sentiment = await AnalyzeSentiment(review);
    Console.WriteLine($"Review: {review}");
    Console.WriteLine($"Sentiment: {sentiment}\n");
}
```

### Data Extraction

```csharp
public async Task<Dictionary<string, string>> ExtractContactInfo(string text)
{
    var prompt = @"
Extract the following information from the text:
- Name
- Email
- Phone
- Company

Format as JSON: {""name"": ""..."", ""email"": ""..."", ""phone"": ""..."", ""company"": ""...""}
";

    var response = await provider.AnalyzeAsync(text, prompt);
    
    if (response.Success)
    {
        // Parse JSON response
        return JsonSerializer.Deserialize<Dictionary<string, string>>(response.Content);
    }
    
    return new Dictionary<string, string>();
}

// Usage
var businessCard = @"
John Smith
Senior Developer
Acme Corporation
john.smith@acme.com
+1-555-0123
";

var info = await ExtractContactInfo(businessCard);
Console.WriteLine($"Name: {info["name"]}");
Console.WriteLine($"Email: {info["email"]}");
```

### Code Review

```csharp
public async Task<string> ReviewCode(string code, string language)
{
    var prompt = $@"
Review this {language} code for:
1. Potential bugs
2. Performance issues
3. Security vulnerabilities
4. Best practice violations

Provide specific, actionable feedback.
";

    var response = await provider.AnalyzeAsync(code, prompt);
    return response.Success ? response.Content : "Review failed";
}

// Usage
var code = @"
public void ProcessUsers(List<User> users)
{
    for (int i = 0; i < users.Count; i++)
    {
        var user = users[i];
        Console.WriteLine(user.Name);
        // Process user...
    }
}
";

var review = await ReviewCode(code, "C#");
Console.WriteLine(review);
```

### Translation

```csharp
public async Task<string> Translate(string text, string targetLanguage)
{
    var response = await provider.AnalyzeAsync(
        text,
        $"Translate this text to {targetLanguage}. Provide only the translation, no explanations."
    );
    
    return response.Success ? response.Content : text;
}

// Usage
var english = "Hello, how are you today?";
var spanish = await Translate(english, "Spanish");
var french = await Translate(english, "French");

Console.WriteLine($"English: {english}");
Console.WriteLine($"Spanish: {spanish}");
Console.WriteLine($"French: {french}");
```

## Session-Based Chatbots

Session-based interactions maintain conversation history, allowing the LLM to remember previous exchanges.

### Basic Chatbot

```csharp
public async Task RunChatbot()
{
    var sessionManager = new SessionManager();
    var session = sessionManager.CreateSession();
    
    Console.WriteLine("Chatbot started. Type 'exit' to quit.\n");
    
    while (true)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();
        
        if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
            break;
        
        var response = await provider.SendMessageAsync(session, input);
        
        if (response.Success)
        {
            Console.WriteLine($"Bot: {response.Content}\n");
        }
        else
        {
            Console.WriteLine($"Error: {response.Error?.Message}\n");
        }
    }
}
```

### Chatbot with System Prompt

```csharp
public async Task RunCustomerSupportBot()
{
    var sessionManager = new SessionManager();
    var session = sessionManager.CreateSession();
    
    // Add system message to set behavior
    session.AddMessage(new Message(
        @"You are a helpful customer support agent for TechStore.
        - Be polite and professional
        - Provide accurate information about products
        - Offer solutions to problems
        - If you don't know something, admit it and offer to escalate",
        MessageRole.System
    ));
    
    Console.WriteLine("Customer Support Bot - How can I help you today?\n");
    
    while (true)
    {
        Console.Write("Customer: ");
        var input = Console.ReadLine();
        
        if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
            break;
        
        var response = await provider.SendMessageAsync(session, input);
        
        if (response.Success)
        {
            Console.WriteLine($"Agent: {response.Content}\n");
        }
    }
}
```

### Multi-Turn Information Gathering

```csharp
public async Task<Dictionary<string, string>> GatherUserInfo()
{
    var sessionManager = new SessionManager();
    var session = sessionManager.CreateSession();
    var userInfo = new Dictionary<string, string>();
    
    // System prompt for structured data gathering
    session.AddMessage(new Message(
        @"You are gathering user information. Ask one question at a time.
        Collect: name, email, phone, and reason for contact.
        Be conversational and friendly.",
        MessageRole.System
    ));
    
    // Start conversation
    var response = await provider.SendMessageAsync(
        session,
        "Start gathering user information"
    );
    Console.WriteLine($"Bot: {response.Content}");
    
    // Collect responses
    for (int i = 0; i < 4; i++)
    {
        Console.Write("You: ");
        var input = Console.ReadLine();
        
        response = await provider.SendMessageAsync(session, input);
        Console.WriteLine($"Bot: {response.Content}");
    }
    
    // Extract information from conversation
    var history = sessionManager.GetSessionHistory(session.SessionId);
    // Parse history to extract structured data...
    
    return userInfo;
}
```

### Context-Aware Assistant

```csharp
public class PersonalAssistant
{
    private readonly ILLMProvider _provider;
    private readonly ISessionManager _sessionManager;
    private readonly Session _session;
    
    public PersonalAssistant(ILLMProvider provider, ISessionManager sessionManager)
    {
        _provider = provider;
        _sessionManager = sessionManager;
        _session = _sessionManager.CreateSession();
        
        // Initialize with context
        _session.AddMessage(new Message(
            @"You are a personal assistant. Remember information the user tells you.
            Help with scheduling, reminders, and answering questions.
            Be proactive and helpful.",
            MessageRole.System
        ));
    }
    
    public async Task<string> SendMessage(string message)
    {
        var response = await _provider.SendMessageAsync(_session, message);
        return response.Success ? response.Content : "I'm having trouble responding right now.";
    }
    
    public IReadOnlyList<Message> GetHistory()
    {
        return _sessionManager.GetSessionHistory(_session.SessionId);
    }
}

// Usage
var assistant = new PersonalAssistant(provider, sessionManager);

await assistant.SendMessage("My name is Alice and I work at Acme Corp");
await assistant.SendMessage("I have a meeting tomorrow at 2 PM");
var response = await assistant.SendMessage("What's my name and when is my meeting?");

Console.WriteLine(response);
// Expected: "Your name is Alice and you have a meeting tomorrow at 2 PM."
```

### Session Management

```csharp
public class ConversationManager
{
    private readonly ISessionManager _sessionManager;
    private readonly Dictionary<string, string> _userSessions;
    
    public ConversationManager(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _userSessions = new Dictionary<string, string>();
    }
    
    public Session GetOrCreateSession(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionId))
        {
            var existingSession = _sessionManager.GetSession(sessionId);
            if (existingSession != null)
                return existingSession;
        }
        
        // Create new session
        var newSession = _sessionManager.CreateSession();
        _userSessions[userId] = newSession.SessionId;
        return newSession;
    }
    
    public void ClearSession(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionId))
        {
            _userSessions.Remove(userId);
            // Session will be garbage collected
        }
    }
    
    public IReadOnlyList<Message> GetHistory(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessionId))
        {
            return _sessionManager.GetSessionHistory(sessionId);
        }
        return Array.Empty<Message>();
    }
}

// Usage
var conversationManager = new ConversationManager(sessionManager);

// User 1
var session1 = conversationManager.GetOrCreateSession("user123");
await provider.SendMessageAsync(session1, "Hello!");

// User 2
var session2 = conversationManager.GetOrCreateSession("user456");
await provider.SendMessageAsync(session2, "Hi there!");

// Each user has separate conversation history
```

## Error Handling Patterns

### Basic Error Handling

```csharp
var response = await provider.AnalyzeAsync(context, prompt);

if (!response.Success)
{
    Console.WriteLine($"Error Type: {response.Error.Type}");
    Console.WriteLine($"Message: {response.Error.Message}");
    
    if (response.Error.InnerException != null)
    {
        Console.WriteLine($"Details: {response.Error.InnerException.Message}");
    }
}
```

### Retry Logic

```csharp
public async Task<LLMResponse> AnalyzeWithRetry(
    string context,
    string prompt,
    int maxAttempts = 3)
{
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var response = await provider.AnalyzeAsync(context, prompt);
            
            if (response.Success)
                return response;
            
            // Retry on transient errors
            if (response.Error.Type == ErrorType.ConnectionError ||
                response.Error.Type == ErrorType.RateLimitError)
            {
                Console.WriteLine($"Attempt {attempt} failed, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
                continue;
            }
            
            // Don't retry on permanent errors
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Attempt {attempt} threw exception: {ex.Message}");
            if (attempt == maxAttempts)
                throw;
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    
    return LLMResponse.CreateError(new LLMError(
        ErrorType.UnknownError,
        "Max retry attempts exceeded"
    ));
}
```

### Graceful Degradation

```csharp
public async Task<string> GetResponseWithFallback(string context, string prompt)
{
    // Try primary provider (Bedrock)
    var primaryResponse = await primaryProvider.AnalyzeAsync(context, prompt);
    if (primaryResponse.Success)
        return primaryResponse.Content;
    
    Console.WriteLine("Primary provider failed, trying fallback...");
    
    // Try fallback provider (Local)
    var fallbackResponse = await fallbackProvider.AnalyzeAsync(context, prompt);
    if (fallbackResponse.Success)
        return fallbackResponse.Content;
    
    // Return default response
    return "I'm unable to process your request at this time. Please try again later.";
}
```

### Error-Specific Handling

```csharp
public async Task<LLMResponse> HandleErrors(string context, string prompt)
{
    var response = await provider.AnalyzeAsync(context, prompt);
    
    if (!response.Success)
    {
        switch (response.Error.Type)
        {
            case ErrorType.ValidationError:
                Console.WriteLine("Invalid input. Please check your context and prompt.");
                // Log validation errors
                break;
                
            case ErrorType.ConnectionError:
                Console.WriteLine("Connection failed. Checking network...");
                // Attempt to reconnect or use cached response
                break;
                
            case ErrorType.AuthenticationError:
                Console.WriteLine("Authentication failed. Please check credentials.");
                // Refresh credentials or notify admin
                break;
                
            case ErrorType.RateLimitError:
                Console.WriteLine("Rate limit exceeded. Waiting before retry...");
                await Task.Delay(TimeSpan.FromMinutes(1));
                return await provider.AnalyzeAsync(context, prompt);
                
            case ErrorType.ProviderError:
                Console.WriteLine($"Provider error: {response.Error.Message}");
                // Log provider-specific error
                break;
                
            case ErrorType.UnknownError:
                Console.WriteLine("Unexpected error occurred.");
                // Log full exception details
                break;
        }
    }
    
    return response;
}
```

### Timeout Handling

```csharp
public async Task<LLMResponse> AnalyzeWithTimeout(
    string context,
    string prompt,
    TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    
    try
    {
        return await provider.AnalyzeAsync(context, prompt, cts.Token);
    }
    catch (OperationCanceledException)
    {
        return LLMResponse.CreateError(new LLMError(
            ErrorType.ConnectionError,
            $"Request timed out after {timeout.TotalSeconds} seconds"
        ));
    }
}

// Usage
var response = await AnalyzeWithTimeout(
    context,
    prompt,
    TimeSpan.FromSeconds(30)
);
```

## Advanced Scenarios

### Batch Processing

```csharp
public async Task<List<LLMResponse>> ProcessBatch(
    List<(string context, string prompt)> items,
    int maxConcurrency = 5)
{
    var semaphore = new SemaphoreSlim(maxConcurrency);
    var tasks = new List<Task<LLMResponse>>();
    
    foreach (var (context, prompt) in items)
    {
        await semaphore.WaitAsync();
        
        tasks.Add(Task.Run(async () =>
        {
            try
            {
                return await provider.AnalyzeAsync(context, prompt);
            }
            finally
            {
                semaphore.Release();
            }
        }));
    }
    
    return (await Task.WhenAll(tasks)).ToList();
}

// Usage
var items = new List<(string, string)>
{
    ("Document 1 content", "Summarize this"),
    ("Document 2 content", "Summarize this"),
    ("Document 3 content", "Summarize this")
};

var results = await ProcessBatch(items);
```

### Streaming Responses (Simulated)

```csharp
public async Task StreamResponse(string context, string prompt)
{
    Console.Write("Response: ");
    
    var response = await provider.AnalyzeAsync(context, prompt);
    
    if (response.Success)
    {
        // Simulate streaming by printing word by word
        var words = response.Content.Split(' ');
        foreach (var word in words)
        {
            Console.Write(word + " ");
            await Task.Delay(50); // Simulate streaming delay
        }
        Console.WriteLine();
    }
}
```

### Caching Responses

```csharp
public class CachedLLMProvider
{
    private readonly ILLMProvider _provider;
    private readonly Dictionary<string, LLMResponse> _cache;
    
    public CachedLLMProvider(ILLMProvider provider)
    {
        _provider = provider;
        _cache = new Dictionary<string, LLMResponse>();
    }
    
    public async Task<LLMResponse> AnalyzeAsync(string context, string prompt)
    {
        var cacheKey = $"{context}|{prompt}".GetHashCode().ToString();
        
        if (_cache.TryGetValue(cacheKey, out var cachedResponse))
        {
            Console.WriteLine("Returning cached response");
            return cachedResponse;
        }
        
        var response = await _provider.AnalyzeAsync(context, prompt);
        
        if (response.Success)
        {
            _cache[cacheKey] = response;
        }
        
        return response;
    }
    
    public void ClearCache()
    {
        _cache.Clear();
    }
}
```

### Multi-Provider Routing

```csharp
public class SmartRouter
{
    private readonly ILLMProvider _fastProvider;    // Local, fast but less accurate
    private readonly ILLMProvider _powerfulProvider; // Bedrock, slower but more accurate
    
    public SmartRouter(ILLMProvider fastProvider, ILLMProvider powerfulProvider)
    {
        _fastProvider = fastProvider;
        _powerfulProvider = powerfulProvider;
    }
    
    public async Task<LLMResponse> RouteRequest(string context, string prompt, bool requiresAccuracy)
    {
        if (requiresAccuracy || context.Length > 10000)
        {
            Console.WriteLine("Routing to powerful provider");
            return await _powerfulProvider.AnalyzeAsync(context, prompt);
        }
        else
        {
            Console.WriteLine("Routing to fast provider");
            return await _fastProvider.AnalyzeAsync(context, prompt);
        }
    }
}

// Usage
var router = new SmartRouter(localProvider, bedrockProvider);

// Simple query -> fast provider
var response1 = await router.RouteRequest("2+2", "Calculate", requiresAccuracy: false);

// Complex query -> powerful provider
var response2 = await router.RouteRequest(longDocument, "Analyze", requiresAccuracy: true);
```

## Best Practices

### 1. Always Validate Configuration

```csharp
// ✅ DO
var config = new LLMConfiguration { /* ... */ };
var validation = config.Validate();
if (!validation.Success)
{
    // Handle errors
    return;
}

// ❌ DON'T
var config = new LLMConfiguration { /* ... */ };
var provider = new LocalProvider(config, httpClient); // May fail at runtime
```

### 2. Use Dependency Injection

```csharp
// ✅ DO
public class MyService
{
    private readonly ILLMProvider _provider;
    private readonly ISessionManager _sessionManager;
    
    public MyService(ILLMProvider provider, ISessionManager sessionManager)
    {
        _provider = provider;
        _sessionManager = sessionManager;
    }
}

// ❌ DON'T
public class MyService
{
    public async Task DoWork()
    {
        var provider = new LocalProvider(/* ... */); // Creates new instance every time
    }
}
```

### 3. Handle Cancellation

```csharp
// ✅ DO
public async Task<LLMResponse> ProcessRequest(
    string context,
    string prompt,
    CancellationToken cancellationToken)
{
    return await provider.AnalyzeAsync(context, prompt, cancellationToken);
}

// ❌ DON'T
public async Task<LLMResponse> ProcessRequest(string context, string prompt)
{
    return await provider.AnalyzeAsync(context, prompt); // Can't be cancelled
}
```

### 4. Dispose Resources Properly

```csharp
// ✅ DO
using var httpClient = new HttpClient();
ILLMProvider provider = new LocalProvider(config, httpClient);

// ❌ DON'T
var httpClient = new HttpClient(); // Never disposed
ILLMProvider provider = new LocalProvider(config, httpClient);
```

### 5. Validate Provider Connectivity

```csharp
// ✅ DO
var validation = await provider.ValidateAsync();
if (!validation.Success)
{
    // Handle connectivity issues before making requests
    return;
}

// ❌ DON'T
// Skip validation and hope it works
var response = await provider.AnalyzeAsync(context, prompt);
```

## Performance Optimization

### 1. Reuse HttpClient

```csharp
// ✅ DO - Single HttpClient instance
private static readonly HttpClient _httpClient = new HttpClient();

public ILLMProvider CreateProvider()
{
    return new LocalProvider(config, _httpClient);
}

// ❌ DON'T - New HttpClient for each provider
public ILLMProvider CreateProvider()
{
    return new LocalProvider(config, new HttpClient());
}
```

### 2. Optimize Context Length

```csharp
// ✅ DO - Trim unnecessary content
var context = longDocument
    .Split('\n')
    .Where(line => !string.IsNullOrWhiteSpace(line))
    .Take(100) // Limit to relevant sections
    .Aggregate((a, b) => a + "\n" + b);

// ❌ DON'T - Send entire document
var context = File.ReadAllText("huge-document.txt"); // 10MB file
```

### 3. Use Parallel Processing

```csharp
// ✅ DO - Process multiple items concurrently
var tasks = items.Select(item => 
    provider.AnalyzeAsync(item.context, item.prompt)
);
var results = await Task.WhenAll(tasks);

// ❌ DON'T - Process sequentially
foreach (var item in items)
{
    var result = await provider.AnalyzeAsync(item.context, item.prompt);
}
```

## Common Pitfalls

### Pitfall 1: Creating New Sessions for Each Message

```csharp
// ❌ WRONG - Loses conversation context
var session1 = sessionManager.CreateSession();
await provider.SendMessageAsync(session1, "My name is Alice");

var session2 = sessionManager.CreateSession(); // New session!
await provider.SendMessageAsync(session2, "What's my name?");
// LLM doesn't know - different session

// ✅ CORRECT - Reuse same session
var session = sessionManager.CreateSession();
await provider.SendMessageAsync(session, "My name is Alice");
await provider.SendMessageAsync(session, "What's my name?");
// LLM remembers - same session
```

### Pitfall 2: Ignoring Error Types

```csharp
// ❌ WRONG - Treat all errors the same
if (!response.Success)
{
    Console.WriteLine("Error occurred");
    return;
}

// ✅ CORRECT - Handle different error types
if (!response.Success)
{
    switch (response.Error.Type)
    {
        case ErrorType.RateLimitError:
            await Task.Delay(60000); // Wait before retry
            break;
        case ErrorType.AuthenticationError:
            RefreshCredentials();
            break;
        // Handle other types...
    }
}
```

### Pitfall 3: Not Setting Timeouts

```csharp
// ❌ WRONG - No timeout, could hang forever
var response = await provider.AnalyzeAsync(context, prompt);

// ✅ CORRECT - Use cancellation token with timeout
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await provider.AnalyzeAsync(context, prompt, cts.Token);
```

### Pitfall 4: Hardcoding Credentials

```csharp
// ❌ WRONG - Credentials in source code
var config = new LLMConfiguration
{
    AccessKey = "AKIAIOSFODNN7EXAMPLE",
    SecretKey = "wJalrXUtnFEMI/K7MDENG..."
};

// ✅ CORRECT - Use environment variables
var config = new LLMConfiguration
{
    AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
    SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
};
```

### Pitfall 5: Not Validating Before Use

```csharp
// ❌ WRONG - Skip validation
var provider = new LocalProvider(config, httpClient);
var response = await provider.AnalyzeAsync(context, prompt); // May fail

// ✅ CORRECT - Validate first
var configValidation = config.Validate();
if (!configValidation.Success) return;

var provider = new LocalProvider(config, httpClient);
var providerValidation = await provider.ValidateAsync();
if (!providerValidation.Success) return;

var response = await provider.AnalyzeAsync(context, prompt);
```

## Summary

This guide covered:
- Context-based analysis patterns for one-shot queries
- Session-based chatbot patterns for conversations
- Comprehensive error handling strategies
- Advanced scenarios like batch processing and caching
- Best practices for production use
- Common pitfalls to avoid

For more information:
- [README.md](README.md) - Library overview
- [GETTING_STARTED.md](GETTING_STARTED.md) - Integration guide
- [API_REFERENCE.md](API_REFERENCE.md) - Complete API documentation

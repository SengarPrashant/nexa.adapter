# LLMProviderAbstraction Demo

This console application demonstrates how to use the LLMProviderAbstraction library with both AWS Bedrock and Local (Ollama) providers.

**Default Configuration**: The demo runs AWS Bedrock (Claude 3 Sonnet) by default with secure credential management.

## Prerequisites

### For Bedrock Provider (Default)
1. **AWS Account** with Bedrock access
2. **AWS Credentials**: Access Key ID and Secret Access Key
3. **IAM Permissions**: Ensure your IAM user has permissions for Bedrock service
4. **Model Access**: Enable model access in AWS Bedrock console (e.g., Claude 3 Sonnet)

### For Local Provider (Ollama)
1. Install Ollama from [https://ollama.ai](https://ollama.ai)
2. Start Ollama service (it runs on `http://localhost:11434` by default)
3. Pull a lightweight model optimized for personal PCs:
   - **Recommended**: `ollama pull phi3:mini` (3.8GB) - Best balance of speed and capability
   - **Ultra lightweight**: `ollama pull tinyllama` (637MB) - Fastest, minimal RAM usage
   - **Alternative**: `ollama pull qwen2:1.5b` (934MB) - Good for general tasks

## Setup

1. Navigate to the demo directory:
   ```bash
   cd LLMProviderAbstraction.Demo
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

## Configuring AWS Credentials

The demo supports three methods for providing AWS credentials (in priority order):

### Method 1: Environment Variables (Recommended - Most Secure)

```bash
# Linux/macOS
export AWS_ACCESS_KEY_ID="your_access_key_id"
export AWS_SECRET_ACCESS_KEY="your_secret_access_key"
export AWS_REGION="us-east-1"

# Windows PowerShell
$env:AWS_ACCESS_KEY_ID="your_access_key_id"
$env:AWS_SECRET_ACCESS_KEY="your_secret_access_key"
$env:AWS_REGION="us-east-1"

# Windows Command Prompt
set AWS_ACCESS_KEY_ID=your_access_key_id
set AWS_SECRET_ACCESS_KEY=your_secret_access_key
set AWS_REGION=us-east-1
```

### Method 2: appsettings.json File

1. Copy the example file:
   ```bash
   cp appsettings.example.json appsettings.json
   ```

2. Edit `appsettings.json` and add your credentials:
   ```json
   {
     "Aws": {
       "AccessKeyId": "your_access_key_id",
       "SecretAccessKey": "your_secret_access_key",
       "Region": "us-east-1"
     }
   }
   ```

**Note:** The `appsettings.json` file is gitignored to prevent accidentally committing credentials.

### Method 3: Hardcoded Values (Not Recommended)

Edit `Program.cs` and modify the `GetBedrockConfiguration()` method. This is not recommended for production use.

## Running the Demos

The `Program.cs` file contains 4 different demos. By default, Demo 3 (Bedrock Provider - Context Analysis) is enabled.

### Demo 3: Bedrock Provider - Context-based Analysis (Default)
Demonstrates using AWS Bedrock (Claude) to analyze text.

**To run:**
1. Configure AWS credentials using one of the methods above
2. Run: `dotnet run`

### Demo 1: Local Provider - Context-based Analysis
Demonstrates using Ollama to analyze text with a specific question.

**To run:**
1. Ensure Ollama is running locally
2. In `Program.cs`, comment out Demo 3 and uncomment Demo 1
3. Run: `dotnet run`

### Demo 2: Local Provider - Chatbot Session
Demonstrates maintaining conversation context across multiple messages with Ollama.

**To run:**
1. Ensure Ollama is running locally
2. In `Program.cs`, comment out Demo 3 and uncomment Demo 2
3. Run: `dotnet run`

### Demo 4: Bedrock Provider - Chatbot Session
Demonstrates maintaining conversation context with AWS Bedrock.

**To run:**
1. Configure AWS credentials using one of the methods above
2. In `Program.cs`, comment out Demo 3 and uncomment Demo 4
3. Run: `dotnet run`

## Code Structure

Each demo follows the same pattern:

1. **Configure the Provider**: Create a `LLMConfiguration` object with provider-specific settings
2. **Validate Configuration**: Ensure all required settings are present
3. **Create Provider Instance**: Instantiate either `LocalProvider` or `BedrockProvider`
4. **Validate Connectivity**: Test connection to the LLM service
5. **Send Requests**: Use either `AnalyzeAsync()` for context-based analysis or `SendMessageAsync()` for chatbot sessions
6. **Handle Responses**: Check for success and display results or errors

## Key Concepts

### Choosing the Right Model for Your PC

The demo uses lightweight models optimized for personal computers:

| Model | Size | RAM Required | Speed | Best For |
|-------|------|--------------|-------|----------|
| **phi3:mini** | 3.8GB | 8GB+ | Fast | Recommended default - great balance |
| **tinyllama** | 637MB | 4GB+ | Very Fast | Low-spec PCs, quick testing |
| **qwen2:1.5b** | 934MB | 6GB+ | Fast | General tasks, good efficiency |

**PC Specifications Guide:**
- **8GB RAM PC**: Use phi3:mini or qwen2:1.5b - excellent performance
- **4-6GB RAM PC**: Use tinyllama - still capable, very responsive
- **16GB+ RAM PC**: Any model works great, phi3:mini recommended for speed

**Why not larger models like llama2 (7GB)?**
- Larger models require more RAM and are slower on personal PCs
- The lightweight models above are specifically optimized for efficiency
- They provide fast response times while demonstrating all library features
- Perfect for development, testing, and learning

### Context-based Analysis
Use `AnalyzeAsync()` when you want to:
- Analyze a piece of text with a specific question
- Perform one-off queries without maintaining conversation history
- Process independent requests

```csharp
var response = await provider.AnalyzeAsync(context, prompt);
```

### Chatbot Sessions
Use `SendMessageAsync()` when you want to:
- Maintain conversation context across multiple messages
- Build interactive chatbots
- Have the LLM remember previous exchanges

```csharp
var session = sessionManager.CreateSession();
var response = await provider.SendMessageAsync(session, message);
```

## Configuration Options

### LLMConfiguration Properties

| Property | Required For | Description |
|----------|-------------|-------------|
| `ProviderType` | All | `ProviderType.Local` or `ProviderType.Bedrock` |
| `ModelIdentifier` | All | Model name (e.g., "phi3:mini" for local, "anthropic.claude-3-sonnet-20240229-v1:0" for Bedrock) |
| `Endpoint` | Local | HTTP endpoint for local LLM (e.g., "http://localhost:11434") |
| `AccessKey` | Bedrock | AWS access key |
| `SecretKey` | Bedrock | AWS secret key |
| `Region` | Bedrock | AWS region (e.g., "us-east-1") |
| `TimeoutSeconds` | All | Request timeout (default: 30) |
| `MaxRetries` | All | Retry attempts for transient errors (default: 3) |

## Error Handling

The library provides comprehensive error handling with specific error types:

- `ConnectionError`: Network connectivity issues
- `AuthenticationError`: Invalid credentials
- `ValidationError`: Invalid request parameters
- `RateLimitError`: API rate limits exceeded
- `ProviderError`: Provider-specific errors
- `UnknownError`: Unexpected errors

Always check `response.Success` before accessing `response.Content`:

```csharp
if (response.Success)
{
    Console.WriteLine(response.Content);
}
else
{
    Console.WriteLine($"Error: {response.Error?.Message}");
}
```

## Security Best Practices

### Credential Management
1. **Use Environment Variables**: Most secure method for production environments
   - Credentials are never stored in files
   - Easy to rotate without code changes
   - Works well with CI/CD pipelines

2. **Use appsettings.json for Development**: Good for local development
   - Ensure the file is gitignored (already configured)
   - Never commit credentials to version control
   - Use `appsettings.example.json` as a template

3. **Never Hardcode Credentials**: Avoid hardcoding in source code
   - Makes credential rotation difficult
   - Risk of accidental exposure in version control
   - Use only for quick testing, never for production

### AWS Security
1. **Principle of Least Privilege**: Grant only necessary IAM permissions
2. **Rotate Keys Regularly**: Change AWS access keys periodically
3. **Use IAM Roles**: In production AWS environments, use IAM roles instead of access keys
4. **Enable MFA**: Protect your AWS account with multi-factor authentication
5. **Monitor Usage**: Use AWS CloudTrail to monitor Bedrock API usage

### File Security
- The `.gitignore` is configured to exclude `appsettings.json`
- Always verify credentials are not committed before pushing code
- Use `appsettings.example.json` to share configuration structure without secrets

## Troubleshooting

### Bedrock Provider Issues

**"No AWS credentials found" warning:**
- Configure credentials using environment variables (recommended)
- Or create `appsettings.json` from `appsettings.example.json`
- See "Configuring AWS Credentials" section above

**"Authentication failed" error:**
- Verify AWS credentials are correct
- Check IAM permissions for Bedrock service
- Ensure credentials have not expired

**"Model not found" error:**
- Ensure model access is enabled in AWS Bedrock console
- Verify the region supports the requested model
- Check the model identifier is correct

### Local Provider Issues

**"Connection refused" error:**
- Ensure Ollama is running: `ollama serve`
- Check if the endpoint is correct (default: http://localhost:11434)

**"Model not found" error:**
- Pull the recommended lightweight model: `ollama pull phi3:mini`
- Or try an ultra-lightweight alternative: `ollama pull tinyllama`
- Verify the model name matches what's installed: `ollama list`

## Next Steps

- Explore different models available in Ollama or Bedrock
- Implement error handling and retry logic in your applications
- Build custom chatbots with session management
- Integrate the library into your .NET applications

## Additional Resources

- [Ollama Documentation](https://github.com/ollama/ollama)
- [AWS Bedrock Documentation](https://docs.aws.amazon.com/bedrock/)
- [LLMProviderAbstraction Library](../LLMProviderAbstraction/)

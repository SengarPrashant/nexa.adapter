# Quick Start Guide

Get up and running with the LLMProviderAbstraction demo in 5 minutes!

## Option 1: AWS Bedrock Provider (Default)

The demo now runs AWS Bedrock by default. You can configure credentials using three methods.

### Method 1: Environment Variables (Recommended - Most Secure)

```bash
# Set environment variables (Linux/macOS)
export AWS_ACCESS_KEY_ID="your_access_key_id"
export AWS_SECRET_ACCESS_KEY="your_secret_access_key"
export AWS_REGION="us-east-1"

# Set environment variables (Windows PowerShell)
$env:AWS_ACCESS_KEY_ID="your_access_key_id"
$env:AWS_SECRET_ACCESS_KEY="your_secret_access_key"
$env:AWS_REGION="us-east-1"

# Set environment variables (Windows Command Prompt)
set AWS_ACCESS_KEY_ID=your_access_key_id
set AWS_SECRET_ACCESS_KEY=your_secret_access_key
set AWS_REGION=us-east-1

# Run the demo
cd LLMProviderAbstraction.Demo
dotnet run
```

### Method 2: appsettings.json File

```bash
# Copy the example file
cp appsettings.example.json appsettings.json

# Edit appsettings.json and add your credentials
# {
#   "Aws": {
#     "AccessKeyId": "your_access_key_id",
#     "SecretAccessKey": "your_secret_access_key",
#     "Region": "us-east-1"
#   }
# }

# Run the demo
dotnet run
```

**Note:** The appsettings.json file is gitignored to prevent accidentally committing credentials.

### Method 3: Hardcoded Values (Not Recommended)

Edit `Program.cs` and modify the `GetBedrockConfiguration()` method to return hardcoded values. This is not recommended for production use.

### Prerequisites for Bedrock

1. **AWS Account** with Bedrock access
2. **AWS Credentials**: Access Key ID and Secret Access Key
3. **IAM Permissions**: Ensure your IAM user has permissions for Bedrock service
4. **Model Access**: Enable model access in AWS Bedrock console
   - Go to AWS Bedrock Console → Model access
   - Request access to Claude 3 Sonnet (or another model)
   - Wait for approval (usually instant)

### Run the Demo

```bash
cd LLMProviderAbstraction.Demo
dotnet run
```

You should see the demo analyze product information using AWS Bedrock!

## Option 2: Local Provider (Ollama)

### Step 1: Install Ollama
```bash
# Visit https://ollama.ai and download the installer for your OS
# Or use package managers:
# macOS: brew install ollama
# Linux: curl -fsSL https://ollama.ai/install.sh | sh
```

### Step 2: Start Ollama and Pull a Lightweight Model
```bash
# Start Ollama (it will run in the background)
ollama serve

# In a new terminal, pull a lightweight model optimized for personal PCs
# Ultra lightweight (637MB) - fastest option 
ollama pull tinyllama

# Alternative lightweight options:
# ollama pull phi3:mini    # phi3:mini(3.8GB) - Microsoft's small but capable model
# ollama pull qwen2:1.5b   # Good balance (934MB) - efficient and capable
```

**Why these models?**
- **phi3:mini** (3.8GB): Microsoft's efficient model, great balance of speed and capability
- **tinyllama** (637MB): Ultra lightweight, perfect for low-spec PCs or quick testing
- **qwen2:1.5b** (934MB): Alibaba's efficient model, good for general tasks

### Step 3: Enable Local Provider in Program.cs

Edit `Program.cs` and change the default demo:
```csharp
// Comment out Bedrock demo
// await RunBedrockProviderContextAnalysisDemo();

// Uncomment Local provider demo
await RunLocalProviderContextAnalysisDemo();
```

### Step 4: Run the Demo
```bash
cd LLMProviderAbstraction.Demo
dotnet run
```

That's it! You should see the demo analyze text using your local LLM.

## Switching Between Demos

In `Program.cs`, you'll find 4 demo methods. The default is Demo 3 (Bedrock Context Analysis). Simply comment/uncomment the one you want to run:

```csharp
// Demo 3: Bedrock Provider - Context Analysis (default)
await RunBedrockProviderContextAnalysisDemo();

// Demo 1: Local Provider - Context Analysis
// await RunLocalProviderContextAnalysisDemo();

// Demo 2: Local Provider - Chatbot
// await RunLocalProviderChatbotDemo();

// Demo 4: Bedrock Provider - Chatbot
// await RunBedrockProviderChatbotDemo();
```

## What Each Demo Does

### Demo 1 & 3: Context-based Analysis
- Sends a piece of text (context) with a question (prompt)
- Gets a single response
- Good for: Document analysis, Q&A, text summarization

### Demo 2 & 4: Chatbot Session
- Maintains conversation history
- Sends multiple messages in sequence
- The LLM remembers previous exchanges
- Good for: Interactive chatbots, multi-turn conversations

## Troubleshooting

### Bedrock Provider Issues

**"No AWS credentials found" warning:**
- Set environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
- Or configure `appsettings.json` with your credentials
- See configuration methods above

**"Authentication failed" error:**
- Verify AWS credentials are correct
- Check IAM permissions for Bedrock service
- Ensure credentials have not expired

**"Model not found" error:**
- Ensure model access is enabled in AWS Bedrock console
- Verify the region supports the requested model
- Check the model identifier is correct

### Local Provider Issues

**"Connection refused" (Local Provider)
```bash
# Make sure Ollama is running
ollama serve
```

**"Model not found" (Local Provider)
```bash
# List available models
ollama list

# Pull the recommended lightweight model
ollama pull phi3:mini

# Or try an ultra-lightweight alternative
ollama pull tinyllama
```

## Security Best Practices

1. **Use Environment Variables**: Most secure method, credentials never stored in files
2. **Use appsettings.json**: Good for development, ensure file is gitignored
3. **Never Commit Credentials**: The .gitignore is configured to exclude appsettings.json
4. **Rotate Keys Regularly**: Change AWS access keys periodically
5. **Use IAM Roles**: In production, use IAM roles instead of access keys when possible

## Next Steps

Once you have the demo running:

1. Try different prompts and contexts
2. Experiment with different models
3. Modify the code to build your own applications
4. Check out the full README.md for more details

## Need Help?

- Check the detailed [README.md](README.md)
- Review the code comments in [Program.cs](Program.cs)
- Consult the library documentation

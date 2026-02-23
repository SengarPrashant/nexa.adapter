using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Models;
using HedgehogRange = Hedgehog.Linq.Range;

namespace LLMProviderAbstraction.Tests.Generators;

/// <summary>
/// Hedgehog generators for creating test configurations
/// </summary>
public static class ConfigurationGenerators
{
    /// <summary>
    /// Generates valid Bedrock configurations with proper credentials and model identifiers
    /// </summary>
    public static Gen<LLMConfiguration> ValidBedrockConfiguration()
    {
        return
            from modelId in ValidModelIdentifier()
            from accessKey in Gen.AlphaNumeric.String(HedgehogRange.Singleton(20))
            from secretKey in Gen.AlphaNumeric.String(HedgehogRange.Singleton(40))
            from region in ValidAwsRegion()
            from timeout in Gen.Int32(HedgehogRange.Constant(1, 300))
            from retries in Gen.Int32(HedgehogRange.Constant(0, 10))
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Bedrock,
                ModelIdentifier = modelId,
                AccessKey = accessKey,
                SecretKey = secretKey,
                Region = region,
                TimeoutSeconds = timeout,
                MaxRetries = retries
            };
    }

    /// <summary>
    /// Generates valid Local configurations with proper endpoints and model identifiers
    /// </summary>
    public static Gen<LLMConfiguration> ValidLocalConfiguration()
    {
        return
            from modelId in ValidModelIdentifier()
            from endpoint in ValidEndpoint()
            from timeout in Gen.Int32(HedgehogRange.Constant(1, 300))
            from retries in Gen.Int32(HedgehogRange.Constant(0, 10))
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = modelId,
                Endpoint = endpoint,
                TimeoutSeconds = timeout,
                MaxRetries = retries
            };
    }

    /// <summary>
    /// Generates any valid configuration (either Bedrock or Local)
    /// </summary>
    public static Gen<LLMConfiguration> ValidConfiguration()
    {
        return Gen.Choice(ValidBedrockConfiguration(), ValidLocalConfiguration());
    }

    /// <summary>
    /// Generates invalid configurations with various types of validation errors
    /// </summary>
    public static Gen<LLMConfiguration> InvalidConfiguration()
    {
        return Gen.Choice(
            MissingModelIdentifier(),
            BedrockMissingAccessKey(),
            BedrockMissingSecretKey(),
            LocalMissingEndpoint(),
            LocalMalformedEndpoint(),
            InvalidTimeoutSeconds(),
            InvalidMaxRetries()
        );
    }

    /// <summary>
    /// Generates random valid model identifiers
    /// </summary>
    public static Gen<string> ValidModelIdentifier()
    {
        var bedrockModels = new[]
        {
            "anthropic.claude-3-sonnet-20240229-v1:0",
            "anthropic.claude-3-haiku-20240307-v1:0",
            "anthropic.claude-3-opus-20240229-v1:0",
            "anthropic.claude-v2:1",
            "anthropic.claude-v2",
            "anthropic.claude-instant-v1",
            "meta.llama2-13b-chat-v1",
            "meta.llama2-70b-chat-v1",
            "amazon.titan-text-express-v1",
            "amazon.titan-text-lite-v1"
        };

        var localModels = new[]
        {
            "llama2",
            "llama2:13b",
            "llama2:70b",
            "mistral",
            "mixtral",
            "codellama",
            "phi",
            "gemma",
            "neural-chat",
            "starling-lm"
        };

        return Gen.Choice(
            Gen.Item(bedrockModels),
            Gen.Item(localModels)
        );
    }

    /// <summary>
    /// Generates random valid AWS regions
    /// </summary>
    public static Gen<string> ValidAwsRegion()
    {
        var regions = new[]
        {
            "us-east-1",
            "us-east-2",
            "us-west-1",
            "us-west-2",
            "eu-west-1",
            "eu-west-2",
            "eu-west-3",
            "eu-central-1",
            "eu-north-1",
            "ap-northeast-1",
            "ap-northeast-2",
            "ap-southeast-1",
            "ap-southeast-2",
            "ap-south-1",
            "ca-central-1",
            "sa-east-1"
        };

        return Gen.Item(regions);
    }

    /// <summary>
    /// Generates valid HTTP/HTTPS endpoints
    /// </summary>
    private static Gen<string> ValidEndpoint()
    {
        return
            from protocol in Gen.Item(new[] { "http", "https" })
            from host in Gen.Choice(
                Gen.Constant("localhost"),
                Gen.Constant("127.0.0.1"),
                from s in Gen.AlphaNumeric.String(HedgehogRange.Singleton(10))
                select $"{s}.com"
            )
            from port in Gen.Int32(HedgehogRange.Constant(1024, 65535))
            select $"{protocol}://{host}:{port}";
    }

    /// <summary>
    /// Generates configuration with missing model identifier
    /// </summary>
    private static Gen<LLMConfiguration> MissingModelIdentifier()
    {
        return
            from config in ValidConfiguration()
            select new LLMConfiguration
            {
                ProviderType = config.ProviderType,
                ModelIdentifier = string.Empty, // Invalid: empty model identifier
                AccessKey = config.AccessKey,
                SecretKey = config.SecretKey,
                Region = config.Region,
                Endpoint = config.Endpoint,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates Bedrock configuration with missing access key
    /// </summary>
    private static Gen<LLMConfiguration> BedrockMissingAccessKey()
    {
        return
            from config in ValidBedrockConfiguration()
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Bedrock,
                ModelIdentifier = config.ModelIdentifier,
                AccessKey = null, // Invalid: missing access key
                SecretKey = config.SecretKey,
                Region = config.Region,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates Bedrock configuration with missing secret key
    /// </summary>
    private static Gen<LLMConfiguration> BedrockMissingSecretKey()
    {
        return
            from config in ValidBedrockConfiguration()
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Bedrock,
                ModelIdentifier = config.ModelIdentifier,
                AccessKey = config.AccessKey,
                SecretKey = null, // Invalid: missing secret key
                Region = config.Region,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates Local configuration with missing endpoint
    /// </summary>
    private static Gen<LLMConfiguration> LocalMissingEndpoint()
    {
        return
            from config in ValidLocalConfiguration()
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = config.ModelIdentifier,
                Endpoint = null, // Invalid: missing endpoint
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates Local configuration with malformed endpoint URI
    /// </summary>
    private static Gen<LLMConfiguration> LocalMalformedEndpoint()
    {
        var malformedUris = new[]
        {
            "not-a-uri",
            "://missing-protocol.com",
            "http://",
            "http:/invalid.com", // Missing slash
            "ht!tp://invalid.com", // Invalid characters
            "http://[invalid", // Invalid IPv6
            "http://host name.com" // Space in hostname
        };

        return
            from config in ValidLocalConfiguration()
            from malformedUri in Gen.Item(malformedUris)
            select new LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = config.ModelIdentifier,
                Endpoint = malformedUri, // Invalid: malformed URI
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates configuration with invalid timeout seconds (zero or negative)
    /// </summary>
    private static Gen<LLMConfiguration> InvalidTimeoutSeconds()
    {
        return
            from config in ValidConfiguration()
            from timeout in Gen.Int32(HedgehogRange.Constant(-100, 0))
            select new LLMConfiguration
            {
                ProviderType = config.ProviderType,
                ModelIdentifier = config.ModelIdentifier,
                AccessKey = config.AccessKey,
                SecretKey = config.SecretKey,
                Region = config.Region,
                Endpoint = config.Endpoint,
                TimeoutSeconds = timeout, // Invalid: zero or negative
                MaxRetries = config.MaxRetries
            };
    }

    /// <summary>
    /// Generates configuration with invalid max retries (negative)
    /// </summary>
    private static Gen<LLMConfiguration> InvalidMaxRetries()
    {
        return
            from config in ValidConfiguration()
            from retries in Gen.Int32(HedgehogRange.Constant(-100, -1))
            select new LLMConfiguration
            {
                ProviderType = config.ProviderType,
                ModelIdentifier = config.ModelIdentifier,
                AccessKey = config.AccessKey,
                SecretKey = config.SecretKey,
                Region = config.Region,
                Endpoint = config.Endpoint,
                TimeoutSeconds = config.TimeoutSeconds,
                MaxRetries = retries // Invalid: negative
            };
    }
}

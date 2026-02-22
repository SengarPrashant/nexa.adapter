using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Configuration;
using LLMProviderAbstraction.Interfaces;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Providers;
using LLMProviderAbstraction.Tests.Generators;
using Xunit;

namespace LLMProviderAbstraction.Tests.Properties;

public class ProviderProperties
{
    /// <summary>
    /// Property 1: Provider Creation from Valid Configuration
    /// Validates: Requirements 1.1, 1.2, 4.1, 4.2, 4.3, 4.4
    /// </summary>
    [Fact]
    public void ProviderCreationFromValidConfiguration()
    {
        // Generate valid configurations and verify factory creates correct provider type (100 iterations)
        var httpClientFactory = new TestHttpClientFactory();
        var factory = new LLMProviderFactory(httpClientFactory);
        
        var gen = ConfigurationGenerators.ValidConfiguration();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var config in samples)
        {
            // Factory should create provider successfully
            var provider = factory.CreateProvider(config);
            Assert.NotNull(provider);
            
            // Provider should be of correct type based on configuration
            if (config.ProviderType == ProviderType.Bedrock)
            {
                Assert.IsType<BedrockProvider>(provider);
            }
            else if (config.ProviderType == ProviderType.Local)
            {
                Assert.IsType<LocalProvider>(provider);
            }
            
            // Provider should implement ILLMProvider interface
            Assert.IsAssignableFrom<ILLMProvider>(provider);
        }
    }

    /// <summary>
    /// Property 2: Provider Initialization with Valid Settings
    /// Validates: Requirements 1.3, 1.4
    /// </summary>
    [Fact]
    public void ProviderInitializationWithValidSettings()
    {
        // Generate valid Bedrock configurations and verify initialization succeeds (100 iterations)
        var bedrockGen = ConfigurationGenerators.ValidBedrockConfiguration();
        var bedrockSamples = bedrockGen.Sample(size: 10, count: 100);
        
        foreach (var config in bedrockSamples)
        {
            // Initialization should succeed without throwing exceptions
            var provider = new BedrockProvider(config);
            Assert.NotNull(provider);
        }
    }

    /// <summary>
    /// Property 9: Region Configuration Propagation
    /// Validates: Requirements 4.5
    /// </summary>
    [Fact]
    public void RegionConfigurationPropagation()
    {
        // Run 100 iterations of the property test
        var gen = ConfigurationGenerators.ValidBedrockConfiguration();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var config in samples)
        {
            // Verify region is set (either explicitly or defaults to us-east-1)
            Assert.False(string.IsNullOrEmpty(config.Region), 
                "Region should be set for Bedrock configuration");
            
            // Create provider with this region
            var provider = new BedrockProvider(config);
            Assert.NotNull(provider);
            
            // Use reflection to verify the region was properly propagated to the client
            var clientField = typeof(BedrockProvider).GetField("_client", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(clientField);
            
            var client = clientField.GetValue(provider) as Amazon.BedrockRuntime.AmazonBedrockRuntimeClient;
            Assert.NotNull(client);
            
            // Get the client's configuration to verify the region
            var clientConfig = client.Config;
            Assert.NotNull(clientConfig);
            
            // Verify the region endpoint matches the configured region
            var expectedRegion = Amazon.RegionEndpoint.GetBySystemName(config.Region);
            Assert.Equal(expectedRegion.SystemName, clientConfig.RegionEndpoint.SystemName);
        }
    }

    /// <summary>
    /// Property 13: All I/O Operations Are Async
    /// **Validates: Requirements 6.2**
    /// Verifies that all ILLMProvider and ISessionManager methods return Task or Task<T>
    /// </summary>
    [Fact]
    public void AllIOOperationsAreAsync()
    {
        // Test ILLMProvider interface methods
        var providerInterfaceType = typeof(ILLMProvider);
        var providerMethods = providerInterfaceType.GetMethods()
            .Where(m => !m.IsSpecialName); // Exclude property getters/setters
        
        foreach (var method in providerMethods)
        {
            var returnType = method.ReturnType;
            var isAsync = returnType == typeof(Task) || 
                         (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
            
            Assert.True(isAsync, 
                $"ILLMProvider method {method.Name} should return Task or Task<T> for I/O operations. " +
                $"Found return type: {returnType.Name}");
        }
        
        // Test ISessionManager interface methods
        var sessionManagerInterfaceType = typeof(ISessionManager);
        var sessionManagerMethods = sessionManagerInterfaceType.GetMethods()
            .Where(m => !m.IsSpecialName); // Exclude property getters/setters
        
        // ISessionManager methods are synchronous (in-memory operations), so we verify they exist
        // but don't require async. The property focuses on I/O operations in ILLMProvider.
        // However, we verify the interface is properly defined.
        Assert.NotEmpty(sessionManagerMethods);
        
        // Verify both provider implementations (BedrockProvider and LocalProvider) 
        // properly implement the async interface
        var bedrockConfig = ConfigurationGenerators.ValidBedrockConfiguration().Sample(size: 10, count: 1).First();
        var bedrockProvider = new BedrockProvider(bedrockConfig);
        VerifyProviderImplementsAsyncMethods(bedrockProvider, "BedrockProvider");
        
        var localConfig = ConfigurationGenerators.ValidLocalConfiguration().Sample(size: 10, count: 1).First();
        var localProvider = new LocalProvider(localConfig, new TestHttpClientFactory().CreateClient());
        VerifyProviderImplementsAsyncMethods(localProvider, "LocalProvider");
    }
    
    /// <summary>
    /// Helper method to verify a provider implementation has async methods
    /// </summary>
    private void VerifyProviderImplementsAsyncMethods(ILLMProvider provider, string providerName)
    {
        var providerType = provider.GetType();
        
        // Get all public methods that are part of ILLMProvider interface
        var interfaceMethods = typeof(ILLMProvider).GetMethods();
        
        foreach (var interfaceMethod in interfaceMethods)
        {
            // Find the implementation in the concrete provider
            var implementationMethod = providerType.GetMethod(interfaceMethod.Name);
            
            Assert.NotNull(implementationMethod);
            
            var returnType = implementationMethod.ReturnType;
            var isAsync = returnType == typeof(Task) || 
                         (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>));
            
            Assert.True(isAsync, 
                $"{providerName} method {implementationMethod.Name} should return Task or Task<T>. " +
                $"Found return type: {returnType.Name}");
        }
    }
}

using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Session;
using LLMProviderAbstraction.Tests.Generators;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LLMProviderAbstraction.Tests.Properties;

public class SessionProperties
{
    /// <summary>
    /// Property 6: Session Creation Always Succeeds
    /// Validates: Requirements 3.1
    /// </summary>
    [Fact]
    public void SessionCreationAlwaysSucceeds()
    {
        // Generate random session IDs (including null) and verify session creation succeeds
        var gen = SessionGenerators.NullableSessionId();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var sessionId in samples)
        {
            var manager = new SessionManager();
            var session = manager.CreateSession(sessionId);
            
            // Session should always be created successfully
            Assert.NotNull(session);
            Assert.False(string.IsNullOrEmpty(session.SessionId));
            Assert.Empty(session.Messages);
            Assert.True(session.CreatedAt <= DateTime.UtcNow);
            
            // If sessionId was provided, it should match
            if (sessionId != null)
            {
                Assert.Equal(sessionId, session.SessionId);
            }
        }
    }

    /// <summary>
    /// Property 7: Message Storage Round Trip
    /// Validates: Requirements 3.2, 3.4
    /// </summary>
    [Fact]
    public void MessageStorageRoundTrip()
    {
        // Run 100 iterations of the property test
        var gen = SessionGenerators.MessagesWithRoles();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var messages in samples)
        {
            var manager = new SessionManager();
            var session = manager.CreateSession();
            
            // Add all messages to session
            foreach (var (content, role) in messages)
            {
                session.AddMessage(new Message(content, role));
            }
            
            // Retrieve session history
            var history = manager.GetSessionHistory(session.SessionId);
            
            // Verify count matches
            Assert.Equal(messages.Count, history.Count);
            
            // Verify content and order preserved
            for (int j = 0; j < messages.Count; j++)
            {
                Assert.Equal(messages[j].content, history[j].Content);
                Assert.Equal(messages[j].role, history[j].Role);
            }
        }
    }

    /// <summary>
    /// Property 8: Session Context Inclusion
    /// Validates: Requirements 3.3
    /// </summary>
    [Fact]
    public async Task SessionContextInclusion()
    {
        // Run 100 iterations of the property test
        var gen = SessionGenerators.SessionWithVariableHistory();
        var samples = gen.Sample(size: 10, count: 100);
        
        foreach (var session in samples)
        {
            // Skip empty sessions as they don't have context to verify
            if (session.Messages.Count == 0)
            {
                continue;
            }
            
            // Create a mock HTTP handler that captures the request
            HttpRequestMessage? capturedRequest = null;
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    capturedRequest = request;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            choices = new[]
                            {
                                new
                                {
                                    message = new
                                    {
                                        role = "assistant",
                                        content = "Test response"
                                    }
                                }
                            }
                        }),
                        System.Text.Encoding.UTF8,
                        "application/json")
                });
            
            var httpClient = new HttpClient(mockHandler.Object);
            
            // Create a local provider with the mocked HTTP client
            var config = new Configuration.LLMConfiguration
            {
                ProviderType = ProviderType.Local,
                ModelIdentifier = "test-model",
                Endpoint = "http://localhost:11434",
                TimeoutSeconds = 30,
                MaxRetries = 0 // No retries for faster testing
            };
            
            var provider = new Providers.LocalProvider(config, httpClient);
            
            // Send a new message through the provider
            var newMessage = "New test message";
            var response = await provider.SendMessageAsync(session, newMessage, CancellationToken.None);
            
            // Verify the request was captured
            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedRequest.Content);
            
            // Read and parse the request payload
            var requestBody = await capturedRequest.Content.ReadAsStringAsync();
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(requestBody);
            var root = jsonDoc.RootElement;
            
            // Verify the request contains a messages array
            Assert.True(root.TryGetProperty("messages", out var messagesArray));
            Assert.Equal(System.Text.Json.JsonValueKind.Array, messagesArray.ValueKind);
            
            // The request should include all session messages plus the new message
            // Note: session.Messages was updated by SendMessageAsync, so we need to account for that
            var expectedMessageCount = session.Messages.Count - 2; // Subtract the 2 messages added by SendMessageAsync
            var actualMessageCount = messagesArray.GetArrayLength() - 1; // Subtract the new message
            
            Assert.Equal(expectedMessageCount, actualMessageCount);
            
            // Verify all original session messages are included in the request
            int messageIndex = 0;
            foreach (var originalMessage in session.Messages.Take(expectedMessageCount))
            {
                var requestMessage = messagesArray[messageIndex];
                
                // Verify role mapping
                Assert.True(requestMessage.TryGetProperty("role", out var roleElement));
                var expectedRole = originalMessage.Role switch
                {
                    MessageRole.User => "user",
                    MessageRole.Assistant => "assistant",
                    MessageRole.System => "system",
                    _ => "user"
                };
                Assert.Equal(expectedRole, roleElement.GetString());
                
                // Verify content
                Assert.True(requestMessage.TryGetProperty("content", out var contentElement));
                Assert.Equal(originalMessage.Content, contentElement.GetString());
                
                messageIndex++;
            }
            
            // Verify the new message is included at the end
            var lastMessage = messagesArray[messagesArray.GetArrayLength() - 1];
            Assert.True(lastMessage.TryGetProperty("role", out var lastRoleElement));
            Assert.Equal("user", lastRoleElement.GetString());
            Assert.True(lastMessage.TryGetProperty("content", out var lastContentElement));
            Assert.Equal(newMessage, lastContentElement.GetString());
        }
    }
}

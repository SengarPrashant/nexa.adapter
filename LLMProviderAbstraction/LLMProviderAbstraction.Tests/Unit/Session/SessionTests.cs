using FluentAssertions;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Session;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Session;

/// <summary>
/// Unit tests for the Session class
/// </summary>
public class SessionTests
{
    [Fact]
    public void Constructor_WithSessionId_CreatesSessionWithId()
    {
        // Arrange
        var sessionId = "test-session-123";

        // Act
        var session = new LLMProviderAbstraction.Session.Session(sessionId);

        // Assert
        session.SessionId.Should().Be(sessionId);
        session.Messages.Should().BeEmpty();
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.LastAccessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithGuidSessionId_CreatesSessionWithGuid()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var session = new LLMProviderAbstraction.Session.Session(sessionId);

        // Assert
        session.SessionId.Should().Be(sessionId);
        Guid.TryParse(session.SessionId, out _).Should().BeTrue();
    }

    [Fact]
    public void AddMessage_WithUserMessage_AddsMessageToSession()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message = new Message("Hello, world!", MessageRole.User);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Should().Be(message);
        session.Messages[0].Content.Should().Be("Hello, world!");
        session.Messages[0].Role.Should().Be(MessageRole.User);
    }

    [Fact]
    public void AddMessage_WithAssistantMessage_AddsMessageToSession()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message = new Message("Hello! How can I help?", MessageRole.Assistant);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Role.Should().Be(MessageRole.Assistant);
    }

    [Fact]
    public void AddMessage_WithSystemMessage_AddsMessageToSession()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message = new Message("You are a helpful assistant", MessageRole.System);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Role.Should().Be(MessageRole.System);
    }

    [Fact]
    public void AddMessage_MultipleMessages_PreservesOrder()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message1 = new Message("First message", MessageRole.User);
        var message2 = new Message("Second message", MessageRole.Assistant);
        var message3 = new Message("Third message", MessageRole.User);

        // Act
        session.AddMessage(message1);
        session.AddMessage(message2);
        session.AddMessage(message3);

        // Assert
        session.Messages.Should().HaveCount(3);
        session.Messages[0].Content.Should().Be("First message");
        session.Messages[1].Content.Should().Be("Second message");
        session.Messages[2].Content.Should().Be("Third message");
    }

    [Fact]
    public void AddMessage_UpdatesLastAccessedAt()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var initialLastAccessed = session.LastAccessedAt;
        
        // Wait a small amount to ensure time difference
        Thread.Sleep(10);

        // Act
        var message = new Message("Test message", MessageRole.User);
        session.AddMessage(message);

        // Assert
        session.LastAccessedAt.Should().BeAfter(initialLastAccessed);
    }

    [Fact]
    public void Messages_ReturnsReadOnlyList()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message = new Message("Test", MessageRole.User);
        session.AddMessage(message);

        // Act
        var messages = session.Messages;

        // Assert
        messages.Should().BeAssignableTo<IReadOnlyList<Message>>();
    }

    [Fact]
    public void AddMessage_WithEmptyContent_AddsMessage()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var message = new Message(string.Empty, MessageRole.User);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Content.Should().BeEmpty();
    }

    [Fact]
    public void AddMessage_WithVeryLongContent_AddsMessage()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var longContent = new string('x', 100000);
        var message = new Message(longContent, MessageRole.User);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages.Should().HaveCount(1);
        session.Messages[0].Content.Length.Should().Be(100000);
    }

    [Fact]
    public void AddMessage_WithSpecialCharacters_PreservesContent()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var specialContent = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?\n\t\r";
        var message = new Message(specialContent, MessageRole.User);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages[0].Content.Should().Be(specialContent);
    }

    [Fact]
    public void AddMessage_WithUnicodeCharacters_PreservesContent()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var unicodeContent = "Hello 世界 🌍 مرحبا";
        var message = new Message(unicodeContent, MessageRole.User);

        // Act
        session.AddMessage(message);

        // Assert
        session.Messages[0].Content.Should().Be(unicodeContent);
    }

    [Fact]
    public void CreatedAt_DoesNotChange_WhenMessagesAdded()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var createdAt = session.CreatedAt;
        
        Thread.Sleep(10);

        // Act
        session.AddMessage(new Message("Test", MessageRole.User));

        // Assert
        session.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AddMessage_ManyMessages_AllStoredCorrectly()
    {
        // Arrange
        var session = new LLMProviderAbstraction.Session.Session("test-session");
        var messageCount = 100;

        // Act
        for (int i = 0; i < messageCount; i++)
        {
            var role = i % 2 == 0 ? MessageRole.User : MessageRole.Assistant;
            session.AddMessage(new Message($"Message {i}", role));
        }

        // Assert
        session.Messages.Should().HaveCount(messageCount);
        for (int i = 0; i < messageCount; i++)
        {
            session.Messages[i].Content.Should().Be($"Message {i}");
        }
    }
}

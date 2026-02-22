using FluentAssertions;
using LLMProviderAbstraction.Models;
using LLMProviderAbstraction.Session;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Session;

/// <summary>
/// Unit tests for the SessionManager class
/// </summary>
public class SessionManagerTests
{
    [Fact]
    public void CreateSession_WithoutId_CreatesSessionWithGeneratedGuid()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var session = manager.CreateSession();

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().NotBeNullOrEmpty();
        Guid.TryParse(session.SessionId, out _).Should().BeTrue();
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void CreateSession_WithSpecifiedId_CreatesSessionWithThatId()
    {
        // Arrange
        var manager = new SessionManager();
        var sessionId = "custom-session-id";

        // Act
        var session = manager.CreateSession(sessionId);

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().Be(sessionId);
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void CreateSession_WithGuidId_CreatesSessionWithGuid()
    {
        // Arrange
        var manager = new SessionManager();
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var session = manager.CreateSession(sessionId);

        // Assert
        session.SessionId.Should().Be(sessionId);
        Guid.TryParse(session.SessionId, out _).Should().BeTrue();
    }

    [Fact]
    public void CreateSession_MultipleTimes_CreatesUniqueSessions()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var session1 = manager.CreateSession();
        var session2 = manager.CreateSession();
        var session3 = manager.CreateSession();

        // Assert
        session1.SessionId.Should().NotBe(session2.SessionId);
        session2.SessionId.Should().NotBe(session3.SessionId);
        session1.SessionId.Should().NotBe(session3.SessionId);
    }

    [Fact]
    public void GetSession_WithExistingId_ReturnsSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("test-session");

        // Act
        var retrieved = manager.GetSession("test-session");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().Be(session);
        retrieved!.SessionId.Should().Be("test-session");
    }

    [Fact]
    public void GetSession_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var retrieved = manager.GetSession("non-existent-session");

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public void GetSession_AfterCreatingMultipleSessions_ReturnsCorrectSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session1 = manager.CreateSession("session-1");
        var session2 = manager.CreateSession("session-2");
        var session3 = manager.CreateSession("session-3");

        // Act
        var retrieved = manager.GetSession("session-2");

        // Assert
        retrieved.Should().Be(session2);
        retrieved!.SessionId.Should().Be("session-2");
    }

    [Fact]
    public void GetSessionHistory_WithExistingSession_ReturnsMessages()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("test-session");
        var message1 = new Message("Hello", MessageRole.User);
        var message2 = new Message("Hi there", MessageRole.Assistant);
        session.AddMessage(message1);
        session.AddMessage(message2);

        // Act
        var history = manager.GetSessionHistory("test-session");

        // Assert
        history.Should().HaveCount(2);
        history[0].Should().Be(message1);
        history[1].Should().Be(message2);
    }

    [Fact]
    public void GetSessionHistory_WithNonExistentSession_ReturnsEmptyList()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var history = manager.GetSessionHistory("non-existent-session");

        // Assert
        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public void GetSessionHistory_WithEmptySession_ReturnsEmptyList()
    {
        // Arrange
        var manager = new SessionManager();
        manager.CreateSession("empty-session");

        // Act
        var history = manager.GetSessionHistory("empty-session");

        // Assert
        history.Should().NotBeNull();
        history.Should().BeEmpty();
    }

    [Fact]
    public void GetSessionHistory_ReturnsReadOnlyList()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("test-session");
        session.AddMessage(new Message("Test", MessageRole.User));

        // Act
        var history = manager.GetSessionHistory("test-session");

        // Assert
        history.Should().BeAssignableTo<IReadOnlyList<Message>>();
    }

    [Fact]
    public void CreateSession_ThenAddMessages_MessagesRetrievableViaGetSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("test-session");

        // Act
        session.AddMessage(new Message("Message 1", MessageRole.User));
        session.AddMessage(new Message("Message 2", MessageRole.Assistant));
        var retrieved = manager.GetSession("test-session");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Messages.Should().HaveCount(2);
        retrieved.Messages[0].Content.Should().Be("Message 1");
        retrieved.Messages[1].Content.Should().Be("Message 2");
    }

    [Fact]
    public async Task ConcurrentSessionAccess_CreateMultipleSessions_AllSessionsAccessible()
    {
        // Arrange
        var manager = new SessionManager();
        var sessionIds = new List<string>();
        var tasks = new List<Task>();

        // Act - Create sessions concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var sessionId = $"concurrent-session-{index}";
                lock (sessionIds)
                {
                    sessionIds.Add(sessionId);
                }
                manager.CreateSession(sessionId);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All sessions should be retrievable
        foreach (var sessionId in sessionIds)
        {
            var session = manager.GetSession(sessionId);
            session.Should().NotBeNull();
            session!.SessionId.Should().Be(sessionId);
        }
    }

    [Fact]
    public async Task ConcurrentSessionAccess_AddMessagesToDifferentSessions_AllMessagesStored()
    {
        // Arrange
        var manager = new SessionManager();
        var tasks = new List<Task>();
        var sessionCount = 10;

        // Act - Add messages to different sessions concurrently
        for (int i = 0; i < sessionCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var session = manager.CreateSession($"concurrent-session-{index}");
                session.AddMessage(new Message($"Message in session {index}", MessageRole.User));
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All sessions should have their messages
        for (int i = 0; i < sessionCount; i++)
        {
            var session = manager.GetSession($"concurrent-session-{i}");
            session.Should().NotBeNull();
            session!.Messages.Should().HaveCount(1);
            session.Messages[0].Content.Should().Be($"Message in session {i}");
        }
    }

    [Fact]
    public async Task ConcurrentSessionAccess_ReadAndWriteSimultaneously_NoExceptions()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("read-write-test");
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Act - Read and write concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            // Write task
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    session.AddMessage(new Message($"Message {index}", MessageRole.User));
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));

            // Read task
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var history = manager.GetSessionHistory("read-write-test");
                    _ = history.Count;
                }
                catch (Exception ex)
                {
                    lock (exceptions) { exceptions.Add(ex); }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty();
        session.Messages.Should().HaveCount(10);
    }

    [Fact]
    public void CreateSession_WithSameIdTwice_ReturnsNewSessionButDoesNotOverwrite()
    {
        // Arrange
        var manager = new SessionManager();
        var sessionId = "duplicate-id";

        // Act
        var session1 = manager.CreateSession(sessionId);
        session1.AddMessage(new Message("First session message", MessageRole.User));
        
        var session2 = manager.CreateSession(sessionId);

        // Assert
        // The second CreateSession returns a new session object
        session2.Should().NotBe(session1);
        session2.SessionId.Should().Be(sessionId);
        
        // But the original session is still in the manager (ConcurrentDictionary.TryAdd behavior)
        var retrieved = manager.GetSession(sessionId);
        retrieved.Should().Be(session1);
        retrieved!.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void SessionManager_ManagesManySessionsEfficiently()
    {
        // Arrange
        var manager = new SessionManager();
        var sessionCount = 1000;

        // Act
        for (int i = 0; i < sessionCount; i++)
        {
            var session = manager.CreateSession($"session-{i}");
            session.AddMessage(new Message($"Message in session {i}", MessageRole.User));
        }

        // Assert
        for (int i = 0; i < sessionCount; i++)
        {
            var session = manager.GetSession($"session-{i}");
            session.Should().NotBeNull();
            session!.Messages.Should().HaveCount(1);
        }
    }

    [Fact]
    public void GetSessionHistory_AfterAddingMessages_ReflectsCurrentState()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession("test-session");

        // Act & Assert - Initially empty
        var history1 = manager.GetSessionHistory("test-session");
        history1.Should().BeEmpty();

        // Add first message
        session.AddMessage(new Message("First", MessageRole.User));
        var history2 = manager.GetSessionHistory("test-session");
        history2.Should().HaveCount(1);

        // Add second message
        session.AddMessage(new Message("Second", MessageRole.Assistant));
        var history3 = manager.GetSessionHistory("test-session");
        history3.Should().HaveCount(2);
    }
}

using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Session;
using HedgehogRange = Hedgehog.Linq.Range;

namespace LLMProviderAbstraction.Tests.Generators;

/// <summary>
/// Hedgehog generators for creating test sessions
/// </summary>
public static class SessionGenerators
{
    /// <summary>
    /// Generates random session IDs
    /// </summary>
    public static Gen<string> SessionId()
    {
        return Gen.Choice(
            GuidSessionId(),
            AlphanumericSessionId(),
            ShortSessionId(),
            LongSessionId()
        );
    }

    /// <summary>
    /// Generates random session IDs including null (for auto-generated IDs)
    /// </summary>
    public static Gen<string?> NullableSessionId()
    {
        return Gen.Choice(
            Gen.Constant<string?>(null),
            SessionId().Select(id => (string?)id)
        );
    }

    /// <summary>
    /// Generates sessions with various message history lengths (0-100 messages)
    /// </summary>
    public static Gen<Session.Session> SessionWithVariableHistory()
    {
        return
            from sessionId in SessionId()
            from messageCount in Gen.Int32(HedgehogRange.Constant(0, 100))
            from messages in MessageGenerators.RandomMessage().List(HedgehogRange.Singleton(messageCount))
            select CreateSessionWithMessages(sessionId, messages);
    }

    /// <summary>
    /// Generates sessions with mixed message roles
    /// </summary>
    public static Gen<Session.Session> SessionWithMixedRoles()
    {
        return
            from sessionId in SessionId()
            from messageCount in Gen.Int32(HedgehogRange.Constant(1, 50))
            from messages in MessageGenerators.RandomMessage().List(HedgehogRange.Singleton(messageCount))
            select CreateSessionWithMessages(sessionId, messages);
    }

    /// <summary>
    /// Generates an empty session (no messages)
    /// </summary>
    public static Gen<Session.Session> EmptySession()
    {
        return
            from sessionId in SessionId()
            select new Session.Session(sessionId);
    }

    /// <summary>
    /// Generates a session with a specific number of messages
    /// </summary>
    public static Gen<Session.Session> SessionWithMessageCount(int count)
    {
        return
            from sessionId in SessionId()
            from messages in MessageGenerators.RandomMessage().List(HedgehogRange.Singleton(count))
            select CreateSessionWithMessages(sessionId, messages);
    }

    /// <summary>
    /// Generates GUID-based session IDs
    /// </summary>
    private static Gen<string> GuidSessionId()
    {
        return Gen.Constant(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Generates alphanumeric session IDs (10-30 characters)
    /// </summary>
    private static Gen<string> AlphanumericSessionId()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(10, 30));
    }

    /// <summary>
    /// Generates short session IDs (5-10 characters)
    /// </summary>
    private static Gen<string> ShortSessionId()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(5, 10));
    }

    /// <summary>
    /// Generates long session IDs (50-100 characters)
    /// </summary>
    private static Gen<string> LongSessionId()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(50, 100));
    }

    /// <summary>
    /// Generates a list of message content and role tuples
    /// </summary>
    public static Gen<List<(string content, Models.MessageRole role)>> MessagesWithRoles()
    {
        return
            from count in Gen.Int32(HedgehogRange.Constant(0, 50))
            from messages in MessageContentAndRole().List(HedgehogRange.Singleton(count))
            select messages;
    }

    /// <summary>
    /// Generates a tuple of message content and role
    /// </summary>
    private static Gen<(string content, Models.MessageRole role)> MessageContentAndRole()
    {
        return
            from content in MessageGenerators.MessageContent()
            from role in MessageGenerators.MessageRoleGen()
            select (content, role);
    }

    /// <summary>
    /// Helper method to create a session and add messages to it
    /// </summary>
    private static Session.Session CreateSessionWithMessages(string sessionId, IEnumerable<Models.Message> messages)
    {
        var session = new Session.Session(sessionId);
        foreach (var message in messages)
        {
            session.AddMessage(message);
        }
        return session;
    }
}

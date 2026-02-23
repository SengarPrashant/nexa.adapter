using Hedgehog;
using Hedgehog.Linq;
using LLMProviderAbstraction.Models;
using HedgehogRange = Hedgehog.Linq.Range;

namespace LLMProviderAbstraction.Tests.Generators;

/// <summary>
/// Hedgehog generators for creating test messages
/// </summary>
public static class MessageGenerators
{
    /// <summary>
    /// Generates random message content with various lengths and character sets
    /// </summary>
    public static Gen<string> MessageContent()
    {
        return Gen.Choice(
            ShortMessageContent(),
            MediumMessageContent(),
            LongMessageContent(),
            SpecialCharacterContent(),
            UnicodeContent()
        );
    }

    /// <summary>
    /// Generates all possible MessageRole values
    /// </summary>
    public static Gen<MessageRole> MessageRoleGen()
    {
        return Gen.Item(new[] { MessageRole.User, MessageRole.Assistant, MessageRole.System });
    }

    /// <summary>
    /// Generates a random Message with various content and roles
    /// </summary>
    public static Gen<Message> RandomMessage()
    {
        return
            from content in MessageContent()
            from role in MessageRoleGen()
            select new Message(content, role);
    }

    /// <summary>
    /// Generates edge case messages (empty, very long, special characters)
    /// </summary>
    public static Gen<Message> EdgeCaseMessage()
    {
        return Gen.Choice(
            EmptyMessage(),
            VeryLongMessage(),
            SpecialCharacterMessage(),
            WhitespaceOnlyMessage(),
            UnicodeMessage()
        );
    }

    /// <summary>
    /// Generates short message content (1-50 characters)
    /// </summary>
    private static Gen<string> ShortMessageContent()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(1, 50));
    }

    /// <summary>
    /// Generates medium message content (51-500 characters)
    /// </summary>
    private static Gen<string> MediumMessageContent()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(51, 500));
    }

    /// <summary>
    /// Generates long message content (501-2000 characters)
    /// </summary>
    private static Gen<string> LongMessageContent()
    {
        return Gen.AlphaNumeric.String(HedgehogRange.Constant(501, 2000));
    }

    /// <summary>
    /// Generates content with special characters
    /// </summary>
    private static Gen<string> SpecialCharacterContent()
    {
        var specialChars = "!@#$%^&*()_+-=[]{}|;:',.<>?/~`\"\\";
        return
            from length in Gen.Int32(HedgehogRange.Constant(10, 100))
            from chars in Gen.Item(specialChars.ToCharArray()).List(HedgehogRange.Singleton(length))
            select new string(chars.ToArray());
    }

    /// <summary>
    /// Generates content with Unicode characters (emoji, accents, etc.)
    /// </summary>
    private static Gen<string> UnicodeContent()
    {
        var unicodeStrings = new[]
        {
            "Hello 世界",
            "Привет мир",
            "مرحبا بالعالم",
            "こんにちは世界",
            "🌍🌎🌏",
            "Café résumé naïve",
            "Ñoño español",
            "Ελληνικά",
            "עברית",
            "한국어"
        };
        return Gen.Item(unicodeStrings);
    }

    /// <summary>
    /// Generates an empty message
    /// </summary>
    private static Gen<Message> EmptyMessage()
    {
        return
            from role in MessageRoleGen()
            select new Message(string.Empty, role);
    }

    /// <summary>
    /// Generates a very long message (5000-10000 characters)
    /// </summary>
    private static Gen<Message> VeryLongMessage()
    {
        return
            from content in Gen.AlphaNumeric.String(HedgehogRange.Constant(5000, 10000))
            from role in MessageRoleGen()
            select new Message(content, role);
    }

    /// <summary>
    /// Generates a message with special characters
    /// </summary>
    private static Gen<Message> SpecialCharacterMessage()
    {
        return
            from content in SpecialCharacterContent()
            from role in MessageRoleGen()
            select new Message(content, role);
    }

    /// <summary>
    /// Generates a message with only whitespace
    /// </summary>
    private static Gen<Message> WhitespaceOnlyMessage()
    {
        return
            from length in Gen.Int32(HedgehogRange.Constant(1, 50))
            from role in MessageRoleGen()
            select new Message(new string(' ', length), role);
    }

    /// <summary>
    /// Generates a message with Unicode content
    /// </summary>
    private static Gen<Message> UnicodeMessage()
    {
        return
            from content in UnicodeContent()
            from role in MessageRoleGen()
            select new Message(content, role);
    }
}

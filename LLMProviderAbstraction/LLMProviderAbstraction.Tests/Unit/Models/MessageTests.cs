using FluentAssertions;
using LLMProviderAbstraction.Models;
using Xunit;

namespace LLMProviderAbstraction.Tests.Unit.Models;

/// <summary>
/// Unit tests for the Message class
/// </summary>
public class MessageTests
{
    [Fact]
    public void Constructor_WithUserRole_CreatesMessageWithCorrectProperties()
    {
        // Arrange
        var content = "Hello, how can I help you?";
        var role = MessageRole.User;
        var beforeTimestamp = DateTime.UtcNow;

        // Act
        var message = new Message(content, role);
        var afterTimestamp = DateTime.UtcNow;

        // Assert
        message.Content.Should().Be(content);
        message.Role.Should().Be(role);
        message.Timestamp.Should().BeOnOrAfter(beforeTimestamp);
        message.Timestamp.Should().BeOnOrBefore(afterTimestamp);
    }

    [Fact]
    public void Constructor_WithAssistantRole_CreatesMessageWithCorrectProperties()
    {
        // Arrange
        var content = "I'm here to assist you.";
        var role = MessageRole.Assistant;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().Be(content);
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithSystemRole_CreatesMessageWithCorrectProperties()
    {
        // Arrange
        var content = "System initialization complete.";
        var role = MessageRole.System;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().Be(content);
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithEmptyString_CreatesMessageWithEmptyContent()
    {
        // Arrange
        var content = string.Empty;
        var role = MessageRole.User;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().BeEmpty();
        message.Role.Should().Be(role);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_PreservesContent()
    {
        // Arrange
        var content = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        var role = MessageRole.User;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().Be(content);
    }

    [Fact]
    public void Constructor_WithUnicodeCharacters_PreservesContent()
    {
        // Arrange
        var content = "Unicode: 你好 مرحبا שלום";
        var role = MessageRole.User;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().Be(content);
    }

    [Fact]
    public void Constructor_WithVeryLongContent_PreservesContent()
    {
        // Arrange
        var content = new string('a', 10000);
        var role = MessageRole.User;

        // Act
        var message = new Message(content, role);

        // Assert
        message.Content.Should().Be(content);
        message.Content.Length.Should().Be(10000);
    }

    [Fact]
    public void Timestamp_IsSetToUtcTime()
    {
        // Arrange & Act
        var message = new Message("test", MessageRole.User);

        // Assert
        message.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Constructor_CreatesNewTimestampForEachMessage()
    {
        // Arrange & Act
        var message1 = new Message("first", MessageRole.User);
        Thread.Sleep(10); // Small delay to ensure different timestamps
        var message2 = new Message("second", MessageRole.User);

        // Assert
        message2.Timestamp.Should().BeAfter(message1.Timestamp);
    }
}

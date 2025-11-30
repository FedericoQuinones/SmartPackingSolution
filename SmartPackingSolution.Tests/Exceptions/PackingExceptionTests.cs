namespace SmartPackingSolution.Tests.Exceptions;

using FluentAssertions;
using SmartPackingSolution.Exceptions;
using Xunit;

public class PackingExceptionTests
{
    [Fact]
    public void Constructor_WithNoParameters_ShouldCreateInstance()
    {
        // Act
        var exception = new PackingException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new PackingException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new PackingException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void PackingException_ShouldBeThrowable()
    {
        // Act
        Action act = () => throw new PackingException("Test exception");

        // Assert
        act.Should().Throw<PackingException>()
            .WithMessage("Test exception");
    }
}
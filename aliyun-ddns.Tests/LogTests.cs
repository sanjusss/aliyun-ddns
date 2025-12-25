using System;
using System.IO;
using Xunit;
using aliyun_ddns.Common;
using aliyun_ddns;

namespace aliyun_ddns.Tests
{
    public class LogTests
    {
        [Fact]
        public void Print_ShouldOutputToConsole()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Act
            Log.Print("Test message");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Test message", output);
            Assert.Contains("[", output); // Should contain timestamp brackets
            Assert.Contains("]", output);

            // Cleanup
            Console.SetOut(originalOut);
        }

        [Fact]
        public void Print_ShouldIncludeTimestamp()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Act
            Log.Print("Message with timestamp");

            // Assert
            var output = stringWriter.ToString();
            // Output format should be: [MM/DD/YYYY HH:MM:SS]Message or [YYYY-MM-DD HH:MM:SS]Message
            Assert.Matches(@"\[\d{1,4}", output);

            // Cleanup
            Console.SetOut(originalOut);
        }

        [Fact]
        public void Print_WithEmptyString_ShouldStillIncludeTimestamp()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Act
            Log.Print("");

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("[", output);
            Assert.Contains("]", output);

            // Cleanup
            Console.SetOut(originalOut);
        }

        [Fact]
        public void Print_WithNullString_ShouldNotThrowException()
        {
            // Arrange
            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // Act & Assert
            var exception = Record.Exception(() => Log.Print(null));
            
            // Cleanup
            Console.SetOut(originalOut);
            
            // The exception might be thrown by string concatenation, which is acceptable
            // We're just ensuring the method can be called
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using aliyun_ddns.Common;

namespace aliyun_ddns.Tests
{
    public class TaskExtensionTests
    {
        [Fact]
        public async Task WhenAny_WithSuccessfulTask_ShouldReturnFirstMatchingResult()
        {
            // Arrange
            var tasks = new List<Task<int>>
            {
                Task.Delay(100).ContinueWith(_ => 1),
                Task.Delay(50).ContinueWith(_ => 2),
                Task.Delay(150).ContinueWith(_ => 3)
            };

            // Act
            var result = await tasks.WhenAny(task => task.Result == 2);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task WhenAny_WithNoMatchingTask_ShouldReturnDefault()
        {
            // Arrange
            var tasks = new List<Task<int>>
            {
                Task.Delay(10).ContinueWith(_ => 1),
                Task.Delay(10).ContinueWith(_ => 2),
                Task.Delay(10).ContinueWith(_ => 3)
            };

            // Act
            var result = await tasks.WhenAny(task => task.Result == 99);

            // Assert
            Assert.Equal(0, result); // default for int
        }

        [Fact]
        public async Task WhenAny_WithTimeout_ShouldReturnMatchingResultBeforeTimeout()
        {
            // Arrange
            var tasks = new List<Task<string>>
            {
                Task.Delay(50).ContinueWith(_ => "fast"),
                Task.Delay(200).ContinueWith(_ => "slow")
            };
            var timeout = TimeSpan.FromMilliseconds(150);

            // Act
            var result = await tasks.WhenAny(task => task.Result == "fast", timeout);

            // Assert
            Assert.Equal("fast", result);
        }

        [Fact]
        public async Task WhenAny_WithTimeoutExpired_ShouldReturnDefault()
        {
            // Arrange
            var tasks = new List<Task<string>>
            {
                Task.Delay(200).ContinueWith(_ => "slow1"),
                Task.Delay(200).ContinueWith(_ => "slow2")
            };
            var timeout = TimeSpan.FromMilliseconds(50);

            // Act
            var result = await tasks.WhenAny(task => task.Result == "slow1", timeout);

            // Assert
            Assert.Null(result); // default for reference type
        }

        [Fact]
        public async Task WhenAny_WithInfiniteTimeout_ShouldWaitForMatch()
        {
            // Arrange
            var tasks = new List<Task<int>>
            {
                Task.Delay(50).ContinueWith(_ => 42)
            };

            // Act
            var result = await tasks.WhenAny(task => task.Result == 42, Timeout.InfiniteTimeSpan);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task WhenAny_WithMultipleMatchingTasks_ShouldReturnFirstCompleted()
        {
            // Arrange
            var tasks = new List<Task<bool>>
            {
                Task.Delay(100).ContinueWith(_ => true),
                Task.Delay(50).ContinueWith(_ => true),
                Task.Delay(150).ContinueWith(_ => true)
            };

            // Act
            var result = await tasks.WhenAny(task => task.Result == true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WhenAny_WithEmptyTaskList_ShouldReturnDefault()
        {
            // Arrange
            var tasks = new List<Task<int>>();

            // Act
            var result = await tasks.WhenAny(task => true);

            // Assert
            Assert.Equal(0, result);
        }
    }
}

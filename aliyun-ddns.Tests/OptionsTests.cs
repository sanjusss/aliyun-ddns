using System;
using Xunit;
using aliyun_ddns;

namespace aliyun_ddns.Tests
{
    public class OptionsTests
    {
        [Fact]
        public void Options_DefaultValues_ShouldBeSet()
        {
            // Arrange & Act
            var options = new Options();

            // Assert
            Assert.Equal("access key id", options.Akid);
            Assert.Equal("access key secret", options.Aksct);
            Assert.Equal("my.domain.com", options.Domain);
            Assert.Null(options.RootDomain);
            Assert.Equal(300, options.Redo);
            Assert.Equal(600, options.TTL);
            Assert.Equal(8.0, options.Timezone);
            Assert.Equal("A,AAAA", options.Type);
            Assert.False(options.CNIPv4);
            Assert.Null(options.WebHook);
            Assert.False(options.CheckLocalNetworkAdaptor);
            Assert.Null(options.IPv4Networks);
            Assert.Null(options.IPv6Networks);
        }

        [Fact]
        public void Options_PropertySetters_ShouldWork()
        {
            // Arrange
            var options = new Options();

            // Act
            options.Akid = "test-key-id";
            options.Aksct = "test-key-secret";
            options.Domain = "test.example.com";
            options.RootDomain = "example.com";
            options.Redo = 600;
            options.TTL = 300;
            options.Timezone = 5.5;
            options.Type = "A";
            options.CNIPv4 = true;
            options.WebHook = "https://example.com/webhook";
            options.CheckLocalNetworkAdaptor = true;
            options.IPv4Networks = "192.168.1.0/24";
            options.IPv6Networks = "240e::/16";

            // Assert
            Assert.Equal("test-key-id", options.Akid);
            Assert.Equal("test-key-secret", options.Aksct);
            Assert.Equal("test.example.com", options.Domain);
            Assert.Equal("example.com", options.RootDomain);
            Assert.Equal(600, options.Redo);
            Assert.Equal(300, options.TTL);
            Assert.Equal(5.5, options.Timezone);
            Assert.Equal("A", options.Type);
            Assert.True(options.CNIPv4);
            Assert.Equal("https://example.com/webhook", options.WebHook);
            Assert.True(options.CheckLocalNetworkAdaptor);
            Assert.Equal("192.168.1.0/24", options.IPv4Networks);
            Assert.Equal("240e::/16", options.IPv6Networks);
        }

        [Fact]
        public void Options_Instance_ShouldNotBeNull()
        {
            // Act
            var instance = Options.Instance;

            // Assert
            Assert.NotNull(instance);
        }

        [Fact]
        public void Options_Instance_ShouldBeSingleton()
        {
            // Act
            var instance1 = Options.Instance;
            var instance2 = Options.Instance;

            // Assert
            Assert.Same(instance1, instance2);
        }
    }
}

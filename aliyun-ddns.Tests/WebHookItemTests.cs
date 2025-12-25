using System;
using Xunit;
using aliyun_ddns.WebHook;

namespace aliyun_ddns.Tests
{
    public class WebHookItemTests
    {
        [Fact]
        public void WebHookItem_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var item = new WebHookItem();

            // Assert
            Assert.Null(item.recordType);
            Assert.Null(item.domain);
            Assert.Null(item.ip);
        }

        [Fact]
        public void WebHookItem_ShouldSetAndGetProperties()
        {
            // Arrange
            var item = new WebHookItem
            {
                recordType = "A",
                domain = "example.com",
                ip = "192.168.1.1"
            };

            // Assert
            Assert.Equal("A", item.recordType);
            Assert.Equal("example.com", item.domain);
            Assert.Equal("192.168.1.1", item.ip);
        }

        [Fact]
        public void WebHookItem_WithAAAARecord_ShouldStoreIPv6()
        {
            // Arrange
            var item = new WebHookItem
            {
                recordType = "AAAA",
                domain = "ipv6.example.com",
                ip = "2001:0db8:85a3:0000:0000:8a2e:0370:7334"
            };

            // Assert
            Assert.Equal("AAAA", item.recordType);
            Assert.Equal("ipv6.example.com", item.domain);
            Assert.Equal("2001:0db8:85a3:0000:0000:8a2e:0370:7334", item.ip);
        }

        [Fact]
        public void WebHookItem_StructEquality_ShouldWorkCorrectly()
        {
            // Arrange
            var item1 = new WebHookItem
            {
                recordType = "A",
                domain = "test.com",
                ip = "1.2.3.4"
            };

            var item2 = new WebHookItem
            {
                recordType = "A",
                domain = "test.com",
                ip = "1.2.3.4"
            };

            // Act & Assert
            Assert.Equal(item1.recordType, item2.recordType);
            Assert.Equal(item1.domain, item2.domain);
            Assert.Equal(item1.ip, item2.ip);
        }
    }
}

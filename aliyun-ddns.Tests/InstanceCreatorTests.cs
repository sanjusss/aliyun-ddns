using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using aliyun_ddns.Common;
using aliyun_ddns.IPGetter;
using aliyun_ddns.IPGetter.IPv4Getter;
using aliyun_ddns.IPGetter.IPv6Getter;

namespace aliyun_ddns.Tests
{
    public class InstanceCreatorTests
    {
        [Fact]
        public void Create_WithIIPv4Getter_ShouldReturnInstances()
        {
            // Act
            var instances = InstanceCreator.Create<IIPv4Getter>();

            // Assert
            Assert.NotNull(instances);
            Assert.NotEmpty(instances);
        }

        [Fact]
        public void Create_WithIIPv6Getter_ShouldReturnInstances()
        {
            // Act
            var instances = InstanceCreator.Create<IIPv6Getter>();

            // Assert
            Assert.NotNull(instances);
            Assert.NotEmpty(instances);
        }

        [Fact]
        public void Create_WithIIPGetter_ShouldReturnInstances()
        {
            // Act
            var instances = InstanceCreator.Create<IIPGetter>();

            // Assert
            Assert.NotNull(instances);
            Assert.NotEmpty(instances);
        }

        [Fact]
        public void Create_ShouldNotReturnAbstractClasses()
        {
            // Act
            var instances = InstanceCreator.Create<IIPGetter>();

            // Assert
            Assert.All(instances, instance => 
                Assert.False(instance.GetType().IsAbstract));
        }

        [Fact]
        public void Create_ShouldNotReturnInterfaces()
        {
            // Act
            var instances = InstanceCreator.Create<IIPGetter>();

            // Assert
            Assert.All(instances, instance => 
                Assert.False(instance.GetType().IsInterface));
        }

        [Fact]
        public void Create_WithFilter_ShouldFilterInstances()
        {
            // Act - filter to only include types whose name contains "Local"
            var allInstances = InstanceCreator.Create<IIPGetter>();
            var filteredInstances = InstanceCreator.Create<IIPGetter>(t => t.Name.Contains("Local"));

            // Assert
            Assert.True(filteredInstances.Count() <= allInstances.Count());
            Assert.All(filteredInstances, instance => 
                Assert.Contains("Local", instance.GetType().Name));
        }

        [Fact]
        public void Create_WithNullFilter_ShouldReturnAllInstances()
        {
            // Act
            var instances1 = InstanceCreator.Create<IIPGetter>(null);
            var instances2 = InstanceCreator.Create<IIPGetter>();

            // Assert
            Assert.Equal(instances1.Count(), instances2.Count());
        }
    }

    // Test interface for testing
    public interface ITestInterface
    {
        string GetValue();
    }

    // Test implementation for testing
    public class TestImplementation : ITestInterface
    {
        public string GetValue() => "test";
    }
}

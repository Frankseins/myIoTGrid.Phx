using FluentAssertions;
using myIoTGrid.Shared.Common.Constants;
using Xunit;

namespace myIoTGrid.Shared.Common.Tests;

public class SpecialTenantsTests
{
    [Fact]
    public void IsSpecialTenant_SharedTenant_ReturnsTrue()
    {
        // Arrange
        var tenantId = SpecialTenants.SHARED;

        // Act
        var result = SpecialTenants.IsSpecialTenant(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSpecialTenant_TemplatesTenant_ReturnsTrue()
    {
        // Arrange
        var tenantId = SpecialTenants.TEMPLATES;

        // Act
        var result = SpecialTenants.IsSpecialTenant(tenantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSpecialTenant_RegularTenant_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = SpecialTenants.IsSpecialTenant(tenantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanWrite_SpecialTenant_AdminTrue_ReturnsTrue()
    {
        // Arrange
        var tenantId = SpecialTenants.SHARED;

        // Act
        var result = SpecialTenants.CanWrite(tenantId, isSystemAdmin: true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanWrite_SpecialTenant_AdminFalse_ReturnsFalse()
    {
        // Arrange
        var tenantId = SpecialTenants.SHARED;

        // Act
        var result = SpecialTenants.CanWrite(tenantId, isSystemAdmin: false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanWrite_RegularTenant_AnyAdmin_ReturnsTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act & Assert
        SpecialTenants.CanWrite(tenantId, isSystemAdmin: false).Should().BeTrue();
        SpecialTenants.CanWrite(tenantId, isSystemAdmin: true).Should().BeTrue();
    }

    [Fact]
    public void SharedTenant_HasExpectedGuid()
    {
        // Assert
        SpecialTenants.SHARED.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TemplatesTenant_HasExpectedGuid()
    {
        // Assert
        SpecialTenants.TEMPLATES.Should().Be(new Guid("99999999-9999-9999-9999-999999999999"));
    }
}

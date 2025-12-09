using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Domain;

public class LocationTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_ShouldCreateEmptyLocation()
    {
        // Act
        var location = new Location();

        // Assert
        location.Name.Should().BeNull();
        location.Latitude.Should().BeNull();
        location.Longitude.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNameOnly_ShouldSetNameAndNullCoordinates()
    {
        // Act
        var location = new Location("Wohnzimmer");

        // Assert
        location.Name.Should().Be("Wohnzimmer");
        location.Latitude.Should().BeNull();
        location.Longitude.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Act
        var location = new Location("Office", 50.9375, 6.9603);

        // Assert
        location.Name.Should().Be("Office");
        location.Latitude.Should().Be(50.9375);
        location.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void Constructor_WithNullName_ShouldAcceptNullName()
    {
        // Act
        var location = new Location(null, 50.0, 6.0);

        // Assert
        location.Name.Should().BeNull();
        location.Latitude.Should().Be(50.0);
        location.Longitude.Should().Be(6.0);
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldAcceptEmptyName()
    {
        // Act
        var location = new Location("");

        // Assert
        location.Name.Should().BeEmpty();
    }

    #endregion

    #region HasCoordinates Tests

    [Fact]
    public void HasCoordinates_WhenBothLatLongSet_ShouldReturnTrue()
    {
        // Arrange
        var location = new Location("Test", 50.0, 6.0);

        // Act & Assert
        location.HasCoordinates.Should().BeTrue();
    }

    [Fact]
    public void HasCoordinates_WhenOnlyLatitudeSet_ShouldReturnFalse()
    {
        // Arrange
        var location = new Location("Test", 50.0, null);

        // Act & Assert
        location.HasCoordinates.Should().BeFalse();
    }

    [Fact]
    public void HasCoordinates_WhenOnlyLongitudeSet_ShouldReturnFalse()
    {
        // Arrange
        var location = new Location("Test", null, 6.0);

        // Act & Assert
        location.HasCoordinates.Should().BeFalse();
    }

    [Fact]
    public void HasCoordinates_WhenNoCoordinates_ShouldReturnFalse()
    {
        // Arrange
        var location = new Location("Test");

        // Act & Assert
        location.HasCoordinates.Should().BeFalse();
    }

    [Fact]
    public void HasCoordinates_WithZeroCoordinates_ShouldReturnTrue()
    {
        // Arrange
        var location = new Location("Equator Prime Meridian", 0.0, 0.0);

        // Act & Assert
        location.HasCoordinates.Should().BeTrue();
    }

    [Fact]
    public void HasCoordinates_WithNegativeCoordinates_ShouldReturnTrue()
    {
        // Arrange
        var location = new Location("Southern Hemisphere", -33.8688, 151.2093);

        // Act & Assert
        location.HasCoordinates.Should().BeTrue();
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new Location("Original", 50.0, 6.0);

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Name.Should().Be(original.Name);
        clone.Latitude.Should().Be(original.Latitude);
        clone.Longitude.Should().Be(original.Longitude);
    }

    [Fact]
    public void Clone_ShouldNotAffectOriginalWhenModified()
    {
        // Arrange
        var original = new Location("Original", 50.0, 6.0);

        // Act
        var clone = original.Clone();
        clone.Name = "Modified";
        clone.Latitude = 99.0;

        // Assert
        original.Name.Should().Be("Original");
        original.Latitude.Should().Be(50.0);
    }

    [Fact]
    public void Clone_WithNullValues_ShouldCloneCorrectly()
    {
        // Arrange
        var original = new Location();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Name.Should().BeNull();
        clone.Latitude.Should().BeNull();
        clone.Longitude.Should().BeNull();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithNameAndCoordinates_ShouldReturnFormattedString()
    {
        // Arrange
        var location = new Location("Office", 50.937500, 6.960300);

        // Act
        var result = location.ToString();

        // Assert - Account for locale differences (comma vs period)
        result.Should().StartWith("Office (");
        result.Should().Contain("50");
        result.Should().Contain("937500");
        result.Should().Contain("6");
        result.Should().Contain("960300");
        result.Should().EndWith(")");
    }

    [Fact]
    public void ToString_WithOnlyName_ShouldReturnName()
    {
        // Arrange
        var location = new Location("Wohnzimmer");

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Be("Wohnzimmer");
    }

    [Fact]
    public void ToString_WithNoNameButCoordinates_ShouldReturnUnknownWithCoordinates()
    {
        // Arrange
        var location = new Location(null, 50.0, 6.0);

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Contain("Unknown");
        result.Should().Contain("50");
        result.Should().Contain("6");
    }

    [Fact]
    public void ToString_WithNoNameAndNoCoordinates_ShouldReturnUnknown()
    {
        // Arrange
        var location = new Location();

        // Act
        var result = location.ToString();

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void ToString_ShouldFormatCoordinatesWithSixDecimals()
    {
        // Arrange
        var location = new Location("Test", 50.1, 6.2);

        // Act
        var result = location.ToString();

        // Assert - Account for locale differences (comma vs period)
        result.Should().Contain("50");
        result.Should().Contain("100000");
        result.Should().Contain("6");
        result.Should().Contain("200000");
    }

    #endregion

    #region Equals Tests

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Office", 50.0, 6.0);

        // Act & Assert
        location1.Equals(location2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentName_ShouldReturnFalse()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Home", 50.0, 6.0);

        // Act & Assert
        location1.Equals(location2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentLatitude_ShouldReturnFalse()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Office", 51.0, 6.0);

        // Act & Assert
        location1.Equals(location2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentLongitude_ShouldReturnFalse()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Office", 50.0, 7.0);

        // Act & Assert
        location1.Equals(location2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var location = new Location("Office", 50.0, 6.0);

        // Act & Assert
        location.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var location = new Location("Office", 50.0, 6.0);

        // Act & Assert
        location.Equals("Office").Should().BeFalse();
    }

    [Fact]
    public void Equals_WithBothNull_ShouldReturnTrue()
    {
        // Arrange
        var location1 = new Location();
        var location2 = new Location();

        // Act & Assert
        location1.Equals(location2).Should().BeTrue();
    }

    [Fact]
    public void Equals_SameInstance_ShouldReturnTrue()
    {
        // Arrange
        var location = new Location("Office", 50.0, 6.0);

        // Act & Assert
        location.Equals(location).Should().BeTrue();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Office", 50.0, 6.0);

        // Act & Assert
        location1.GetHashCode().Should().Be(location2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var location1 = new Location("Office", 50.0, 6.0);
        var location2 = new Location("Home", 51.0, 7.0);

        // Act & Assert
        location1.GetHashCode().Should().NotBe(location2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var location = new Location("Office", 50.0, 6.0);

        // Act
        var hashCode1 = location.GetHashCode();
        var hashCode2 = location.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void GetHashCode_WithNullValues_ShouldNotThrow()
    {
        // Arrange
        var location = new Location();

        // Act & Assert
        var act = () => location.GetHashCode();
        act.Should().NotThrow();
    }

    #endregion

    #region Property Setter Tests

    [Fact]
    public void Name_Setter_ShouldUpdateValue()
    {
        // Arrange
        var location = new Location("Original");

        // Act
        location.Name = "Updated";

        // Assert
        location.Name.Should().Be("Updated");
    }

    [Fact]
    public void Latitude_Setter_ShouldUpdateValue()
    {
        // Arrange
        var location = new Location("Test", 50.0, 6.0);

        // Act
        location.Latitude = 60.0;

        // Assert
        location.Latitude.Should().Be(60.0);
    }

    [Fact]
    public void Longitude_Setter_ShouldUpdateValue()
    {
        // Arrange
        var location = new Location("Test", 50.0, 6.0);

        // Act
        location.Longitude = 10.0;

        // Assert
        location.Longitude.Should().Be(10.0);
    }

    [Fact]
    public void Latitude_Setter_ShouldAcceptNegativeValues()
    {
        // Arrange
        var location = new Location();

        // Act
        location.Latitude = -90.0;

        // Assert
        location.Latitude.Should().Be(-90.0);
    }

    [Fact]
    public void Longitude_Setter_ShouldAcceptNegativeValues()
    {
        // Arrange
        var location = new Location();

        // Act
        location.Longitude = -180.0;

        // Assert
        location.Longitude.Should().Be(-180.0);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Location_WithMaxDoubleValues_ShouldWork()
    {
        // Arrange & Act
        var location = new Location("Max", double.MaxValue, double.MaxValue);

        // Assert
        location.Latitude.Should().Be(double.MaxValue);
        location.Longitude.Should().Be(double.MaxValue);
    }

    [Fact]
    public void Location_WithMinDoubleValues_ShouldWork()
    {
        // Arrange & Act
        var location = new Location("Min", double.MinValue, double.MinValue);

        // Assert
        location.Latitude.Should().Be(double.MinValue);
        location.Longitude.Should().Be(double.MinValue);
    }

    [Fact]
    public void Location_WithVeryLongName_ShouldWork()
    {
        // Arrange
        var longName = new string('A', 10000);

        // Act
        var location = new Location(longName);

        // Assert
        location.Name.Should().Be(longName);
        location.Name!.Length.Should().Be(10000);
    }

    [Fact]
    public void Location_WithSpecialCharactersInName_ShouldWork()
    {
        // Arrange & Act
        var location = new Location("Büro / Étage 1 - 日本語");

        // Assert
        location.Name.Should().Be("Büro / Étage 1 - 日本語");
    }

    #endregion
}

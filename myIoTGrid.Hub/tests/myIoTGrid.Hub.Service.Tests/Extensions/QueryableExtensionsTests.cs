using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Extensions;

/// <summary>
/// Tests for QueryableExtensions for pagination, sorting, search, and date filtering.
/// </summary>
public class QueryableExtensionsTests
{
    #region Test Data Classes

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int Value { get; set; }
    }

    private IQueryable<TestEntity> CreateTestData() =>
        new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", Description = "First item", CreatedAt = DateTime.UtcNow.AddDays(-5), Value = 100 },
            new() { Id = 2, Name = "Beta", Description = "Second item", CreatedAt = DateTime.UtcNow.AddDays(-4), Value = 200 },
            new() { Id = 3, Name = "Charlie", Description = null, CreatedAt = DateTime.UtcNow.AddDays(-3), Value = 150 },
            new() { Id = 4, Name = "Delta", Description = "Fourth item", CreatedAt = DateTime.UtcNow.AddDays(-2), Value = 50 },
            new() { Id = 5, Name = "Echo", Description = "Fifth item", CreatedAt = DateTime.UtcNow.AddDays(-1), Value = 250 },
        }.AsQueryable();

    #endregion

    #region ApplyPaging Tests

    [Fact]
    public void ApplyPaging_WithQueryParams_ReturnsCorrectPage()
    {
        // Arrange
        var data = CreateTestData();
        // Page is 0-based: Page 0 = first page
        var queryParams = new QueryParamsDto { Page = 0, Size = 2 };

        // Act
        var result = data.ApplyPaging(queryParams).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Beta");
    }

    [Fact]
    public void ApplyPaging_SecondPage_ReturnsCorrectItems()
    {
        // Arrange
        var data = CreateTestData();
        // Page is 0-based: Page 1 = second page
        var queryParams = new QueryParamsDto { Page = 1, Size = 2 };

        // Act
        var result = data.ApplyPaging(queryParams).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Charlie");
        result[1].Name.Should().Be("Delta");
    }

    [Fact]
    public void ApplyPaging_LastPartialPage_ReturnsRemainingItems()
    {
        // Arrange
        var data = CreateTestData();
        // Page is 0-based: Page 2 = third page (items 5-6, only Echo remains)
        var queryParams = new QueryParamsDto { Page = 2, Size = 2 };

        // Act
        var result = data.ApplyPaging(queryParams).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Echo");
    }

    [Fact]
    public void ApplyPaging_WithExplicitValues_ReturnsCorrectItems()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplyPaging(skip: 1, take: 3).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Beta");
    }

    [Fact]
    public void ApplyPaging_BeyondData_ReturnsEmpty()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Page = 10, Size = 10 };

        // Act
        var result = data.ApplyPaging(queryParams).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ApplySort Tests

    [Fact]
    public void ApplySort_ByName_SortsAscending()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Sort = "Name" };

        // Act
        var result = data.ApplySort(queryParams).ToList();

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[4].Name.Should().Be("Echo");
    }

    [Fact]
    public void ApplySort_ByNameDescending_SortsDescending()
    {
        // Arrange
        var data = CreateTestData();
        // Format is "field,desc" not "-field"
        var queryParams = new QueryParamsDto { Sort = "Name,desc" };

        // Act
        var result = data.ApplySort(queryParams).ToList();

        // Assert
        result[0].Name.Should().Be("Echo");
        result[4].Name.Should().Be("Alpha");
    }

    [Fact]
    public void ApplySort_ByValue_SortsCorrectly()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Sort = "Value" };

        // Act
        var result = data.ApplySort(queryParams).ToList();

        // Assert
        result[0].Value.Should().Be(50);
        result[4].Value.Should().Be(250);
    }

    [Fact]
    public void ApplySort_WithInvalidField_UsesDefault()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Sort = "NonExistentField" };

        // Act
        var result = data.ApplySort(queryParams, "Id").ToList();

        // Assert
        result[0].Id.Should().Be(1);
        result[4].Id.Should().Be(5);
    }

    [Fact]
    public void ApplySort_WithNullSort_UsesDefault()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Sort = null };

        // Act
        var result = data.ApplySort(queryParams, "Name").ToList();

        // Assert
        result[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public void ApplySort_CaseInsensitive_Works()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto { Sort = "name" }; // lowercase

        // Act
        var result = data.ApplySort(queryParams).ToList();

        // Assert
        result[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public void ApplySort_WithExplicitValues_SortsCorrectly()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySort("Value", ascending: false).ToList();

        // Assert
        result[0].Value.Should().Be(250);
        result[4].Value.Should().Be(50);
    }

    #endregion

    #region ThenApplySort Tests

    [Fact]
    public void ThenApplySort_AddsSecondarySort()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", Value = 100 },
            new() { Id = 2, Name = "Alpha", Value = 50 },
            new() { Id = 3, Name = "Beta", Value = 75 },
        }.AsQueryable();

        // Act
        var orderedQuery = data.ApplySort("Name", true) as IOrderedQueryable<TestEntity>;
        var result = orderedQuery!.ThenApplySort("Value", true).ToList();

        // Assert
        result[0].Value.Should().Be(50); // Alpha with lower value first
        result[1].Value.Should().Be(100); // Alpha with higher value
        result[2].Name.Should().Be("Beta");
    }

    [Fact]
    public void ThenApplySort_Descending_Works()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", Value = 100 },
            new() { Id = 2, Name = "Alpha", Value = 50 },
            new() { Id = 3, Name = "Beta", Value = 75 },
        }.AsQueryable();

        // Act
        var orderedQuery = data.ApplySort("Name", true) as IOrderedQueryable<TestEntity>;
        var result = orderedQuery!.ThenApplySort("Value", ascending: false).ToList();

        // Assert
        result[0].Value.Should().Be(100); // Alpha with higher value first
        result[1].Value.Should().Be(50); // Alpha with lower value
    }

    [Fact]
    public void ThenApplySort_WithInvalidField_ReturnsOriginalOrder()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", Value = 100 },
            new() { Id = 2, Name = "Alpha", Value = 50 },
        }.AsQueryable();

        // Act
        var orderedQuery = data.ApplySort("Name", true) as IOrderedQueryable<TestEntity>;
        var result = orderedQuery!.ThenApplySort("NonExistent").ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region ApplySearch Tests

    [Fact]
    public void ApplySearch_FindsByName()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("alpha", x => x.Name).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Alpha");
    }

    [Fact]
    public void ApplySearch_CaseInsensitive()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("BETA", x => x.Name).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Beta");
    }

    [Fact]
    public void ApplySearch_PartialMatch()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("ta", x => x.Name).ToList(); // matches Beta, Delta

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Name == "Beta");
        result.Should().Contain(x => x.Name == "Delta");
    }

    [Fact]
    public void ApplySearch_MultipleProperties()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("first", x => x.Name, x => x.Description).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Description.Should().Be("First item");
    }

    [Fact]
    public void ApplySearch_IgnoresNullProperties()
    {
        // Arrange
        var data = CreateTestData();

        // Act - Charlie has null Description
        var result = data.ApplySearch("harlie", x => x.Name, x => x.Description).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("Charlie");
    }

    [Fact]
    public void ApplySearch_EmptySearchTerm_ReturnsAll()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("", x => x.Name).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void ApplySearch_NullSearchTerm_ReturnsAll()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch(null, x => x.Name).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public void ApplySearch_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var data = CreateTestData();

        // Act
        var result = data.ApplySearch("xyz123", x => x.Name).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ApplyDateFilter Tests (Non-Nullable)

    [Fact]
    public void ApplyDateFilter_WithFromDate_FiltersCorrectly()
    {
        // Arrange
        // Create data with fixed dates for reliable testing
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", CreatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "Beta", CreatedAt = now.AddDays(-4) },
            new() { Id = 3, Name = "Charlie", CreatedAt = now.AddDays(-3) },
            new() { Id = 4, Name = "Delta", CreatedAt = now.AddDays(-2) },
            new() { Id = 5, Name = "Echo", CreatedAt = now.AddDays(-1) },
        }.AsQueryable();

        var fromDate = now.AddDays(-2);
        var queryParams = new QueryParamsDto { DateFrom = fromDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.CreatedAt).ToList();

        // Assert
        result.Should().HaveCount(2); // Delta and Echo
        result.Should().OnlyContain(x => x.CreatedAt >= fromDate);
    }

    [Fact]
    public void ApplyDateFilter_WithToDate_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", CreatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "Beta", CreatedAt = now.AddDays(-4) },
            new() { Id = 3, Name = "Charlie", CreatedAt = now.AddDays(-3) },
            new() { Id = 4, Name = "Delta", CreatedAt = now.AddDays(-2) },
            new() { Id = 5, Name = "Echo", CreatedAt = now.AddDays(-1) },
        }.AsQueryable();

        var toDate = now.AddDays(-3);
        var queryParams = new QueryParamsDto { DateTo = toDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.CreatedAt).ToList();

        // Assert
        result.Should().HaveCount(3); // Alpha, Beta, Charlie
        result.Should().OnlyContain(x => x.CreatedAt <= toDate);
    }

    [Fact]
    public void ApplyDateFilter_WithBothDates_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", CreatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "Beta", CreatedAt = now.AddDays(-4) },
            new() { Id = 3, Name = "Charlie", CreatedAt = now.AddDays(-3) },
            new() { Id = 4, Name = "Delta", CreatedAt = now.AddDays(-2) },
            new() { Id = 5, Name = "Echo", CreatedAt = now.AddDays(-1) },
        }.AsQueryable();

        var fromDate = now.AddDays(-4);
        var toDate = now.AddDays(-2);
        var queryParams = new QueryParamsDto { DateFrom = fromDate, DateTo = toDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.CreatedAt).ToList();

        // Assert
        result.Should().HaveCount(3); // Beta, Charlie, Delta
        result.Should().OnlyContain(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate);
    }

    [Fact]
    public void ApplyDateFilter_WithNoDates_ReturnsAll()
    {
        // Arrange
        var data = CreateTestData();
        var queryParams = new QueryParamsDto();

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.CreatedAt).ToList();

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region ApplyDateFilter Tests (Nullable)

    [Fact]
    public void ApplyDateFilter_Nullable_WithFromDate_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "A", UpdatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "B", UpdatedAt = now.AddDays(-2) },
            new() { Id = 3, Name = "C", UpdatedAt = null },
        }.AsQueryable();

        var fromDate = now.AddDays(-3);
        var queryParams = new QueryParamsDto { DateFrom = fromDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.UpdatedAt).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("B");
    }

    [Fact]
    public void ApplyDateFilter_Nullable_WithToDate_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "A", UpdatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "B", UpdatedAt = now.AddDays(-2) },
            new() { Id = 3, Name = "C", UpdatedAt = null },
        }.AsQueryable();

        var toDate = now.AddDays(-3);
        var queryParams = new QueryParamsDto { DateTo = toDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.UpdatedAt).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    [Fact]
    public void ApplyDateFilter_Nullable_WithBothDates_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "A", UpdatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "B", UpdatedAt = now.AddDays(-3) },
            new() { Id = 3, Name = "C", UpdatedAt = now.AddDays(-1) },
            new() { Id = 4, Name = "D", UpdatedAt = null },
        }.AsQueryable();

        var fromDate = now.AddDays(-4);
        var toDate = now.AddDays(-2);
        var queryParams = new QueryParamsDto { DateFrom = fromDate, DateTo = toDate };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.UpdatedAt).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("B");
    }

    [Fact]
    public void ApplyDateFilter_Nullable_ExcludesNullValues()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "A", UpdatedAt = now },
            new() { Id = 2, Name = "B", UpdatedAt = null },
        }.AsQueryable();

        var queryParams = new QueryParamsDto { DateFrom = now.AddDays(-1) };

        // Act
        var result = data.ApplyDateFilter(queryParams, x => x.UpdatedAt).ToList();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("A");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CombinedOperations_SearchSortAndPaginate_WorksCorrectly()
    {
        // Arrange
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Apple", Value = 100 },
            new() { Id = 2, Name = "Banana", Value = 50 },
            new() { Id = 3, Name = "Apricot", Value = 75 },
            new() { Id = 4, Name = "Avocado", Value = 125 },
            new() { Id = 5, Name = "Blueberry", Value = 80 },
        }.AsQueryable();

        var queryParams = new QueryParamsDto
        {
            Search = "A",          // matches Apple, Apricot, Avocado
            Sort = "Value,desc",   // sort by value descending
            Page = 0,              // first page (0-based)
            Size = 2
        };

        // Act
        var result = data
            .ApplySearch(queryParams.Search, x => x.Name)
            .ApplySort(queryParams)
            .ApplyPaging(queryParams)
            .ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Avocado"); // Highest value (125)
        result[1].Name.Should().Be("Apple");   // Second highest (100)
    }

    [Fact]
    public void CombinedOperations_DateFilterSearchAndPaginate_WorksCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<TestEntity>
        {
            new() { Id = 1, Name = "Alpha", CreatedAt = now.AddDays(-5) },
            new() { Id = 2, Name = "Beta", CreatedAt = now.AddDays(-3) },
            new() { Id = 3, Name = "Gamma", CreatedAt = now.AddDays(-1) },
            new() { Id = 4, Name = "Delta", CreatedAt = now.AddDays(-2) },
        }.AsQueryable();

        var queryParams = new QueryParamsDto
        {
            Search = "a",          // matches Alpha, Beta, Gamma, Delta
            DateFrom = now.AddDays(-4),
            DateTo = now.AddDays(-1),
            Sort = "Name",
            Page = 0,              // first page (0-based)
            Size = 10
        };

        // Act
        var result = data
            .ApplySearch(queryParams.Search, x => x.Name)
            .ApplyDateFilter(queryParams, x => x.CreatedAt)
            .ApplySort(queryParams)
            .ApplyPaging(queryParams)
            .ToList();

        // Assert
        result.Should().HaveCount(3); // Beta, Delta, Gamma (within date range)
        result[0].Name.Should().Be("Beta");
        result[1].Name.Should().Be("Delta");
        result[2].Name.Should().Be("Gamma");
    }

    #endregion
}

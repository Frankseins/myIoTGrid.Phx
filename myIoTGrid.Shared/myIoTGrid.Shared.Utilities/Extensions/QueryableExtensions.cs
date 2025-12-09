using System.Linq.Expressions;
using System.Reflection;
using myIoTGrid.Shared.Common.DTOs.Common;

namespace myIoTGrid.Shared.Utilities.Extensions;

/// <summary>
/// Extension Methods für IQueryable zur Unterstützung von Paginierung, Sortierung und Suche
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Wendet Paginierung auf ein IQueryable an
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        QueryParamsDto queryParams)
    {
        return query
            .Skip(queryParams.Skip)
            .Take(queryParams.Take);
    }

    /// <summary>
    /// Wendet Paginierung mit expliziten Werten an
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(
        this IQueryable<T> query,
        int skip,
        int take)
    {
        return query.Skip(skip).Take(take);
    }

    /// <summary>
    /// Wendet Sortierung auf ein IQueryable an (dynamisch per Property-Name)
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        QueryParamsDto queryParams,
        string defaultSortField = "Id")
    {
        var (field, ascending) = queryParams.ParseSort();
        return query.ApplySort(field, ascending, defaultSortField);
    }

    /// <summary>
    /// Wendet Sortierung mit expliziten Werten an
    /// </summary>
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortField,
        bool ascending = true,
        string defaultSortField = "Id")
    {
        var field = string.IsNullOrWhiteSpace(sortField) ? defaultSortField : sortField;

        // Prüfen ob Property existiert
        var property = typeof(T).GetProperty(
            field,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
        {
            property = typeof(T).GetProperty(
                defaultSortField,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        }

        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = ascending ? "OrderBy" : "OrderByDescending";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), property.PropertyType },
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// Fügt ThenBy Sortierung hinzu
    /// </summary>
    public static IOrderedQueryable<T> ThenApplySort<T>(
        this IOrderedQueryable<T> query,
        string sortField,
        bool ascending = true)
    {
        var property = typeof(T).GetProperty(
            sortField,
            BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = ascending ? "ThenBy" : "ThenByDescending";

        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new[] { typeof(T), property.PropertyType },
            query.Expression,
            Expression.Quote(lambda));

        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(resultExpression);
    }

    /// <summary>
    /// Wendet globale Suche auf mehrere String-Properties an
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(
        this IQueryable<T> query,
        string? searchTerm,
        params Expression<Func<T, string?>>[] searchProperties)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchProperties.Length == 0)
            return query;

        var term = searchTerm.ToLower();
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var propertySelector in searchProperties)
        {
            var propertyBody = (MemberExpression)propertySelector.Body;
            var propertyAccess = Expression.Property(parameter, (PropertyInfo)propertyBody.Member);

            // x.Property != null && x.Property.ToLower().Contains(term)
            var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(propertyAccess, "ToLower", Type.EmptyTypes);
            var contains = Expression.Call(toLower, "Contains", Type.EmptyTypes, Expression.Constant(term));
            var condition = Expression.AndAlso(notNull, contains);

            combinedExpression = combinedExpression == null
                ? condition
                : Expression.OrElse(combinedExpression, condition);
        }

        if (combinedExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Wendet Datumsfilter an
    /// </summary>
    public static IQueryable<T> ApplyDateFilter<T>(
        this IQueryable<T> query,
        QueryParamsDto queryParams,
        Expression<Func<T, DateTime>> dateProperty)
    {
        if (queryParams.DateFrom == null && queryParams.DateTo == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyBody = (MemberExpression)dateProperty.Body;
        var propertyAccess = Expression.Property(parameter, (PropertyInfo)propertyBody.Member);

        Expression? combinedExpression = null;

        if (queryParams.DateFrom.HasValue)
        {
            var fromDate = Expression.Constant(queryParams.DateFrom.Value);
            var greaterOrEqual = Expression.GreaterThanOrEqual(propertyAccess, fromDate);
            combinedExpression = greaterOrEqual;
        }

        if (queryParams.DateTo.HasValue)
        {
            var toDate = Expression.Constant(queryParams.DateTo.Value);
            var lessOrEqual = Expression.LessThanOrEqual(propertyAccess, toDate);
            combinedExpression = combinedExpression == null
                ? lessOrEqual
                : Expression.AndAlso(combinedExpression, lessOrEqual);
        }

        if (combinedExpression == null)
            return query;

        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }

    /// <summary>
    /// Wendet Datumsfilter für nullable DateTime an
    /// </summary>
    public static IQueryable<T> ApplyDateFilter<T>(
        this IQueryable<T> query,
        QueryParamsDto queryParams,
        Expression<Func<T, DateTime?>> dateProperty)
    {
        if (queryParams.DateFrom == null && queryParams.DateTo == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyBody = (MemberExpression)dateProperty.Body;
        var propertyAccess = Expression.Property(parameter, (PropertyInfo)propertyBody.Member);

        // Property.HasValue
        var hasValue = Expression.Property(propertyAccess, "HasValue");
        var getValue = Expression.Property(propertyAccess, "Value");

        Expression? dateCondition = null;

        if (queryParams.DateFrom.HasValue)
        {
            var fromDate = Expression.Constant(queryParams.DateFrom.Value);
            var greaterOrEqual = Expression.GreaterThanOrEqual(getValue, fromDate);
            dateCondition = greaterOrEqual;
        }

        if (queryParams.DateTo.HasValue)
        {
            var toDate = Expression.Constant(queryParams.DateTo.Value);
            var lessOrEqual = Expression.LessThanOrEqual(getValue, toDate);
            dateCondition = dateCondition == null
                ? lessOrEqual
                : Expression.AndAlso(dateCondition, lessOrEqual);
        }

        if (dateCondition == null)
            return query;

        var combinedExpression = Expression.AndAlso(hasValue, dateCondition);
        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }
}

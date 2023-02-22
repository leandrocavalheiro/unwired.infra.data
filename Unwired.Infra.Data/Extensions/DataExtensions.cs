using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Unwired.Models.ViewModels;
using Unwired.Domain.Abstractions.Entities;
using AutoMapper.QueryableExtensions;
using System.Linq.Expressions;
using Unwired.Models.Types;

namespace Unwired.Infra.Data.Extensions;

public static class DataExtensions
{
    [Obsolete("Use method with ICollection<USort> parameter")]
    public static IQueryable<T> Sort<T>(this IQueryable<T> source, Dictionary<string, string> sortBy)
    {

        foreach (var currentSortBy in sortBy)
        {
            if (currentSortBy.Key == null)
                continue;

            if (currentSortBy.Value == null || currentSortBy.Value == "desc")
           
                source = source.OrderBy(ToLambda<T>(currentSortBy.Key));            
            else            
                source = source.OrderByDescending(ToLambda<T>(currentSortBy.Key));            
        }

        return source;
    }
    public static IQueryable<T> Sort<T>(this IQueryable<T> source, ICollection<USort> sortBy)
    {

        foreach (var currentSortBy in sortBy)
        {
            if (string.IsNullOrEmpty(currentSortBy.Field))
                continue;

            if (currentSortBy.Descending)
                source = source.OrderByDescending(ToLambda<T>(currentSortBy.Field));
            else
                source = source.OrderBy(ToLambda<T>(currentSortBy.Field));
        }

        return source;
    }
    public static IOrderedQueryable<T> SortAsc<T>(this IQueryable<T> source, string propertyName)
        => source.OrderBy(ToLambda<T>(propertyName));
    public static IOrderedQueryable<T> SortDesc<T>(this IQueryable<T> source, string propertyName)
        => source.OrderByDescending(ToLambda<T>(propertyName));    
    private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, propertyName);
        var propAsObject = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
    }
    public static async Task<UPaginatedViewModel<TViewModel>> PaginationAsync<TEntity, TViewModel>(this IQueryable<TEntity> query, IMapper mapper, int page = 1, int pageSize = 25, bool filterDeleted = true, CancellationToken cancellationToken = default) where TEntity : UEntity
    {
        var skip = (page - 1) * pageSize;
        if (filterDeleted)        
            query = query.Where(p => p.DeletedAt.Equals(null));
        var totalRecords = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((totalRecords / (decimal)pageSize));

        var list = await
            query
                .Skip(skip)
                .Take(pageSize)
                .ProjectTo<TViewModel>(mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

        return new UPaginatedViewModel<TViewModel>(list, page, pageSize, totalRecords, totalPages);
    }
}

using CleanArchTemplate.Shared.Constants;

namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Represents a pagination request
/// </summary>
public class PaginationRequest
{
    private int _pageNumber = 1;
    private int _pageSize = ApplicationConstants.Pagination.DefaultPageSize;

    /// <summary>
    /// Gets or sets the page number (1-based)
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < ApplicationConstants.Pagination.MinPageSize => ApplicationConstants.Pagination.MinPageSize,
            > ApplicationConstants.Pagination.MaxPageSize => ApplicationConstants.Pagination.MaxPageSize,
            _ => value
        };
    }

    /// <summary>
    /// Gets the number of items to skip
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take
    /// </summary>
    public int Take => PageSize;
}
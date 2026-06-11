using System.Text.Json;
using CRM.Application.Abstractions;
using CRM.Application.Common;
using CRM.Application.Customers.Dtos;
using CRM.Application.Customers.Mapping;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Customers.Services;

public sealed class CustomerService : ICustomerService
{
    private const string CachePrefix = "customer:";
    private const string ListPrefix = "customer:list:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(IApplicationDbContext db, ICacheService cache, ILogger<CustomerService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<CustomerDto>> ListAsync(CustomerFilter filter, CancellationToken ct)
    {
        var cacheKey = ListPrefix + JsonSerializer.Serialize(filter);
        var cached = await _cache.GetAsync<PagedResult<CustomerDto>>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT {Key}", cacheKey);
            return cached;
        }
        _logger.LogInformation("Cache MISS {Key}", cacheKey);

        IQueryable<Customer> query = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(s) ||
                c.LastName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                (c.Company != null && c.Company.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(c => c.City == filter.City);
        if (!string.IsNullOrWhiteSpace(filter.Country))
            query = query.Where(c => c.Country == filter.Country);
        if (!string.IsNullOrWhiteSpace(filter.Company))
            query = query.Where(c => c.Company == filter.Company);

        query = ApplySort(query, filter.Sort);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(CustomerMapper.Projection)
            .ToListAsync(ct);

        var result = new PagedResult<CustomerDto>(items, total, filter.Page, filter.PageSize);
        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);
        return result;
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var cacheKey = $"{CachePrefix}{id}";
        var cached = await _cache.GetAsync<CustomerDto>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT {Key}", cacheKey);
            return Result<CustomerDto>.Success(cached);
        }
        _logger.LogInformation("Cache MISS {Key}", cacheKey);

        var dto = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(CustomerMapper.Projection)
            .FirstOrDefaultAsync(ct);

        if (dto is null)
            return Result<CustomerDto>.Failure($"Customer {id} not found.", ResultErrorCodes.NotFound);

        await _cache.SetAsync(cacheKey, dto, CacheTtl, ct);
        return Result<CustomerDto>.Success(dto);
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var duplicate = await _db.Customers.AnyAsync(c => c.Email.ToLower() == email, ct);
        if (duplicate)
            return Result<CustomerDto>.Failure(
                $"A customer with email '{email}' already exists.", ResultErrorCodes.Duplicate);

        var customer = CustomerMapper.ToEntity(request);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveByPrefixAsync(ListPrefix, ct);
        _logger.LogInformation("Customer {CustomerId} created (email={Email})", customer.Id, customer.Email);
        return Result<CustomerDto>.Success(CustomerMapper.ToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
            return Result<CustomerDto>.Failure($"Customer {id} not found.", ResultErrorCodes.NotFound);

        var email = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _db.Customers.AnyAsync(c => c.Id != id && c.Email.ToLower() == email, ct);
            if (duplicate)
                return Result<CustomerDto>.Failure(
                    $"A customer with email '{email}' already exists.", ResultErrorCodes.Duplicate);
        }

        CustomerMapper.Apply(request, customer);
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"{CachePrefix}{id}", ct);
        await _cache.RemoveByPrefixAsync(ListPrefix, ct);
        _logger.LogInformation("Customer {CustomerId} updated", customer.Id);
        return Result<CustomerDto>.Success(CustomerMapper.ToDto(customer));
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
            return Result<bool>.Failure($"Customer {id} not found.", ResultErrorCodes.NotFound);

        // Block delete when the customer still has live sales (DbSet<Sale> has a query
        // filter on !IsDeleted, so this only sees active deals). Reassign or close the
        // sales first — industry-typical guard against orphaning a deal's parent record.
        var activeSales = await _db.Sales.CountAsync(s => s.CustomerId == id, ct);
        if (activeSales > 0)
        {
            return Result<bool>.Failure(
                $"Cannot delete this customer: {activeSales} active sale(s) reference them. " +
                "Close or reassign the sale(s) first.",
                ResultErrorCodes.Conflict);
        }

        customer.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        await _cache.RemoveAsync($"{CachePrefix}{id}", ct);
        await _cache.RemoveByPrefixAsync(ListPrefix, ct);
        _logger.LogInformation("Customer {CustomerId} soft-deleted", customer.Id);
        return Result<bool>.Success(true);
    }

    private static IQueryable<Customer> ApplySort(IQueryable<Customer> query, string? sort) =>
        (sort?.ToLower()) switch
        {
            "name" => query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName),
            "-name" => query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName),
            "email" => query.OrderBy(c => c.Email),
            "-email" => query.OrderByDescending(c => c.Email),
            "created" => query.OrderBy(c => c.CreatedAt),
            "-created" or _ => query.OrderByDescending(c => c.CreatedAt),
        };
}

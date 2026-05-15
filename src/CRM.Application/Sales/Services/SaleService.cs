using System.Globalization;
using CRM.Application.Abstractions;
using CRM.Application.Common;
using CRM.Application.Sales.Dtos;
using CRM.Application.Sales.Mapping;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Application.Sales.Services;

public sealed class SaleService : ISaleService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        ILogger<SaleService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<SaleDto>> ListAsync(SaleFilter filter, CancellationToken ct)
    {
        var query = BuildQuery(filter);
        query = ApplySort(query, filter.Sort);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Include(s => s.Customer)
            .Include(s => s.User)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = items.ConvertAll(SaleMapper.ToDto);
        return new PagedResult<SaleDto>(dtos, total, filter.Page, filter.PageSize);
    }

    public async Task<Result<SaleDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var sale = await _db.Sales.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        return sale is null
            ? Result<SaleDto>.Failure($"Sale {id} not found.", ResultErrorCodes.NotFound)
            : Result<SaleDto>.Success(SaleMapper.ToDto(sale));
    }

    public async Task<Result<SaleDto>> CreateAsync(CreateSaleRequest request, CancellationToken ct)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            return Result<SaleDto>.Failure(
                $"Customer {request.CustomerId} not found.", ResultErrorCodes.NotFound);

        var ownerUserId = request.UserId ?? _currentUser.UserId
            ?? throw new InvalidOperationException("Sale owner could not be determined.");

        var userExists = await _db.Users.AnyAsync(u => u.Id == ownerUserId, ct);
        if (!userExists)
            return Result<SaleDto>.Failure(
                $"User {ownerUserId} not found.", ResultErrorCodes.NotFound);

        var sale = SaleMapper.ToEntity(request);
        sale.UserId = ownerUserId;
        ApplyStageLifecycle(sale);

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Sale {SaleId} created (customer={CustomerId}, amount={Amount})",
            sale.Id, sale.CustomerId, sale.Amount);

        var created = await _db.Sales.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.User)
            .FirstAsync(s => s.Id == sale.Id, ct);

        return Result<SaleDto>.Success(SaleMapper.ToDto(created));
    }

    public async Task<Result<SaleDto>> UpdateAsync(int id, UpdateSaleRequest request, CancellationToken ct)
    {
        var sale = await _db.Sales
            .Include(s => s.Customer)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null)
            return Result<SaleDto>.Failure($"Sale {id} not found.", ResultErrorCodes.NotFound);

        if (sale.CustomerId != request.CustomerId)
        {
            var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
            if (!customerExists)
                return Result<SaleDto>.Failure(
                    $"Customer {request.CustomerId} not found.", ResultErrorCodes.NotFound);
        }

        SaleMapper.Apply(request, sale);
        ApplyStageLifecycle(sale);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Sale {SaleId} updated", sale.Id);
        return Result<SaleDto>.Success(SaleMapper.ToDto(sale));
    }

    /// <summary>
    /// Stamps <see cref="Sale.ActualCloseDate"/> the first time a sale enters a closed stage,
    /// and clears it if the sale is later reopened. Preserves the original close date as long
    /// as the deal remains in a closed stage so reports stay stable.
    /// </summary>
    private static void ApplyStageLifecycle(Sale sale)
    {
        if (SaleStages.IsClosed(sale.Stage))
        {
            sale.ActualCloseDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else
        {
            sale.ActualCloseDate = null;
        }
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var sale = await _db.Sales.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (sale is null)
            return Result<bool>.Failure($"Sale {id} not found.", ResultErrorCodes.NotFound);

        sale.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Sale {SaleId} soft-deleted", sale.Id);
        return Result<bool>.Success(true);
    }

    public async Task<Result<byte[]>> ExportCsvAsync(SaleFilter filter, CancellationToken ct)
    {
        var query = BuildQuery(filter);
        var sales = await query
            .Include(s => s.Customer)
            .Include(s => s.User)
            .AsNoTracking()
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(ct);

        using var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
        {
            csv.WriteField("SaleId");
            csv.WriteField("CustomerId");
            csv.WriteField("CustomerName");
            csv.WriteField("Company");
            csv.WriteField("Owner");
            csv.WriteField("PipelineName");
            csv.WriteField("Stage");
            csv.WriteField("Amount");
            csv.WriteField("SaleDate");
            csv.WriteField("ExpectedCloseDate");
            csv.WriteField("ActualCloseDate");
            await csv.NextRecordAsync();

            foreach (var s in sales)
            {
                csv.WriteField(s.Id);
                csv.WriteField(s.CustomerId);
                csv.WriteField(s.Customer?.FullName);
                csv.WriteField(s.Customer?.Company);
                csv.WriteField(s.User?.Username);
                csv.WriteField(s.PipelineName);
                csv.WriteField(s.Stage);
                csv.WriteField(s.Amount.ToString("F2", CultureInfo.InvariantCulture));
                csv.WriteField(s.SaleDate.ToString("yyyy-MM-dd"));
                csv.WriteField(s.ExpectedCloseDate?.ToString("yyyy-MM-dd"));
                csv.WriteField(s.ActualCloseDate?.ToString("yyyy-MM-dd"));
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
        }

        return Result<byte[]>.Success(ms.ToArray());
    }

    private IQueryable<Sale> BuildQuery(SaleFilter filter)
    {
        IQueryable<Sale> query = _db.Sales;

        if (filter.CustomerId.HasValue) query = query.Where(s => s.CustomerId == filter.CustomerId);
        if (filter.UserId.HasValue) query = query.Where(s => s.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.Stage)) query = query.Where(s => s.Stage == filter.Stage);
        if (filter.FromDate.HasValue) query = query.Where(s => s.SaleDate >= filter.FromDate);
        if (filter.ToDate.HasValue) query = query.Where(s => s.SaleDate <= filter.ToDate);
        if (filter.MinAmount.HasValue) query = query.Where(s => s.Amount >= filter.MinAmount);
        if (filter.MaxAmount.HasValue) query = query.Where(s => s.Amount <= filter.MaxAmount);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(x =>
                x.PipelineName.ToLower().Contains(s) ||
                (x.Customer != null && (
                    x.Customer.FirstName.ToLower().Contains(s) ||
                    x.Customer.LastName.ToLower().Contains(s) ||
                    (x.Customer.Company != null && x.Customer.Company.ToLower().Contains(s)))));
        }

        return query;
    }

    private static IQueryable<Sale> ApplySort(IQueryable<Sale> query, string? sort) =>
        (sort?.ToLower()) switch
        {
            "amount" => query.OrderBy(s => s.Amount),
            "-amount" => query.OrderByDescending(s => s.Amount),
            "date" => query.OrderBy(s => s.SaleDate),
            "-date" or _ => query.OrderByDescending(s => s.SaleDate),
        };
}

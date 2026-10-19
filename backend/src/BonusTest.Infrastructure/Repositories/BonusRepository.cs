using Microsoft.EntityFrameworkCore;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Infrastructure.Persistence;

namespace BonusTest.Infrastructure.Repositories;

public class BonusRepository(AppDbContext context) : IBonusRepository
{
    public async Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken)
    {
        var bonuses = context.Bonuses.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.DepartmentCode))
            bonuses = bonuses.Where(x => x.DepartmentCode == filter.DepartmentCode);
        if (filter.EmployeeNumber.HasValue)
            bonuses = bonuses.Where(x => x.EmployeeNumber == filter.EmployeeNumber.Value);
        if (!string.IsNullOrWhiteSpace(filter.Period))
            bonuses = bonuses.Where(x => x.Period == filter.Period);
        if (!string.IsNullOrWhiteSpace(filter.BonusStateCode))
            bonuses = bonuses.Where(x => x.BonusStateCode == filter.BonusStateCode);

        var totalRecords = await bonuses.CountAsync(cancellationToken);

        // Skip/Take sobre el IQueryable: EF lo traduce a OFFSET/FETCH, la página se arma en la BD.
        var rows = await (
            from bonus in bonuses
            join department in context.Departments on bonus.DepartmentCode equals department.DepartmentCode
            orderby bonus.Period descending, bonus.BonusId descending
            select new BonusListItemDto(
                bonus.BonusId,
                bonus.BonusStateCode,
                bonus.BonusState.StateDescription,
                bonus.Period,
                bonus.DepartmentCode,
                department.DepartmentName,
                bonus.EmployeeNumber,
                bonus.Employee.FullName,
                bonus.BonusTypeId,
                bonus.BonusType.BonusTypeDescription,
                bonus.BonusValue,
                bonus.Reason.ReasonDescription,
                bonus.GrossAmount,
                bonus.TaxAmount,
                bonus.NetAmount))
            .Skip((filter.PageIndex - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResponse<BonusListItemDto>.Create(rows, filter.PageIndex, filter.PageSize, totalRecords);
    }

    public async Task<Employee?> FindEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        await context.Employees.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber, cancellationToken);

    public async Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken) =>
        await context.BonusReasons.AnyAsync(x => x.ReasonCode == reasonCode && x.State, cancellationToken);

    public async Task<bool> HasActiveBonusInPeriodAsync(string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken) =>
        await context.Bonuses.AnyAsync(x =>
            x.DepartmentCode == departmentCode
            && x.EmployeeNumber == employeeNumber
            && x.Period == period
            && x.BonusStateCode != BonusStates.Cancelled
            && !x.IsDeleted, cancellationToken);

    public async Task<Bonus?> FindBonusAsync(int bonusId, CancellationToken cancellationToken) =>
        await context.Bonuses.FirstOrDefaultAsync(x => x.BonusId == bonusId && !x.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken) =>
        await context.EmployeeProjectHours.AsNoTracking()
            .Where(x => x.DepartmentCode == departmentCode && x.EmployeeNumber == employeeNumber)
            .OrderBy(x => x.EmployeeProjectHoursId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Bonus bonus, CancellationToken cancellationToken) =>
        await context.Bonuses.AddAsync(bonus, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}

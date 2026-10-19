using BonusTest.Application.Common;
using BonusTest.Domain.Entities;

namespace BonusTest.Application.Bonuses;

public interface IBonusRepository
{
    Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken);
    Task<Employee?> FindEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task<bool> IsReasonActiveAsync(string reasonCode, CancellationToken cancellationToken);
    Task<bool> HasActiveBonusInPeriodAsync(string departmentCode, int employeeNumber, string period, CancellationToken cancellationToken);
    Task<Bonus?> FindBonusAsync(int bonusId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeProjectHours>> GetProjectHoursAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken);
    Task AddAsync(Bonus bonus, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

using System.Text.RegularExpressions;
using BonusTest.Application.Common;
using BonusTest.Domain.Constants;
using BonusTest.Domain.Entities;
using BonusTest.Domain.Exceptions;

namespace BonusTest.Application.Bonuses;

public class BonusService(IBonusRepository repository)
{
    private static readonly Regex PeriodFormat = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    public Task<PagedResponse<BonusListItemDto>> GetPagedAsync(BonusFilterRequest filter, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(filter, cancellationToken);

    public async Task<EmployeeSummaryDto> SearchEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await GetActiveEmployeeAsync(departmentCode, employeeNumber, cancellationToken);
        return new EmployeeSummaryDto(
            employee.DepartmentCode,
            employee.EmployeeNumber,
            employee.FullName,
            employee.HireDate,
            employee.MonthlySalary,
            employee.TaxRate);
    }

    public async Task<int> RegisterAsync(RegisterBonusRequest request, string? userCode, CancellationToken cancellationToken)
    {
        var user = RequireUser(userCode);
        var employee = await GetActiveEmployeeAsync(request.DepartmentCode, request.EmployeeNumber, cancellationToken);

        ValidatePeriod(request.Period);
        ValidateBonusValue(request.BonusTypeId, request.BonusValue, employee.MonthlySalary);

        if (!await repository.IsReasonActiveAsync(request.ReasonCode, cancellationToken))
            throw new BusinessException("REASON_NOT_ACTIVE", "El motivo no existe o no está activo.");

        if (await repository.HasActiveBonusInPeriodAsync(request.DepartmentCode, request.EmployeeNumber, request.Period, cancellationToken))
            throw new BusinessException("BONUS_ALREADY_EXISTS", "El empleado ya tiene un bono activo en ese periodo.");

        var amounts = BonusCalculator.Calculate(request.BonusTypeId, request.BonusValue, employee.MonthlySalary, employee.TaxRate);

        var bonus = new Bonus
        {
            DepartmentCode = request.DepartmentCode,
            EmployeeNumber = request.EmployeeNumber,
            Period = request.Period,
            BonusTypeId = request.BonusTypeId,
            BonusValue = request.BonusValue,
            ReasonCode = request.ReasonCode,
            GrossAmount = amounts.Gross,
            TaxAmount = amounts.Tax,
            NetAmount = amounts.Net,
            BonusStateCode = BonusStates.Registered,
            CreatedBy = user,
            CreatedAt = DateTime.Now
        };

        await repository.AddAsync(bonus, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return bonus.BonusId;
    }

    public async Task CancelAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var user = RequireUser(userCode);
        var bonus = await GetRegisteredBonusAsync(bonusId, "anular", cancellationToken);

        bonus.BonusStateCode = BonusStates.Cancelled;
        MarkModified(bonus, user);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int bonusId, string? userCode, CancellationToken cancellationToken)
    {
        var user = RequireUser(userCode);
        var bonus = await GetRegisteredBonusAsync(bonusId, "eliminar", cancellationToken);

        bonus.IsDeleted = true;
        MarkModified(bonus, user);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Employee> GetActiveEmployeeAsync(string departmentCode, int employeeNumber, CancellationToken cancellationToken)
    {
        var employee = await repository.FindEmployeeAsync(departmentCode, employeeNumber, cancellationToken)
            ?? throw new BusinessException("EMPLOYEE_NOT_FOUND", "El empleado no existe.");

        if (employee.EmployeeStateCode != EmployeeStates.Active)
            throw new BusinessException("EMPLOYEE_NOT_ACTIVE", "El empleado está cesado; solo se registran bonos a empleados activos.");

        return employee;
    }

    private async Task<Bonus> GetRegisteredBonusAsync(int bonusId, string action, CancellationToken cancellationToken)
    {
        var bonus = await repository.FindBonusAsync(bonusId, cancellationToken)
            ?? throw new BusinessException("BONUS_NOT_FOUND", "El bono no existe.");

        if (bonus.BonusStateCode != BonusStates.Registered)
            throw new BusinessException("BONUS_NOT_REGISTERED", $"Solo se puede {action} un bono en estado Registrado.");

        return bonus;
    }

    private static void ValidatePeriod(string period)
    {
        if (!PeriodFormat.IsMatch(period))
            throw new BusinessException("PERIOD_INVALID", "El periodo debe tener el formato YYYY-MM.");

        // El orden lexicográfico de YYYY-MM coincide con el cronológico.
        if (string.Compare(period, DateTime.Today.ToString("yyyy-MM"), StringComparison.Ordinal) > 0)
            throw new BusinessException("PERIOD_IN_FUTURE", "El periodo no puede ser posterior al mes actual.");
    }

    private static void ValidateBonusValue(int bonusTypeId, decimal bonusValue, decimal monthlySalary)
    {
        switch (bonusTypeId)
        {
            case BonusTypes.FixedAmount when bonusValue <= 0 || bonusValue > monthlySalary:
                throw new BusinessException("AMOUNT_OUT_OF_RANGE", "El monto debe ser mayor a 0 y no mayor al sueldo mensual.");
            case BonusTypes.SalaryPercentage when bonusValue <= 0 || bonusValue > 100:
                throw new BusinessException("PERCENTAGE_OUT_OF_RANGE", "El porcentaje debe ser mayor a 0 y hasta 100.");
            case not (BonusTypes.FixedAmount or BonusTypes.SalaryPercentage):
                throw new BusinessException("BONUS_TYPE_INVALID", "El tipo de bono no es válido.");
        }
    }

    private static void MarkModified(Bonus bonus, string user)
    {
        bonus.ModifiedBy = user;
        bonus.ModifiedAt = DateTime.Now;
    }

    private static string RequireUser(string? userCode) =>
        string.IsNullOrWhiteSpace(userCode)
            ? throw new BusinessException("USER_REQUIRED", "No se identificó al usuario que opera (header UserCode).")
            : userCode.Trim();
}

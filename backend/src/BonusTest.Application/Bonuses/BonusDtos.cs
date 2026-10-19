using BonusTest.Application.Common;

namespace BonusTest.Application.Bonuses;

/// <summary>Filtros opcionales del listado paginado.</summary>
public class BonusFilterRequest : PaginationRequest
{
    public string? DepartmentCode { get; set; }
    public int? EmployeeNumber { get; set; }
    public string? Period { get; set; }
    public string? BonusStateCode { get; set; }
}

public record RegisterBonusRequest(
    string DepartmentCode,
    int EmployeeNumber,
    string Period,
    int BonusTypeId,
    decimal BonusValue,
    string ReasonCode);

public record EmployeeSummaryDto(
    string DepartmentCode,
    int EmployeeNumber,
    string FullName,
    DateTime HireDate,
    decimal MonthlySalary,
    decimal TaxRate);

/// <summary>Fila del listado con las descripciones listas para mostrar.</summary>
public record BonusListItemDto(
    int BonusId,
    string BonusStateCode,
    string BonusStateDescription,
    string Period,
    string DepartmentCode,
    string DepartmentName,
    int EmployeeNumber,
    string EmployeeFullName,
    int BonusTypeId,
    string BonusTypeDescription,
    decimal BonusValue,
    string ReasonDescription,
    decimal GrossAmount,
    decimal TaxAmount,
    decimal NetAmount);

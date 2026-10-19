namespace BonusTest.Domain.Entities;

public class Bonus
{
    public int BonusId { get; set; }
    public string DepartmentCode { get; set; } = null!;
    public int EmployeeNumber { get; set; }
    /// <summary>Periodo del bono en formato YYYY-MM.</summary>
    public string Period { get; set; } = null!;
    public int BonusTypeId { get; set; }
    /// <summary>El monto fijo o el porcentaje ingresado, según el tipo de bono.</summary>
    public decimal BonusValue { get; set; }
    public string ReasonCode { get; set; } = null!;
    public decimal GrossAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string BonusStateCode { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public Employee Employee { get; set; } = null!;
    public BonusType BonusType { get; set; } = null!;
    public BonusReason Reason { get; set; } = null!;
    public State BonusState { get; set; } = null!;
    public List<BonusPaymentDetail> PaymentDetails { get; set; } = [];
}

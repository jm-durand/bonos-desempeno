namespace BonusTest.Domain.Entities;

/// <summary>Distribución del bruto de un bono pagado entre los proyectos del empleado.</summary>
public class BonusPaymentDetail
{
    public int BonusPaymentDetailId { get; set; }
    public int BonusId { get; set; }
    public string ProjectName { get; set; } = null!;
    public decimal Hours { get; set; }
    public decimal Amount { get; set; }
}

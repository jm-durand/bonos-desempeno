using BonusTest.Domain.Constants;

namespace BonusTest.Application.Bonuses;

public record BonusAmounts(decimal Gross, decimal Tax, decimal Net);

/// <summary>
/// Cálculo del bono, aislado y sin dependencias para poder probarlo de forma unitaria.
/// Todos los montos se redondean a 2 decimales con el medio hacia arriba (0.125 → 0.13).
/// </summary>
public static class BonusCalculator
{
    public static BonusAmounts Calculate(int bonusTypeId, decimal bonusValue, decimal monthlySalary, decimal taxRate)
    {
        var gross = bonusTypeId == BonusTypes.FixedAmount
            ? bonusValue
            : monthlySalary * bonusValue / 100m;

        gross = RoundMoney(gross);
        var tax = RoundMoney(gross * taxRate / 100m);
        return new BonusAmounts(gross, tax, gross - tax);
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

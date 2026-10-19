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

    /// <summary>
    /// Reparte un total en proporción a los pesos (horas por proyecto).
    /// Cada parte se redondea a 2 decimales; la última recibe lo que falta
    /// para cuadrar exacto, de modo que la suma siempre es igual al total.
    /// </summary>
    public static IReadOnlyList<decimal> Distribute(decimal total, IReadOnlyList<decimal> weights)
    {
        var totalWeight = weights.Sum();
        var amounts = new decimal[weights.Count];
        decimal assigned = 0;

        for (var i = 0; i < weights.Count; i++)
        {
            if (i == weights.Count - 1)
            {
                amounts[i] = total - assigned;
            }
            else
            {
                amounts[i] = RoundMoney(total * weights[i] / totalWeight);
                assigned += amounts[i];
            }
        }

        return amounts;
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

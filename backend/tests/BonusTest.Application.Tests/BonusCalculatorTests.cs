using BonusTest.Application.Bonuses;
using BonusTest.Domain.Constants;

namespace BonusTest.Application.Tests;

public class BonusCalculatorCalculateTests
{
    [Fact]
    public void PercentageBonus_MatchesStatementExample()
    {
        // TECH1-101: sueldo 5,000.00, retención 8 %, bono de 10 %.
        var amounts = BonusCalculator.Calculate(BonusTypes.SalaryPercentage, 10m, 5000m, 8m);

        Assert.Equal(500.00m, amounts.Gross);
        Assert.Equal(40.00m, amounts.Tax);
        Assert.Equal(460.00m, amounts.Net);
    }

    [Fact]
    public void FixedAmountBonus_GrossIsTheAmountItself()
    {
        var amounts = BonusCalculator.Calculate(BonusTypes.FixedAmount, 1200.50m, 3000m, 10m);

        Assert.Equal(1200.50m, amounts.Gross);
        Assert.Equal(120.05m, amounts.Tax);
        Assert.Equal(1080.45m, amounts.Net);
    }

    [Fact]
    public void ZeroTaxRate_NetEqualsGross()
    {
        // VENTA-201 tiene TaxRate 0.
        var amounts = BonusCalculator.Calculate(BonusTypes.SalaryPercentage, 50m, 2500m, 0m);

        Assert.Equal(1250.00m, amounts.Gross);
        Assert.Equal(0.00m, amounts.Tax);
        Assert.Equal(1250.00m, amounts.Net);
    }

    [Fact]
    public void MidpointRoundsAwayFromZero_NotBankers()
    {
        // 0.125 debe dar 0.13 (el enunciado lo fija); Math.Round por defecto (banker's) daría 0.12.
        var amounts = BonusCalculator.Calculate(BonusTypes.FixedAmount, 0.125m, 1000m, 0m);

        Assert.Equal(0.13m, amounts.Gross);
    }

    [Fact]
    public void TaxIsRoundedBeforeComputingNet()
    {
        // Bruto 100.25 × 10 % = 10.025 → retención 10.03; neto = 100.25 − 10.03.
        var amounts = BonusCalculator.Calculate(BonusTypes.FixedAmount, 100.25m, 1000m, 10m);

        Assert.Equal(10.03m, amounts.Tax);
        Assert.Equal(90.22m, amounts.Net);
    }

    [Fact]
    public void PercentageGrossIsRoundedToTwoDecimals()
    {
        // 3,333.33 × 10 % = 333.333 → 333.33.
        var amounts = BonusCalculator.Calculate(BonusTypes.SalaryPercentage, 10m, 3333.33m, 0m);

        Assert.Equal(333.33m, amounts.Gross);
    }
}

public class BonusCalculatorDistributeTests
{
    [Fact]
    public void LastProjectReceivesTheAdjustmentSoTheSumIsExact()
    {
        // Caso real probado contra la BD: 100.01 entre 30/50/20 horas.
        // 30.003 → 30.00; 50.005 → 50.01; el último recibe 100.01 − 80.01 = 20.00.
        var amounts = BonusCalculator.Distribute(100.01m, [30m, 50m, 20m]);

        Assert.Equal([30.00m, 50.01m, 20.00m], amounts);
        Assert.Equal(100.01m, amounts.Sum());
    }

    [Fact]
    public void SingleProjectReceivesEverything()
    {
        var amounts = BonusCalculator.Distribute(500m, [160m]);

        Assert.Equal([500m], amounts);
    }

    [Fact]
    public void ProportionalSplitWithExactThirds()
    {
        // 100 entre pesos iguales: 33.33 + 33.33 y el último cuadra con 33.34.
        var amounts = BonusCalculator.Distribute(100m, [1m, 1m, 1m]);

        Assert.Equal([33.33m, 33.33m, 33.34m], amounts);
    }

    [Fact]
    public void DistributionIsProportionalToHours()
    {
        // TECH1-101: 120 y 40 horas → 3/4 y 1/4 del bruto.
        var amounts = BonusCalculator.Distribute(500m, [120m, 40m]);

        Assert.Equal([375.00m, 125.00m], amounts);
    }

    [Theory]
    [InlineData(100.01, new[] { 30.0, 50.0, 20.0 })]
    [InlineData(999.99, new[] { 1.0, 2.0, 3.0, 4.0 })]
    [InlineData(0.01, new[] { 7.0, 11.0 })]
    [InlineData(1234.56, new[] { 45.0, 35.0, 20.0 })]
    public void SumAlwaysEqualsTheTotal(double total, double[] hours)
    {
        var amounts = BonusCalculator.Distribute((decimal)total, hours.Select(x => (decimal)x).ToList());

        Assert.Equal((decimal)total, amounts.Sum());
    }
}

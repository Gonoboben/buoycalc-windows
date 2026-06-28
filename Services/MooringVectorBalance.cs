using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringVectorBalanceTerm(
    string Name,
    string Kind,
    double FxN,
    double FzN,
    string Note);

public sealed record MooringVectorBalanceResult(
    IReadOnlyList<MooringVectorBalanceTerm> Terms,
    double SumExternalFxN,
    double SumExternalFzN,
    double RequiredReactionFxN,
    double RequiredReactionFzN,
    double AnchorHorizontalCapacityN,
    double AnchorHorizontalReserve,
    bool IsSolved,
    string MethodNote);

public static class MooringVectorBalance
{
    private const double G = 9.80665;

    public static MooringVectorBalanceResult Build(CalculationResult result)
    {
        var terms = new List<MooringVectorBalanceTerm>();

        foreach (var row in result.ElementRows)
        {
            var fxN = row.CurrentForceN;
            var fzN = -row.WeightWaterKg * G;

            terms.Add(new MooringVectorBalanceTerm(
                row.Title,
                row.Kind,
                fxN,
                fzN,
                "Fx: сила течения из расчётной строки элемента; Fz: вес в воде с положительным направлением вверх."));
        }

        if (Math.Abs(result.WaveForceN) > 0)
        {
            terms.Add(new MooringVectorBalanceTerm(
                "Волновая нагрузка на буй",
                "Волна",
                result.WaveForceN,
                0,
                "Горизонтальная волновая добавка из расчётного ядра."));
        }

        var sumFxN = terms.Sum(x => x.FxN);
        var sumFzN = terms.Sum(x => x.FzN);
        var requiredReactionFxN = -sumFxN;
        var requiredReactionFzN = -sumFzN;
        var anchorCapacityN = Math.Max(0, result.AnchorHoldingKg) * G;
        var anchorReserve = Math.Abs(requiredReactionFxN) > 0
            ? anchorCapacityN / Math.Abs(requiredReactionFxN)
            : 0;

        return new MooringVectorBalanceResult(
            terms,
            sumFxN,
            sumFzN,
            requiredReactionFxN,
            requiredReactionFzN,
            anchorCapacityN,
            anchorReserve,
            IsSolved: false,
            MethodNote: "v0.28: силовые члены собраны в единую векторную ведомость. Реакции пока вычислены как требуемые для замыкания ΣF=0, но не найдены итерационным solver равновесной формы.");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringSequencePositionRow(
    int Number,
    string Kind,
    string Title,
    string PresetName,
    double StartAlongLineM,
    double EndAlongLineM,
    double PositionAlongLineM,
    double LengthM,
    double WeightWaterKg,
    double CurrentForceN,
    bool IsDistributed,
    bool IsDiscrete,
    string SolverRole,
    string NextStepNote);

public sealed record MooringSequencePositionResult(
    IReadOnlyList<MooringSequencePositionRow> Rows,
    double TotalLineLengthM,
    int DistributedElementCount,
    int DiscreteElementCount,
    double DiscreteWeightWaterKg,
    double DiscreteCurrentForceN,
    string MethodNote);

public static class MooringSequencePositioner
{
    public static MooringSequencePositionResult Build(CalculationResult result)
    {
        var rows = new List<MooringSequencePositionRow>();
        var sM = 0.0;

        foreach (var element in result.ElementRows.OrderBy(x => x.Number))
        {
            var isLine = string.Equals(element.Kind, "Линия", StringComparison.OrdinalIgnoreCase);
            var isBuoy = string.Equals(element.Kind, "Буй", StringComparison.OrdinalIgnoreCase);
            var isAnchor = string.Equals(element.Kind, "Якорь", StringComparison.OrdinalIgnoreCase);
            var isDiscrete = !isLine;
            var startM = sM;
            var endM = isLine ? sM + Math.Max(0, element.LengthM) : sM;
            var positionM = isLine ? (startM + endM) / 2.0 : sM;

            var solverRole = element.Kind switch
            {
                "Буй" => "верхний граничный узел",
                "Якорь" => "нижний граничный узел",
                "Линия" => "распределённые сегменты формы и натяжений",
                _ => "дискретная нагрузка по s в натяжениях и кандидатной форме"
            };

            var nextStep = element.Kind switch
            {
                "Линия" => "участвует как распределённая линия",
                "Буй" => "участвует как верхнее граничное условие плавучести",
                "Якорь" => "участвует как нижнее граничное условие удержания",
                _ => "локальные вес в воде и сила учитываются ниже этой точки s"
            };

            rows.Add(new MooringSequencePositionRow(
                element.Number,
                element.Kind,
                element.Title,
                element.PresetName,
                startM,
                endM,
                positionM,
                element.LengthM,
                element.WeightWaterKg,
                element.CurrentForceN,
                isLine,
                isDiscrete,
                solverRole,
                nextStep));

            if (isLine)
            {
                sM = endM;
            }
        }

        var discreteRows = rows.Where(x => x.IsDiscrete && x.Kind != "Буй" && x.Kind != "Якорь").ToList();
        return new MooringSequencePositionResult(
            rows,
            sM,
            rows.Count(x => x.IsDistributed),
            discreteRows.Count,
            discreteRows.Sum(x => x.WeightWaterKg),
            discreteRows.Sum(x => x.CurrentForceN),
            "Позиционная модель задаёт координату s вдоль линии для каждого элемента. Линии занимают распределённые интервалы; соединители и приборы передают локальные вес в воде и горизонтальную силу в модель дискретных нагрузок и итерационный feedback-цикл кандидатной формы.");
    }
}

#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BuoyCalc.Windows.Models;
using SkiaSharp;

namespace BuoyCalc.Windows.Services;

public static class PdfReportBuilder
{
    private const float PageWidth = 595;
    private const float PageHeight = 842;
    private const float Margin = 36;
    private const float LineGap = 5;

    public static void Build(string filePath, UserEngineeringReportReadModel report)
    {
        ArgumentNullException.ThrowIfNull(report);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");

        using var stream = File.Open(filePath, FileMode.Create, FileAccess.Write);
        using var document = SKDocument.CreatePdf(stream);
        using var regularTypeface = SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;
        using var boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? regularTypeface;

        var writer = new PdfCanvasWriter(document, regularTypeface, boldTypeface);
        var diagramRows = ToDiagramRows(report.Elements);
        var diagram = report.SelectedShape is null
            ? null
            : Mooring2DDiagramReadModelBuilder.Build(report.SelectedShape, diagramRows);

        WriteExecutivePage(writer, report);
        WriteConditionsPage(writer, report);
        WriteCompositionPage(writer, report);
        WriteGeometryPage(writer, report, diagram);
        WriteBuoyancyAndLoadsPage(writer, report);
        WriteStructuralCapacityPage(writer, report);
        WriteAnchorPage(writer, report);
        WriteAssessmentPage(writer, report);

        document.Close();
    }

    private static void WriteExecutivePage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        var assessment = report.Assessment;
        var verdict = assessment?.Verdict ?? "Требуется проверка";
        var mainRisk = assessment?.MainRisk ?? "Selected F1-F4 инженерная оценка недоступна для этого расчёта.";
        var designTension = report.DesignLoad is null ? "не определена" : $"{report.DesignLoad.DemandKn:0.##} кН";
        var governingReserve = report.Structural?.GoverningReserve is double reserve
            ? reserve.ToString("0.##", CultureInfo.InvariantCulture)
            : "не определён";
        var offset = report.SelectedShape is null
            ? "не определён"
            : $"{report.SelectedShape.Shape.HorizontalOffsetM:0.##} м";
        var anchorContact = report.AnchorReaction is null
            ? "не определён"
            : AnchorContactText(report.AnchorReaction.ContactClassification);

        writer.BeginPage();
        writer.Title("BuoyCalc Windows — инженерный отчёт постановки");
        writer.Text($"Проект: {report.ProjectName}", 11);
        writer.Text($"Версия приложения и расчётной модели: {AppInfo.DisplayVersion}", 9.5f);
        writer.Space(10);
        writer.VerdictBanner(verdict, mainRisk);
        writer.Space(12);
        writer.Section("Ключевые показатели");
        writer.KeyValueTable(new[]
        {
            ("Глубина постановки", $"{report.Environment.DepthM:0.##} м"),
            ("Длина линии", $"{report.Calculation.LineLengthM:0.##} м"),
            ("Selected design-нагрузка", designTension),
            ("Определяющий локальный запас", governingReserve),
            ("Снос selected X/Z", offset),
            ("Контакт якоря", anchorContact),
            ("Чистая плавучесть", $"{report.Calculation.NetBuoyancyKg:0.##} кг"),
            ("Расчётное течение", $"{report.Environment.EffectiveCurrentSpeedMS:0.###} м/с")
        });
        writer.Space(12);
        writer.Section("Граница применимости решения");
        writer.Text(
            "PDF отображает только уже рассчитанное состояние. Горизонтальная удерживающая способность системы якорь/грунт в v1 не является валидированной selected-capacity моделью и не может подтверждать итоговый проход по якорю; требуется отдельная физическая проверка якоря и грунта.",
            9.5f);
        writer.EndPage();
    }

    private static void WriteConditionsPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        var env = report.Environment;

        writer.BeginPage();
        writer.Title("Исходные условия расчёта");
        writer.KeyValueTable(new[]
        {
            ("Плотность воды, ввод", $"{env.WaterDensityKgM3:0.##} кг/м³"),
            ("Плотность воды, эффективная", $"{env.EffectiveWaterDensityKgM3:0.##} кг/м³"),
            ("Глубина", $"{env.DepthM:0.##} м"),
            ("Скорость течения, ввод", $"{env.CurrentSpeedMS:0.###} м/с"),
            ("Скорость течения, расчётная", $"{env.EffectiveCurrentSpeedMS:0.###} м/с"),
            ("Высота волны", $"{env.WaveHeightM:0.##} м"),
            ("Период волны", $"{env.WavePeriodS:0.##} с"),
            ("Грунт", $"{env.SeabedName} · K={env.SeabedHoldingMultiplier:0.##}")
        });

        writer.Space(10);
        writer.Section("Течение");
        if (env.UsesCurrentProfile && env.CurrentProfile.Count > 0)
        {
            writer.Text("Используется заданный профиль течения по глубине.", 9.5f);
            writer.CurrentProfileTable(env.CurrentProfile);
        }
        else
        {
            writer.Text($"Профиль течения отключён; используется одно значение {env.EffectiveCurrentSpeedMS:0.###} м/с.", 9.5f);
        }

        writer.Space(10);
        writer.Section("Буй");
        writer.KeyValueTable(new[]
        {
            ("Наименование", report.Buoy.Name),
            ("Объём", $"{report.Buoy.VolumeM3:0.####} м³"),
            ("Масса в воздухе", $"{report.Buoy.WeightAirKg:0.##} кг"),
            ("Проекционная площадь", $"{report.Buoy.ProjectedAreaM2:0.####} м²"),
            ("Cd", report.Buoy.DragCoefficient.ToString("0.###", CultureInfo.InvariantCulture))
        });

        writer.Space(10);
        writer.Section("Якорь");
        writer.KeyValueTable(new[]
        {
            ("Наименование", report.Anchor.Name),
            ("Тип", report.Anchor.Type),
            ("Материал", report.Anchor.Material),
            ("Масса в воздухе", $"{report.Anchor.WeightAirKg:0.##} кг"),
            ("Объём", $"{report.Anchor.VolumeM3:0.####} м³"),
            ("Вес в воде", $"{report.Anchor.WeightWaterKg:0.##} кг")
        });
        writer.EndPage();
    }

    private static void WriteCompositionPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        writer.BeginPage();
        writer.Title("Состав постановки");
        writer.Text("Последовательность приведена сверху вниз — от буя к якорю. Значения взяты из сохранённой таблицы выполненного расчёта.", 9.5f);
        writer.Space(10);
        writer.ElementTable(report.Elements);
        writer.Space(10);
        writer.Text(
            "Локальная прочность и selected design-нагрузки приведены в последующих инженерных разделах отчёта; legacy reserve в этой таблице не используется как selected-authority.",
            9.2f);
        writer.EndPage();
    }

    private static void WriteGeometryPage(
        PdfCanvasWriter writer,
        UserEngineeringReportReadModel report,
        Mooring2DDiagramReadModel? diagram)
    {
        writer.BeginPage();
        writer.Title("Расчётная геометрия постановки X/Z");
        if (diagram is not null)
        {
            writer.Text(
                "Схема построена только по выбранной расчётной форме X/Z через общий presentation read model. PDF не выбирает кандидата, не восстанавливает координаты из текста и не создаёт fallback-геометрию.",
                9.5f);
            writer.Space(10);
            writer.SelectedShapeDiagram(diagram);
        }
        else
        {
            writer.Text("Выбранная расчётная форма X/Z недоступна. PDF не строит приблизительную инженерную схему без рассчитанных X/Z-узлов.", 10);
            writer.Text($"Контекст расчёта: глубина {report.Environment.DepthM:0.##} м; длина линии {report.Calculation.LineLengthM:0.##} м.", 10);
        }
        writer.EndPage();
    }

    private static void WriteBuoyancyAndLoadsPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        writer.BeginPage();
        writer.Title("Плавучесть и расчётные нагрузки");
        writer.Section("Баланс плавучести");
        writer.KeyValueTable(new[]
        {
            ("Полная плавучесть", $"{report.Calculation.BuoyancyKg:0.##} кг"),
            ("Суммарный вес в воде", $"{report.Calculation.TotalWeightWaterKg:0.##} кг"),
            ("Чистая плавучесть", $"{report.Calculation.NetBuoyancyKg:0.##} кг"),
            ("Коэффициент запаса SF", report.Calculation.SafetyFactor.ToString("0.###", CultureInfo.InvariantCulture))
        });

        writer.Space(12);
        writer.Section("Горизонтальные воздействия, сохранённые расчётом");
        writer.KeyValueTable(new[]
        {
            ("Суммарная сила течения", $"{report.Calculation.CurrentForceN:0.##} Н"),
            ("Волновой horizontal proxy", $"{report.Calculation.WaveForceN:0.##} Н"),
            ("Legacy horizontal sum", $"{report.Calculation.LegacyHorizontalForceN:0.##} Н")
        });
        writer.Text("Волновое значение — существующий квазистатический horizontal design proxy v1; это не динамический или time-domain расчёт.", 9.2f);

        writer.Space(12);
        writer.Section("Selected F1 design-нагрузка");
        if (report.DesignLoad is null)
        {
            writer.Text("Selected F1 design-нагрузка недоступна для этого расчёта.", 10);
        }
        else
        {
            var load = report.DesignLoad;
            var governingLocation = DesignLocationText(load.LocationKind);
            if (load.SegmentNumber.HasValue)
            {
                governingLocation += $" · сегмент {load.SegmentNumber.Value}";
            }
            if (!string.IsNullOrWhiteSpace(load.SourceElement))
            {
                governingLocation += $" · {load.SourceElement}";
            }

            writer.KeyValueTable(new[]
            {
                ("Определяющая design-нагрузка", $"{load.DemandKn:0.##} кН"),
                ("Расположение", governingLocation),
                ("Координата s", $"{load.AlongLineM:0.##} м"),
                ("Wave increment", $"{load.WaveHorizontalIncrementN:0.##} Н"),
                ("Поверхность: H / V / T", $"{load.SurfaceDesignHN:0.##} / {load.SurfaceDesignVN:0.##} / {load.SurfaceDesignTensionN:0.##} Н"),
                ("Якорь: H / V / T", $"{load.AnchorDesignHN:0.##} / {load.AnchorDesignVN:0.##} / {load.AnchorDesignTensionN:0.##} Н"),
                ("Макс. midpoint", $"сегмент {load.MaxDesignMidpointSegmentNumber}; {load.MaxDesignMidpointTensionN:0.##} Н"),
                ("Источник authority", load.SourceIdentity.ToString())
            });
        }
        writer.EndPage();
    }

    private static void WriteStructuralCapacityPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        writer.BeginPage();
        writer.Title("Локальная прочность элементов — F3");

        if (report.Structural is null)
        {
            writer.Text("Selected F3 локальная capacity-модель недоступна для этого расчёта.", 10);
            writer.EndPage();
            return;
        }

        var structural = report.Structural;
        var governing = structural.GoverningElementNumber.HasValue
            ? $"#{structural.GoverningElementNumber.Value} {structural.GoverningTitle} / {structural.GoverningPresetName}"
            : "не определён";
        var reserve = structural.GoverningReserve.HasValue
            ? structural.GoverningReserve.Value.ToString("0.##", CultureInfo.InvariantCulture)
            : "не определён";

        writer.KeyValueTable(new[]
        {
            ("Структурных элементов ожидается", structural.ExpectedStructuralElementCount.ToString(CultureInfo.InvariantCulture)),
            ("Оценено по capacity", structural.RatedStructuralElementCount.ToString(CultureInfo.InvariantCulture)),
            ("Неполное покрытие", structural.IncompleteStructuralElementCount.ToString(CultureInfo.InvariantCulture)),
            ("Недостаточная прочность", structural.InsufficientElementCount.ToString(CultureInfo.InvariantCulture)),
            ("Покрытие complete", structural.CoverageComplete ? "да" : "нет"),
            ("Определяющий элемент", governing),
            ("Определяющий локальный запас", reserve)
        });
        writer.Space(10);
        writer.StructuralTable(structural.Rows);
        writer.Space(10);
        writer.Text(
            "Приборы/payload могут иметь рассчитанную локальную нагрузку, но в текущей модели не имеют MBL capacity-поля и поэтому не считаются rated structural element. Для соединителя Count должен быть ровно 1; масштабирование прочности параллельных/последовательных соединителей не предполагается.",
            9.1f);
        writer.EndPage();
    }

    private static void WriteAnchorPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        writer.BeginPage();
        writer.Title("Якорь и контакт с грунтом — F2");

        if (report.AnchorReaction is null)
        {
            writer.Text("Selected F2 реакция якорной границы недоступна для этого расчёта.", 10);
        }
        else
        {
            var reaction = report.AnchorReaction;
            writer.Section("Selected реакция якорной границы");
            writer.KeyValueTable(new[]
            {
                ("Горизонтальная нагрузка", $"{reaction.HorizontalDemandN:0.##} Н"),
                ("Подъёмная составляющая линии", $"{reaction.UpwardLinePullN:0.##} Н"),
                ("Прижимающая составляющая линии", $"{reaction.DownwardLinePushN:0.##} Н"),
                ("Вес якоря в воде", $"{reaction.AnchorWeightWaterKg:0.##} кг / {reaction.AnchorWeightWaterN:0.##} Н"),
                ("Signed normal reaction", $"{reaction.SignedNormalReactionN:0.##} Н"),
                ("Compressive normal reaction", $"{reaction.CompressiveNormalReactionN:0.##} Н"),
                ("Uplift excess", $"{reaction.UpliftExcessN:0.##} Н"),
                ("Классификация контакта", AnchorContactText(reaction.ContactClassification)),
                ("Источник authority", reaction.SourceIdentity.ToString())
            });
        }

        writer.Space(14);
        writer.Section("СПРАВОЧНО: legacy holding estimate — compatibility only");
        writer.Text(
            "Следующие значения сохранены расчётным ядром для совместимости с прежним отчётом. Они НЕ являются валидированной selected-моделью горизонтальной удерживающей способности якорь/грунт и не могут самостоятельно разрешить итоговый проход.",
            9.3f);
        writer.KeyValueTable(new[]
        {
            ("Base holding coefficient", report.Anchor.BaseHoldingCoefficient.ToString("0.###", CultureInfo.InvariantCulture)),
            ("Legacy type multiplier", report.Anchor.LegacyTypeMultiplier.ToString("0.###", CultureInfo.InvariantCulture)),
            ("Legacy seabed multiplier", report.Anchor.LegacySeabedMultiplier.ToString("0.###", CultureInfo.InvariantCulture)),
            ("Legacy holding estimate", $"{report.Anchor.LegacyHoldingKg:0.##} кг"),
            ("Legacy required holding", $"{report.Anchor.LegacyRequiredHoldingKg:0.##} кг"),
            ("Legacy reserve", report.Anchor.LegacyReserve.ToString("0.##", CultureInfo.InvariantCulture))
        });
        writer.Text("Для окончательной проверки требуется отдельная физически валидированная модель конкретного якоря и фактического грунта/контакта.", 9.3f);
        writer.EndPage();
    }

    private static void WriteAssessmentPage(PdfCanvasWriter writer, UserEngineeringReportReadModel report)
    {
        writer.BeginPage();
        writer.Title("Инженерные проверки и заключение — F4");

        if (report.Assessment is null)
        {
            writer.VerdictBanner("Требуется проверка", "Selected F4 engineering assessment недоступна для этого расчёта.");
            writer.EndPage();
            return;
        }

        var assessment = report.Assessment;
        writer.VerdictBanner(assessment.Verdict, assessment.MainRisk);
        writer.Space(12);
        writer.Section("Проверки");
        foreach (var check in assessment.Checks)
        {
            writer.CheckBlock(CheckStatusText(check.Status), check.Summary, check.Detail);
        }

        writer.Space(10);
        writer.Section("Итоговое инженерное заключение");
        if (assessment.HasHardFailure)
        {
            writer.Text("Постановка имеет как минимум одно жёсткое невыполненное условие и по selected F4 assessment имеет вердикт «Не подходит». До устранения hard-failure переход к эксплуатации не рекомендуется.", 10);
        }
        else if (assessment.RequiresReview)
        {
            writer.Text("Жёстких базовых отказов не выявлено, но selected F4 assessment требует инженерной проверки. Перед эксплуатацией необходимо закрыть все review-пункты, прежде всего физическую проверку горизонтальной удерживающей способности якоря на фактическом грунте.", 10);
        }
        else
        {
            writer.Text("По доступным selected F4 проверкам жёстких отказов и review-пунктов не выявлено. Решение остаётся ограничено допущениями текущей квазистатической модели и областью валидированных расчётных authority.", 10);
        }

        writer.Space(8);
        writer.Text($"Main risk code: {assessment.MainRiskCode}", 9);
        writer.Text($"Anchor horizontal capacity disposition: {assessment.AnchorHorizontalCapacityDisposition}", 9);
        writer.Text($"Selected authority source: {assessment.SourceIdentity}", 9);
        writer.EndPage();
    }

    private static IReadOnlyList<ElementCalculationDisplayRow> ToDiagramRows(
        IReadOnlyList<UserEngineeringElementReadModel> elements)
    {
        return elements
            .OrderBy(x => x.Number)
            .Select(x => new ElementCalculationDisplayRow
            {
                Number = x.Number,
                Kind = x.Kind,
                Title = x.Title,
                PresetName = x.PresetName,
                LengthM = Format(x.LengthM),
                SourceLengthM = x.LengthM,
                Count = x.Count.ToString(CultureInfo.InvariantCulture),
                WeightWaterKg = Format(x.WeightWaterKg),
                ProjectedAreaM2 = Format(x.ProjectedAreaM2),
                DragCoefficient = Format(x.DragCoefficient),
                CurrentForceN = Format(x.CurrentForceN),
                BreakingLoadKn = Format(x.BreakingLoadKn),
                WorkingLoadKn = Format(x.WorkingLoadKn),
                Reserve = Format(x.LegacyReserve),
                Status = UserStatusPolicy.ToUserStatus(x.LegacyStatus)
            })
            .ToArray();
    }

    private static string AnchorContactText(MooringAnchorContactClassification classification)
    {
        return classification switch
        {
            MooringAnchorContactClassification.CompressiveContact => "сжимающий контакт",
            MooringAnchorContactClassification.ZeroNormalLimit => "предел нулевой нормальной реакции",
            MooringAnchorContactClassification.UpliftSeparation => "расчётный отрыв от грунта",
            _ => classification.ToString()
        };
    }

    private static string DesignLocationText(MooringDesignTensionLocationKind kind)
    {
        return kind switch
        {
            MooringDesignTensionLocationKind.Surface => "поверхность / буй",
            MooringDesignTensionLocationKind.AnchorEnd => "якорная граница",
            MooringDesignTensionLocationKind.Midpoint => "локальный midpoint сегмента",
            _ => kind.ToString()
        };
    }

    private static string StructuralStatusText(MooringLocalStructuralCapacityStatus status)
    {
        return status switch
        {
            MooringLocalStructuralCapacityStatus.Ok => "ОК",
            MooringLocalStructuralCapacityStatus.Insufficient => "НЕДОСТАТОЧНО",
            MooringLocalStructuralCapacityStatus.DemandUnavailable => "нет demand",
            MooringLocalStructuralCapacityStatus.CapacityUnavailable => "нет capacity",
            MooringLocalStructuralCapacityStatus.SafetyFactorUnavailable => "нет SF",
            MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount => "Count не поддержан",
            MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel => "не rated",
            MooringLocalStructuralCapacityStatus.NoPositiveDemand => "нет +demand",
            _ => status.ToString()
        };
    }

    private static string CheckStatusText(MooringEngineeringAssessmentCheckStatus status)
    {
        return status switch
        {
            MooringEngineeringAssessmentCheckStatus.Ok => "ОК",
            MooringEngineeringAssessmentCheckStatus.RequiresReview => "ПРОВЕРИТЬ",
            MooringEngineeringAssessmentCheckStatus.HardFailure => "НЕ ПРОХОДИТ",
            _ => status.ToString()
        };
    }

    private static string Nullable(double? value, string format = "0.##")
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? value.Value.ToString(format, CultureInfo.InvariantCulture)
            : "—";
    }

    private static string Format(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    private sealed class PdfCanvasWriter
    {
        private readonly SKDocument _document;
        private readonly SKTypeface _regularTypeface;
        private readonly SKTypeface _boldTypeface;
        private SKCanvas? _canvas;
        private int _pageNumber;
        private float _y;

        public PdfCanvasWriter(SKDocument document, SKTypeface regularTypeface, SKTypeface boldTypeface)
        {
            _document = document;
            _regularTypeface = regularTypeface;
            _boldTypeface = boldTypeface;
        }

        public void BeginPage()
        {
            _canvas = _document.BeginPage(PageWidth, PageHeight);
            _pageNumber++;
            _y = Margin;
            DrawFooter();
        }

        public void EndPage()
        {
            _document.EndPage();
            _canvas = null;
        }

        public void Title(string text)
        {
            DrawText(text, 19, true);
            Space(6);
        }

        public void Section(string text)
        {
            EnsureSpace(34);
            DrawText(text, 13, true);
            Space(5);
        }

        public void Text(string text, float size = 10)
        {
            foreach (var line in Wrap(text, size, PageWidth - 2 * Margin))
            {
                DrawText(line, size, false);
            }
        }

        public void VerdictBanner(string verdict, string mainRisk)
        {
            const float height = 86;
            EnsureSpace(height + 8);
            var rect = new SKRect(Margin, _y, PageWidth - Margin, _y + height);
            var background = verdict.StartsWith("Не подходит", StringComparison.OrdinalIgnoreCase)
                ? new SKColor(255, 236, 236)
                : verdict.StartsWith("Подходит", StringComparison.OrdinalIgnoreCase)
                    ? new SKColor(235, 248, 239)
                    : new SKColor(255, 247, 226);
            var accent = verdict.StartsWith("Не подходит", StringComparison.OrdinalIgnoreCase)
                ? new SKColor(173, 45, 45)
                : verdict.StartsWith("Подходит", StringComparison.OrdinalIgnoreCase)
                    ? new SKColor(46, 125, 69)
                    : new SKColor(177, 108, 0);

            using var fill = new SKPaint { Color = background, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = accent, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, IsAntialias = true };
            _canvas!.DrawRoundRect(rect, 6, 6, fill);
            _canvas.DrawRoundRect(rect, 6, 6, border);
            DrawTextAt("ИНЖЕНЕРНЫЙ ВЕРДИКТ", rect.Left + 12, rect.Top + 18, 8.5f, true, accent);
            DrawTextAt(verdict, rect.Left + 12, rect.Top + 41, 16, true, accent);

            var riskLines = Wrap(mainRisk, 8.8f, rect.Width - 24).Take(3).ToList();
            for (var i = 0; i < riskLines.Count; i++)
            {
                DrawTextAt(riskLines[i], rect.Left + 12, rect.Top + 59 + i * 11, 8.8f, false, new SKColor(70, 70, 70));
            }
            _y += height;
        }

        public void KeyValueTable(IReadOnlyList<(string Label, string Value)> rows)
        {
            const float labelWidth = 210;
            var widths = new[] { labelWidth, PageWidth - 2 * Margin - labelWidth };
            foreach (var row in rows)
            {
                DrawTableRow(new[] { row.Label, row.Value }, widths, false, 22, 8.5f, 44, 62);
            }
        }

        public void CurrentProfileTable(IReadOnlyList<CurrentProfilePointInput> points)
        {
            var headers = new[] { "Глубина, м", "East, м/с", "North, м/с", "Vertical, м/с", "ρ, кг/м³" };
            var widths = new[] { 92f, 102f, 102f, 105f, 122f };
            DrawTableRow(headers, widths, true, 22, 7.9f, 18, 18);
            foreach (var p in points.OrderBy(x => x.DepthM))
            {
                DrawTableRow(
                    new[]
                    {
                        p.DepthM.ToString("0.##", CultureInfo.InvariantCulture),
                        p.EastCurrentMS.ToString("0.###", CultureInfo.InvariantCulture),
                        p.NorthCurrentMS.ToString("0.###", CultureInfo.InvariantCulture),
                        p.VerticalCurrentMS.ToString("0.###", CultureInfo.InvariantCulture),
                        p.WaterDensityKgM3.ToString("0.##", CultureInfo.InvariantCulture)
                    },
                    widths,
                    false,
                    21,
                    7.5f,
                    18,
                    18);
            }
        }

        public void ElementTable(IReadOnlyList<UserEngineeringElementReadModel> rows)
        {
            var headers = new[] { "№", "Тип", "Элемент / исполнение", "L / n", "Вес в воде, кг", "Fx, Н" };
            var widths = new[] { 24f, 65f, 190f, 55f, 94f, 95f };
            DrawTableRow(headers, widths, true, 24, 7.8f, 16, 18);

            foreach (var row in rows.OrderBy(x => x.Number))
            {
                var lengthOrCount = row.LengthM > 0
                    ? $"{row.LengthM:0.##} м"
                    : $"n={row.Count}";
                var title = string.IsNullOrWhiteSpace(row.PresetName)
                    ? row.Title
                    : $"{row.Title} / {row.PresetName}";
                DrawTableRow(
                    new[]
                    {
                        row.Number.ToString(CultureInfo.InvariantCulture),
                        row.Kind,
                        title,
                        lengthOrCount,
                        row.WeightWaterKg.ToString("0.##", CultureInfo.InvariantCulture),
                        row.CurrentForceN.ToString("0.##", CultureInfo.InvariantCulture)
                    },
                    widths,
                    false,
                    24,
                    7.4f,
                    16,
                    34);
            }
        }

        public void StructuralTable(IReadOnlyList<UserEngineeringStructuralRowReadModel> rows)
        {
            var headers = new[] { "№", "Элемент", "Demand, кН", "MBL, кН", "WLL, кН", "Запас", "Статус" };
            var widths = new[] { 26f, 145f, 68f, 62f, 62f, 56f, 104f };
            DrawTableRow(headers, widths, true, 23, 6.9f, 14, 18);

            foreach (var row in rows.OrderBy(x => x.ElementNumber))
            {
                var title = string.IsNullOrWhiteSpace(row.PresetName)
                    ? row.Title
                    : $"{row.Title} / {row.PresetName}";
                DrawTableRow(
                    new[]
                    {
                        row.ElementNumber.ToString(CultureInfo.InvariantCulture),
                        title,
                        Nullable(row.LocalDesignDemandKn),
                        Nullable(row.BreakingLoadKn),
                        Nullable(row.WorkingLoadKn),
                        Nullable(row.LocalReserve),
                        StructuralStatusText(row.Status)
                    },
                    widths,
                    false,
                    23,
                    6.7f,
                    14,
                    27);
            }
        }

        public void CheckBlock(string status, string summary, string detail)
        {
            var statusColor = status == "НЕ ПРОХОДИТ"
                ? new SKColor(173, 45, 45)
                : status == "ПРОВЕРИТЬ"
                    ? new SKColor(177, 108, 0)
                    : new SKColor(46, 125, 69);
            var summaryLines = Wrap(summary, 9.2f, PageWidth - 2 * Margin - 94).Take(2).ToList();
            var detailLines = Wrap(detail, 8.2f, PageWidth - 2 * Margin - 24).Take(3).ToList();
            var height = 34 + summaryLines.Count * 11 + detailLines.Count * 10;
            EnsureSpace(height + 6);

            var rect = new SKRect(Margin, _y, PageWidth - Margin, _y + height);
            using var fill = new SKPaint { Color = new SKColor(249, 250, 252), Style = SKPaintStyle.Fill, IsAntialias = true };
            using var border = new SKPaint { Color = new SKColor(215, 222, 233), Style = SKPaintStyle.Stroke, StrokeWidth = 0.8f, IsAntialias = true };
            _canvas!.DrawRoundRect(rect, 4, 4, fill);
            _canvas.DrawRoundRect(rect, 4, 4, border);
            DrawTextAt(status, rect.Left + 10, rect.Top + 18, 8.2f, true, statusColor);

            for (var i = 0; i < summaryLines.Count; i++)
            {
                DrawTextAt(summaryLines[i], rect.Left + 92, rect.Top + 18 + i * 11, 9.2f, true, SKColors.Black);
            }
            for (var i = 0; i < detailLines.Count; i++)
            {
                DrawTextAt(detailLines[i], rect.Left + 10, rect.Top + 42 + i * 10, 8.2f, false, new SKColor(80, 92, 112));
            }
            _y += height + 5;
        }

        public void SelectedShapeDiagram(Mooring2DDiagramReadModel diagram)
        {
            const float diagramHeight = 430;
            EnsureSpace(diagramHeight + 20);

            var selectedShape = diagram.SelectedShape;
            var shape = selectedShape.Shape;
            var nodes = shape.Nodes
                .OrderBy(v => v.Number)
                .Select(v => new PlotNode(v.XOffsetM, v.ZDepthM))
                .ToList();

            if (nodes.Count < 2)
            {
                Text("Выбранная расчётная форма X/Z не содержит достаточного количества узлов для схемы.", 10);
                return;
            }

            var x = Margin;
            var y = _y;
            var width = PageWidth - 2 * Margin;
            var plotHeight = 315f;
            var surfaceY = y + 58;
            var plotRect = new SKRect(x, y, x + width, y + diagramHeight);

            var minX = nodes.Min(v => v.X);
            var maxX = nodes.Max(v => v.X);
            var maxZ = Math.Max(0.0001, nodes.Max(v => v.Z));
            var drawingDepth = Math.Max(1, Math.Max(shape.DepthM, maxZ));
            var horizontalSpan = Math.Max(0.0001, maxX - minX);
            var scale = Math.Min((width - 110) / horizontalSpan, plotHeight / drawingDepth);
            var spanX = horizontalSpan * scale;
            var startX = x + width / 2f - (float)(spanX / 2.0);
            var bottomLineY = surfaceY + (float)(drawingDepth * scale);
            var waterRect = new SKRect(x, surfaceY, x + width, bottomLineY);

            using var plotPaint = Fill("#F7F9FC");
            using var waterPaint = Fill("#DCEBFF");
            using var bottomPaint = Fill("#E7DED3");
            using var borderPaint = Stroke("#D7DEE9", 1);
            using var linePaint = Stroke("#315B9A", 2.0f);
            using var thinPaint = Stroke("#A7C7EE", 1);
            using var buoyPaint = Fill("#F2A33A");
            using var anchorPaint = Fill("#5C4634");
            using var nodeBorderPaint = Stroke("#315B9A", 0.9f);
            using var warningPaint = Stroke("#D46B08", 1.0f);

            _canvas!.DrawRect(plotRect, plotPaint);
            _canvas.DrawRect(plotRect, borderPaint);
            _canvas.DrawRect(waterRect, waterPaint);
            _canvas.DrawRect(waterRect, borderPaint);
            _canvas.DrawRect(new SKRect(x, bottomLineY, x + width, bottomLineY + 28), bottomPaint);
            _canvas.DrawRect(new SKRect(x, bottomLineY, x + width, bottomLineY + 28), borderPaint);

            SKPoint Map(double mx, double mz) => new(
                (float)(startX + (mx - minX) * scale),
                (float)(surfaceY + Math.Clamp(mz, 0, drawingDepth) * scale));

            var points = nodes.Select(v => Map(v.X, v.Z)).ToList();
            _canvas.DrawLine(new SKPoint(x + 8, bottomLineY), new SKPoint(x + width - 8, bottomLineY), thinPaint);
            DrawPolyline(_canvas, points, linePaint);

            foreach (var marker in diagram.ElementMarkers)
            {
                DrawElementMarker(Map(marker.XOffsetM, marker.ZDepthM), marker);
            }

            var buoyPoint = points[0];
            var anchorPoint = points[^1];
            var userShapeStatus = shape.Converged ? "форма: ОК" : "форма: требует проверки";
            _canvas.DrawCircle(buoyPoint, 9, buoyPaint);
            _canvas.DrawCircle(buoyPoint, 9, shape.Converged ? nodeBorderPaint : warningPaint);
            _canvas.DrawRect(new SKRect(anchorPoint.X - 12, anchorPoint.Y - 6, anchorPoint.X + 12, anchorPoint.Y + 6), anchorPaint);

            DrawTextAt("поверхность воды", x + 14, surfaceY - 24, 10, true, SKColors.Black);
            DrawTextAt($"глубина {drawingDepth:0.##} м", x + 14, surfaceY + 18, 9, false, new SKColor(80, 92, 112));
            DrawTextAt("дно / грунт", x + 14, bottomLineY + 18, 10, true, new SKColor(92, 70, 52));
            DrawLegendLine(x + 14, y + 18, linePaint, "выбранная расчётная форма X/Z", new SKColor(49, 91, 154));
            DrawTextAt(userShapeStatus, x + 250, y + 23, 9.2f, false, shape.Converged ? new SKColor(80, 92, 112) : new SKColor(212, 107, 8));
            DrawTextAt($"снос X/Z {shape.HorizontalOffsetM:0.##} м", x + 390, y + 23, 9.2f, false, new SKColor(80, 92, 112));
            DrawTextAt($"источник: {selectedShape.SourceDescription}", x + 14, bottomLineY + 52, 9, false, new SKColor(80, 92, 112));
            DrawTextAt("масштаб X=Z; координаты взяты только из выбранных расчётных X/Z-узлов", x + 14, bottomLineY + 72, 8.5f, false, new SKColor(80, 92, 112));

            _y += diagramHeight + 10;
        }

        private void DrawElementMarker(SKPoint point, Mooring2DElementMarker marker)
        {
            using var connectorFill = Fill("#FFFFFF");
            using var payloadFill = Fill("#F2A33A");
            using var markerStroke = Stroke("#315B9A", 0.9f);

            switch (marker.MarkerKind)
            {
                case Mooring2DElementMarkerKind.LineBoundary:
                    _canvas!.DrawLine(new SKPoint(point.X - 4, point.Y), new SKPoint(point.X + 4, point.Y), markerStroke);
                    break;

                case Mooring2DElementMarkerKind.Payload:
                    _canvas!.DrawCircle(point, 3.4f, payloadFill);
                    _canvas.DrawCircle(point, 3.4f, markerStroke);
                    break;

                case Mooring2DElementMarkerKind.Connector:
                    _canvas!.DrawRect(new SKRect(point.X - 3.0f, point.Y - 3.0f, point.X + 3.0f, point.Y + 3.0f), connectorFill);
                    _canvas.DrawRect(new SKRect(point.X - 3.0f, point.Y - 3.0f, point.X + 3.0f, point.Y + 3.0f), markerStroke);
                    break;

                default:
                    _canvas!.DrawCircle(point, 3.0f, connectorFill);
                    _canvas.DrawCircle(point, 3.0f, markerStroke);
                    break;
            }
        }

        public void Space(float value)
        {
            _y += value;
        }

        private void DrawTableRow(
            IReadOnlyList<string> values,
            IReadOnlyList<float> widths,
            bool isHeader,
            float rowHeight,
            float fontSize,
            int headerMaxLength,
            int valueMaxLength)
        {
            EnsureSpace(rowHeight + 4);

            var x = Margin;
            for (var i = 0; i < values.Count; i++)
            {
                var rect = new SKRect(x, _y, x + widths[i], _y + rowHeight);
                using var fill = new SKPaint
                {
                    Color = isHeader ? new SKColor(233, 239, 250) : SKColors.White,
                    IsAntialias = true
                };
                using var border = new SKPaint
                {
                    Color = new SKColor(215, 222, 233),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 0.7f,
                    IsAntialias = true
                };
                _canvas!.DrawRect(rect, fill);
                _canvas.DrawRect(rect, border);

                var text = Shorten(values[i], isHeader ? headerMaxLength : valueMaxLength);
                using var paint = CreatePaint(fontSize, isHeader);
                _canvas.DrawText(text, rect.Left + 3, rect.Top + rowHeight * 0.64f, paint);
                x += widths[i];
            }

            _y += rowHeight;
        }

        private void DrawText(string text, float size, bool bold)
        {
            EnsureSpace(size + LineGap + 3);
            using var paint = CreatePaint(size, bold);
            _canvas!.DrawText(text ?? string.Empty, Margin, _y + size, paint);
            _y += size + LineGap;
        }

        private void DrawTextAt(string text, float x, float y, float size, bool bold, SKColor color)
        {
            using var paint = CreatePaint(size, bold, color);
            _canvas!.DrawText(text ?? string.Empty, x, y, paint);
        }

        private SKPaint CreatePaint(float size, bool bold)
        {
            return CreatePaint(size, bold, SKColors.Black);
        }

        private SKPaint CreatePaint(float size, bool bold, SKColor color)
        {
            return new SKPaint
            {
                Color = color,
                IsAntialias = true,
                TextSize = size,
                Typeface = bold ? _boldTypeface : _regularTypeface
            };
        }

        private void EnsureSpace(float required)
        {
            if (_y + required < PageHeight - Margin)
            {
                return;
            }

            EndPage();
            BeginPage();
        }

        private void DrawFooter()
        {
            using var paint = CreatePaint(8, false);
            _canvas!.DrawText($"BuoyCalc Windows {AppInfo.Version} · стр. {_pageNumber}", Margin, PageHeight - 18, paint);
        }

        private IEnumerable<string> Wrap(string text, float size, float maxWidth)
        {
            text ??= string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                yield return string.Empty;
                yield break;
            }

            using var paint = CreatePaint(size, false);
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
                if (paint.MeasureText(candidate) <= maxWidth)
                {
                    line = candidate;
                }
                else
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        yield return line;
                    }
                    line = word;
                }
            }

            if (!string.IsNullOrEmpty(line))
            {
                yield return line;
            }
        }

        private static void DrawPolyline(SKCanvas canvas, IReadOnlyList<SKPoint> points, SKPaint paint)
        {
            for (var i = 1; i < points.Count; i++)
            {
                canvas.DrawLine(points[i - 1], points[i], paint);
            }
        }

        private void DrawLegendLine(float x, float y, SKPaint linePaint, string text, SKColor textColor)
        {
            _canvas!.DrawLine(new SKPoint(x, y), new SKPoint(x + 28, y), linePaint);
            DrawTextAt(text, x + 36, y + 4, 9, false, textColor);
        }

        private static SKPaint Fill(string color)
        {
            return new SKPaint { Color = SKColor.Parse(color), IsAntialias = true, Style = SKPaintStyle.Fill };
        }

        private static SKPaint Stroke(string color, float width)
        {
            return new SKPaint { Color = SKColor.Parse(color), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = width };
        }

        private static string Shorten(string value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
        }

        private sealed record PlotNode(double X, double Z);
    }
}

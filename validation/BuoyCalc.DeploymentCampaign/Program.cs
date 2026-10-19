using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal sealed record Scenario(string Id, string Group, double DepthM, double LengthM,
    string Soil, double AnchorMassKg, EnvironmentInput? Environment, BuoyInput Buoy,
    IReadOnlyList<AssemblyItemInput> Assembly, AnchorInput? Anchor, string Source, string? Blocked = null);
internal sealed record Check(string Category, string Name, string Status, string Detail, double? Deviation = null, double? Tolerance = null);
internal sealed record Row(string Id, string Group, double DepthM, double LengthM, string Soil, double AnchorMassKg,
    string Execution, string Invariants, string LimitedIndependentChecks, string FullNumericalValidation,
    string FieldSuitability, string Presentation, string Solver, int Iterations, bool ExactFixedPoint,
    double? X, double? Z, double? CurrentN, double? WaveN, double? F1N, double? F2HN,
    double? F2NormalN, bool F3, bool F4, string? Verdict, string Diagnostic, string Notes);
internal static class Program
{
    private const string CoreSha = "0db1e7cc17e309fc1402fe58b265d2e95c2d4d39";
    private static readonly double[] Depths = {20, 50, 100, 350, 380, 500};
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly BuoyInput ReferenceBuoy = new("Series A reference buoy", 1, 100, 0.1, 0.8);
    private static readonly RopePreset ReferenceLine = new("campaign:rope-14", "Series A 14 mm line", "Synthetic reference", 14, 70, 0.15, 1,
        "Existing Series A fixture; no manufacturer provenance.");
    private static readonly AnchorInput ReferenceAnchor = new("Series A 3000 kg block", "Concrete block", "Concrete", 3000, 1.2, 1);
    private static string Output = "";
    private static string CampaignSha = "";

    public static int Main(string[] args)
    {
        Output = Arg(args, "--output");
        CampaignSha = Arg(args, "--source-sha");
        if (CampaignSha.Length != 40) throw new ArgumentException("Exact source SHA required.");
        Directory.CreateDirectory(Output);
        Directory.CreateDirectory(Path.Combine(Output, "scenarios"));
        var scenarios = BuildScenarios();
        Write("inputs.json", new { CoreSha, CampaignSha, CommandLine = args, SegmentationM = 0.20, Budget = 64, SafetyFactor = 3.0, Scenarios = scenarios });
        var rows = new List<Row>();
        foreach (var scenario in scenarios)
        {
            rows.Add(Run(scenario));
            File.WriteAllText(Path.Combine(Output, "matrix.json"), JsonSerializer.Serialize(rows, Json), Encoding.UTF8);
            Console.WriteLine("CAMPAIGN_ROW|" + JsonSerializer.Serialize(rows[^1], new JsonSerializerOptions()));
        }
        WriteCsv(rows);
        WriteReport(rows);
        var failed = rows.Count(x => x.Execution == "FAIL" || x.Invariants == "FAIL" || x.LimitedIndependentChecks == "FAIL" || x.Presentation == "FAIL");
        Console.WriteLine($"DEPLOYMENT_COMPLETE total={rows.Count} executed={rows.Count(x => x.Execution == "PASS")} blocked={rows.Count(x => x.Execution == "BLOCKED")} failing_rows={failed}");
        // Preserve evidence, but do not make an invariant failure look green.
        return failed == 0 ? 0 : 2;
    }
    private static string Arg(string[] args, string key)
    {
        var i = Array.IndexOf(args, key);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : throw new ArgumentException(key);
    }
    private static void Write(string name, object data) =>
        File.WriteAllText(Path.Combine(Output, name), JsonSerializer.Serialize(data, Json), Encoding.UTF8);
    private static AssemblyItemInput Line(RopePreset rope, double length) => new(AssemblyItemKind.Line, rope.Name, true, rope, null, length, 1, 0, 0, 0, 0);
    private static AssemblyItemInput Connector(ConnectorPreset c) => new(AssemblyItemKind.Connector, c.Name, true, null, c, 0, 1, 0, 0, 0, 0);
    private static AssemblyItemInput Payload(PayloadLibraryItem p) => new(AssemblyItemKind.Payload, p.Name, true, null, null, 0, 1, p.WeightAirKg, p.VolumeM3, p.ProjectedAreaM2, p.DragCoefficient);
    private static AnchorInput Anchor(AnchorPreset a) => new(a.Name, a.Type, a.Material, a.WeightAirKg, a.VolumeM3, a.BaseHoldingCoefficient);
    private static IReadOnlyList<CurrentProfilePointInput> Profile(double depth, double u, string kind) => kind switch
    {
        "gradient" => new[] { new CurrentProfilePointInput(0, u, 0, 0, 1025), new(depth / 2, u / 2, 0, 0, 1025), new(depth, u / 6, 0, 0, 1025) },
        "reverse" => new[] { new CurrentProfilePointInput(0, u, 0, 0, 1025), new(depth / 2, 0, 0, 0, 1025), new(depth, -u, 0, 0, 1025) },
        "veer" => new[] { new CurrentProfilePointInput(0, u, 0, 0, 1025), new(depth / 2, u / 2, u / 2, 0, 1025), new(depth, 0, u / 3, 0, 1025) },
        _ => new[] { new CurrentProfilePointInput(0, u, 0, 0, 1025), new(depth, u, 0, 0, 1025) }
    };
    private static Scenario Make(string id, string group, double depth = 380, double ratio = 1.05, double u = 0.45,
        string soil = "sand", AnchorInput? anchor = null, RopePreset? rope = null, BuoyInput? buoy = null,
        double h = 0, double t = 0, string profile = "constant", int sequence = 0)
    {
        anchor ??= ReferenceAnchor; rope ??= ReferenceLine; buoy ??= ReferenceBuoy;
        var seabed = SeabedCatalog.Presets.Single(x => x.Id == soil);
        var length = depth * ratio;
        var assembly = new List<AssemblyItemInput>();
        if (sequence == 0) assembly.Add(Line(rope, length));
        else
        {
            assembly.Add(Connector(ConnectorCatalog.Presets[0]));
            assembly.Add(Line(rope, length / 2));
            assembly.Add(Connector(ConnectorCatalog.Presets[2]));
            assembly.Add(Payload(PayloadLibraryStorage.BuiltInPayloads[0]));
            if (sequence == 2) assembly.Add(Payload(PayloadLibraryStorage.BuiltInPayloads[1]));
            assembly.Add(Line(rope, length - length / 2));
            assembly.Add(Connector(ConnectorCatalog.Presets[1]));
        }
        return new(id, group, depth, length, soil, anchor.WeightAirKg,
            new(1025, depth, 0, h, t, seabed, true, Profile(depth, u, profile)),
            buoy, assembly, anchor,
            "Core " + CoreSha + "; Models/Catalogs.cs; Services/BuoyLibraryStorage.cs; Services/PayloadLibraryStorage.cs; Series A reference hardware. " +
            "Catalog values are educational. Depth/length/current/profile/wave combinations are explicit synthetic test inputs, not climate or equipment specifications.");
    }
    private static IReadOnlyList<Scenario> BuildScenarios()
    {
        var s = new List<Scenario>();
        var masses = new double[] {500,1000,1200,1500,1800,2000,3000};
        // Preserve 210 requested slots, including unsupported combinations.
        foreach (var d in Depths)
        foreach (var soil in new[] {"sand","mud","dense_clay","gravel","rock"})
        foreach (var mass in masses)
        {
            var id = $"B-{d}-{soil}-{mass}";
            AnchorInput? a = mass == 3000 ? ReferenceAnchor :
                AnchorCatalog.Presets.Where(x => x.Type == "Deadweight" && x.WeightAirKg == mass).Select(Anchor).SingleOrDefault();
            var reasons = new List<string>();
            if (!SeabedCatalog.Presets.Any(x => x.Id == soil)) reasons.Add("No catalog soil model; do not substitute mud/sand.");
            if (a is null) reasons.Add("No existing concrete fixture with this mass AND documented volume; volume not inferred.");
            if (reasons.Count != 0)
                s.Add(new(id,"base210",d,d*1.05,soil,mass,null,ReferenceBuoy,new[]{Line(ReferenceLine,d*1.05)},a,
                    "User requested matrix; catalogs and Series A inspected at " + CoreSha, string.Join(" ", reasons)));
            else s.Add(Make(id,"base210",d,soil:soil,anchor:a));
        }
        foreach (var d in Depths)
        foreach (var ratio in new[] {0.99,1.0,1.01,1.05,1.10,1.20})
        foreach (var u in new[] {0.0,0.2,0.45,0.6})
            s.Add(Make($"G-{d}-{ratio}-{u}","geometry-current",d,ratio,u));
        foreach (var d in new[] {20.0,380.0,500.0})
        foreach (var kind in new[] {"gradient","reverse","veer"})
            s.Add(Make($"P-{d}-{kind}","profiles",d,profile:kind));
        foreach (var a in AnchorCatalog.Presets)
            s.Add(Make("A-"+a.Id,"anchor-types",anchor:Anchor(a)));
        foreach (var rope in RopeCatalog.Presets)
        foreach (var d in new[] {20.0,380.0,500.0})
            s.Add(Make($"R-{rope.Id}-{d}","catalog-lines",d,rope:rope));
        var signed = new RopePreset("reg:buoyant-line","Regression buoyant line","Synthetic buoyant",20,100,-0.05,1.2,
            "Exact existing HistoricalGoldenImpactRegression fixture; no product claim.");
        foreach (var d in new[] {20.0,380.0,500.0})
            s.Add(Make($"S-{d}","signed-water-weight",d,rope:signed) with { Source = "HistoricalGoldenImpactRegression.BuildHistoricalScenarios buoyantLine; "+CoreSha+"; test depth/length varied." });
        foreach (var b in BuoyLibraryStorage.BuiltInBuoys)
            s.Add(Make("U-"+b.Id.Replace(':','-'),"catalog-buoys",buoy:new(b.Name,b.VolumeM3,b.WeightKg,b.ProjectedAreaM2,b.DragCoefficient)));
        foreach (var d in new[] {20.0,380.0,500.0})
        foreach (var seq in new[] {1,2})
            s.Add(Make($"Q-{d}-{seq}","sequences",d,sequence:seq));
        foreach (var d in new[] {20.0,380.0,500.0})
        foreach (var h in new[] {0.5,1.0,1.5})
        foreach (var t in new[] {5.0,8.0})
            s.Add(Make($"W-{d}-{h}-{t}","waves",d,h:h,t:t));
        s.Add(Make("BL820-380","user-deployment") with {Blocked =
            "No verified BL-820 wind/current/wave/mooring loads or supported vessel-force input; no equipment passports, surveyed seabed/holding model, local extremes or verified sequence. Not a completed field calculation."});
        if (s.Select(x => x.Id).Distinct().Count() != s.Count) throw new InvalidOperationException("Duplicate ID");
        return s;
    }
    private static Row Run(Scenario s)
    {
        if (s.Blocked is not null)
        {
            var blocked = new Row(s.Id,s.Group,s.DepthM,s.LengthM,s.Soil,s.AnchorMassKg,"BLOCKED","BLOCKED","BLOCKED","BLOCKED","BLOCKED","PENDING","NOT_RUN",0,false,null,null,null,null,null,null,null,false,false,null,"",s.Blocked);
            Write("scenarios/"+s.Id+".json",new{CoreSha,CampaignSha,Input=s,Summary=blocked});
            return blocked;
        }
        var checks = new List<Check>();
        void Test(string category,string name,bool passed,string detail,double? deviation=null,double? tolerance=null) =>
            checks.Add(new(category,name,passed?"PASS":"FAIL",detail,deviation,tolerance));
        try
        {
            var run = ApplicationCalculationRunner.Run(s.Environment!,s.Buoy,s.Assembly,s.Anchor!,3);
            var r = run.Result; var snap = run.Snapshot;
            var c = snap.SignedCandidate ?? throw new InvalidOperationException("Missing candidate");
            var accepted = c.Status == MooringSignedCandidateStatus.Accepted;
            var rejected = c.Status == MooringSignedCandidateStatus.RejectedPhysical;
            var end = snap.SelectedShape?.Shape.AnchorPoint;
            var f1=snap.SelectedDesignTensionDemand; var f2=snap.SelectedAnchorReaction;
            var f3=snap.SelectedLocalStructuralCapacity; var f4=snap.SelectedEngineeringAssessment;
            Test("Invariant","Budget",MooringSignedCandidateResult.ProductionFeedbackBudget==64 && c.FeedbackIterations>=0 && c.FeedbackIterations<=64,"Production budget and actual iterations.");
            var max = r.SegmentRows.Select(x=>x.SegmentLengthM).DefaultIfEmpty().Max();
            Test("Invariant","Segmentation",double.IsFinite(max)&&max<=0.20+1e-12,"0.20 m maximum, 1e-12 m rounding allowance.",max-0.20,1e-12);
            var lengthError=r.SegmentRows.Sum(x=>x.SegmentLengthM)-s.LengthM;
            Test("Invariant","LengthClosure",double.IsFinite(lengthError)&&Math.Abs(lengthError)<=1e-6,"Segment sum minus input length; summation allowance 1 micrometre.",lengthError,1e-6);
            var weightExpected=s.Assembly.Where(x=>x.Kind==AssemblyItemKind.Line).Sum(x=>x.LengthM*x.RopePreset!.WeightWaterKgM);
            var weightError=r.SegmentRows.Sum(x=>x.WeightWaterKg)-weightExpected;
            Test("Invariant","SignedWeight",double.IsFinite(weightError)&&Math.Abs(weightError)<=1e-7,"Segment water weight minus signed input integral, kg.",weightError,1e-7);
            Test("Invariant","FiniteForces",double.IsFinite(r.CurrentForceN)&&double.IsFinite(r.WaveForceN),"Production scalar current/wave outputs finite.");
            if (accepted)
                Test("Invariant","AcceptedAuthority",c.ExactFixedPointReached &&
                    snap.SelectedShape?.Source==MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString() &&
                    f1 is not null && f2 is not null && f3 is not null && f4 is not null,
                    "Exact fixed point and retained selected F1-F4; no epsilon.");
            else
                Test("Invariant","NoUnacceptedF1F4",f1 is null&&f2 is null&&f3 is null&&f4 is null,"Non-Accepted state has no selected F1-F4.");
            if (rejected)
            {
                var p=snap.PhysicalDisposition;
                Test("Invariant","PhysicalDisposition",p?.Verdict=="Не подходит"&&p.HasHardFailure&&p.BlocksEngineeringGeometry&&p.DiagnosticCode==c.DiagnosticCode&&p.DiagnosticText==c.DiagnosticText,"Exact candidate diagnostic code/text and terminal verdict.");
                Test("Invariant","RejectedAuthorityAbsent",snap.SelectedShape is null&&snap.ShadowSelectedCore is null&&snap.SelectedDesignEnvelope is null&&snap.SelectedLocalElementDemand is null&&f1 is null&&f2 is null&&f3 is null&&f4 is null,"No selected geometry or selected F1-F4.");
                var text=UserReportBuilder.Build(s.Environment!,snap);
                Test("Presentation","RejectedReport",text.Contains("Вердикт: Не подходит",StringComparison.Ordinal)&&text.Contains(c.DiagnosticCode,StringComparison.Ordinal)&&text.Contains(c.DiagnosticText,StringComparison.Ordinal),"Generated user report text only; UI/2D/PDF rendering still PENDING.");
            }
            else Test("Invariant","NoFalsePhysicalDisposition",snap.PhysicalDisposition is null,"Non-physical states do not get physical rejection.");
            if(s.LengthM<s.DepthM)
                Test("Independent","ShortLine",rejected&&c.Boundary?.Classification==MooringSurfaceBoundaryInfoClassification.LineShorterThanDepth,"Inextensible surface line L<D impossible.");
            if(s.LengthM==s.DepthM && s.Environment!.EffectiveCurrentProfile.Any(x=>x.HorizontalSpeedMS>0))
                Test("Independent","TautHorizontal",rejected&&c.Boundary?.Classification==MooringSurfaceBoundaryInfoClassification.TautNonZeroHorizontalLoadNoFiniteRoot,"L=D with nonzero horizontal load has no finite inclined root.");
            if(s.Environment!.EffectiveCurrentProfile.All(x=>x.SpeedMS==0))
                Test("Independent","ZeroCurrent",r.CurrentForceN==0,"Exactly zero imposed current must give zero current drag.",r.CurrentForceN,0);
            if(s.Environment.WaveHeightM==0)
                Test("Independent","ZeroWave",r.WaveForceN==0,"Disabled wave must give zero wave force.",r.WaveForceN,0);
            if(end is not null)
            {
                var chord=Math.Sqrt(end.XOffsetM*end.XOffsetM+end.ZDepthM*end.ZDepthM);
                Test("Independent","SelectedChord",double.IsFinite(chord)&&chord<=s.LengthM+1e-6,"Selected endpoint chord cannot exceed inextensible line length; includes fallback, which is not Accepted authority.",chord-s.LengthM,1e-6);
            }
            string Status(string category) => checks.Any(x=>x.Category==category&&x.Status=="FAIL")?"FAIL":checks.Any(x=>x.Category==category)?"PASS":"PENDING";
            var notes = "Full X/Z/tension reference comparison PENDING; anchor-soil holding BLOCKED (educational coefficients); field use BLOCKED; UI/2D/PDF PENDING.";
            if(!accepted&&!rejected) notes += " Signed authority unavailable: "+c.Status+".";
            var row = new Row(s.Id,s.Group,s.DepthM,s.LengthM,s.Soil,s.AnchorMassKg,"PASS",Status("Invariant"),Status("Independent"),"PENDING","BLOCKED",
                Status("Presentation")=="FAIL"?"FAIL":"PENDING",c.Status.ToString(),c.FeedbackIterations,c.ExactFixedPointReached,
                end?.XOffsetM,end?.ZDepthM,r.CurrentForceN,r.WaveForceN,f1?.DemandN,f2?.HorizontalDemandN,f2?.SignedNormalReactionN,
                f3 is not null,f4 is not null,snap.PhysicalDisposition?.Verdict??f4?.Verdict,c.DiagnosticCode,notes);
            Write("scenarios/"+s.Id+".json",new
            {
                CoreSha,CampaignSha,Input=s,SafetyFactor=3,Summary=row,Checks=checks,
                Candidate=new{Status=c.Status.ToString(),Boundary=c.Boundary?.Classification.ToString(),c.DiagnosticCode,c.DiagnosticText,c.FeedbackIterations,c.ExactFixedPointReached},
                LegacyCalculation=r,SelectedShape=snap.SelectedShape,PhysicalDisposition=snap.PhysicalDisposition,
                F1=f1,F2=f2,F3=f3,F4=f4,
                AuthorityNote="LegacyCalculation scalars are compatibility outputs; selected forces and geometry retain their explicitly named authority."
            });
            return row;
        }
        catch(Exception ex)
        {
            var row=new Row(s.Id,s.Group,s.DepthM,s.LengthM,s.Soil,s.AnchorMassKg,"FAIL","PENDING","PENDING","PENDING","BLOCKED","PENDING","EXCEPTION",0,false,null,null,null,null,null,null,null,false,false,null,"CampaignException",ex.ToString());
            Write("scenarios/"+s.Id+".json",new{CoreSha,CampaignSha,Input=s,Summary=row,Checks=checks,Exception=ex.ToString()});
            return row;
        }
    }
    private static void WriteCsv(IReadOnlyList<Row> rows)
    {
        var properties=typeof(Row).GetProperties();
        string Csv(object? v) => "\"" + Convert.ToString(v,CultureInfo.InvariantCulture)?.Replace("\"","\"\"") + "\"";
        var lines=new List<string>{string.Join(",",properties.Select(p=>Csv(p.Name)))};
        lines.AddRange(rows.Select(r=>string.Join(",",properties.Select(p=>Csv(p.GetValue(r))))));
        File.WriteAllLines(Path.Combine(Output,"matrix.csv"),lines,Encoding.UTF8);
    }
    private static void WriteReport(IReadOnlyList<Row> rows)
    {
        var text=new StringBuilder("# Deployment campaign: actual execution evidence\n\n");
        text.AppendLine("Core SHA: "+CoreSha+"; campaign SHA: "+CampaignSha+".");
        text.AppendLine("\nPASS means only the named criterion passed. Accepted is not field suitability. All field suitability is BLOCKED. Full numerical reference validation and UI/2D/PDF are PENDING.");
        text.AppendLine("\n## Actual rollup\n");
        foreach(var g in rows.GroupBy(x=>x.Group))
            text.AppendLine($"- {g.Key}: total={g.Count()}, executed={g.Count(x=>x.Execution=="PASS")}, blocked={g.Count(x=>x.Execution=="BLOCKED")}, exceptions={g.Count(x=>x.Execution=="FAIL")}, invariant failures={g.Count(x=>x.Invariants=="FAIL")}, limited independent failures={g.Count(x=>x.LimitedIndependentChecks=="FAIL")}.");
        foreach(var g in rows.Where(x=>x.Execution=="PASS").GroupBy(x=>x.Solver))text.AppendLine($"- Solver {g.Key}: {g.Count()}.");
        text.AppendLine("\n## Matrix\n\n| ID | D | L | Execution | Invariants | Limited independent | Signed status | Iterations | F1 N | F2 H N | Verdict |\n|---|---:|---:|---|---|---|---|---:|---:|---:|---|");
        foreach(var r in rows) text.AppendLine($"| {r.Id} | {r.DepthM} | {r.LengthM} | {r.Execution} | {r.Invariants} | {r.LimitedIndependentChecks} | {r.Solver} | {r.Iterations} | {r.F1N} | {r.F2HN} | {r.Verdict} |");
        text.AppendLine("\n## Limitations and next step\n");
        text.AppendLine("- 210 base slots are not a full Cartesian environmental/equipment sweep. Soil mud means catalog silt only, not a validated clay substitute. Rock is an educational catalog, not a rock-contact model.");
        text.AppendLine("- Concrete 1200/1500/1800/2000 kg lacks explicit volume fixture; dense clay and gravel lack catalog models. These slots were not run. 3000 kg uses the existing Series A fixture, not a manufacturer specification.");
        text.AppendLine("- Wave heights 0.5/1/1.5 m and periods 5/8 s are synthetic test inputs. The production quasi-static wave model is not a validated dynamic sea-state simulation.");
        text.AppendLine("- BL-820 at 380 m: verified vessel displacement/draft, windage and underwater areas, coefficients, heading, mooring attachment and dynamic loads, wind/wave/current extremes, surveyed soil, hardware passports and actual sequence are missing. No passenger mass or vessel load was silently inserted.");
        text.AppendLine("- Series A is rerun unchanged; PhysicalRejectionNotPromotedToF4HardFailure is an obsolete criterion after #604. Preserve its raw findings; use this campaign and #601 for the current rejection contract.");
        text.AppendLine("- Feedback diagnostics are a production-stage mirror, not an independent solver. Budget exhaustion must not be converted into epsilon acceptance.");
        text.AppendLine("- Investigate any FAIL using scenario JSON; propose a separate approved physics package if needed. Obtain missing input data and independent X/Z/tension reference evidence before field approval.");
        File.WriteAllText(Path.Combine(Output,"report.md"),text.ToString(),Encoding.UTF8);
    }
}

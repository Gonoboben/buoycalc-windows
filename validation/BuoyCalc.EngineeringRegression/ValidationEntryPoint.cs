internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        try
        {
            ShapeLineLengthSourceRegression.Validate();
            ForceShapeConsistencyRegression.Validate();
            SignedNodeEquilibriumRegression.Validate();
            FinalIterationDiscreteStateRegression.Validate();
            FinalIterationSignedNodeEquilibriumRegression.Validate();
            IterativeFeedbackCouplingRegression.Validate();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Engineering validation regression failure:");
            Console.Error.WriteLine(ex);
            return 1;
        }

        return Program.Main(args);
    }
}

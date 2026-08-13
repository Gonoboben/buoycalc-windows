internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        try
        {
            ShapeLineLengthSourceRegression.Validate();
            ForceShapeConsistencyRegression.Validate();
            SignedOrientationRegression.Validate();
            BoundaryLoadOwnershipRegression.Validate();
            ConstantLoadAnalyticalReferenceRegression.Validate();
            PiecewisePointLoadAnalyticalReferenceRegression.Validate();
            BerteauxVectorOverlapRegression.Validate();
            SignedNodeEquilibriumRegression.Validate();
            FinalIterationDiscreteStateRegression.Validate();
            FinalIterationSignedNodeEquilibriumRegression.Validate();
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

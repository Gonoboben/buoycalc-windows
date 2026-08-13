internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        try
        {
            ShapeLineLengthSourceRegression.Validate();
            ForceShapeConsistencyRegression.Validate();
            SignedNodeEquilibriumRegression.Validate();
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

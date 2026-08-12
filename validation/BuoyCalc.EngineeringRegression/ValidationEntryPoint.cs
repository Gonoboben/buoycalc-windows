internal static class ValidationEntryPoint
{
    public static int Main(string[] args)
    {
        try
        {
            ForceShapeConsistencyRegression.Validate();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Force-shape consistency regression failure:");
            Console.Error.WriteLine(ex);
            return 1;
        }

        return Program.Main(args);
    }
}

namespace Tzkt.Api
{
    public class OrParameter(params (string, List<int>?)[] colsAndVals)
    {
        public (string, List<int>?)[] ColsAndVals { get; } = colsAndVals;
    }
}

namespace ExtensibleBatchRunner
{
    internal interface IInnerPathFinder
    {
        string[] GetAllInnerPaths(string[] paths);
    }
}
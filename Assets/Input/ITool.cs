namespace CityBuilder.Roads
{
    /// <summary>
    /// Simple lifecycle hooks for tools that can be toggled by ToolManager.
    /// </summary>
    public interface ITool
    {
        string ToolName { get; }
        void OnToolActivated();
        void OnToolDeactivated();
    }
}

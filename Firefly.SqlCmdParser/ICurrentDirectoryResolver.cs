namespace Firefly.SqlCmdParser
{
    /// <summary>
    /// Interface to provide the current directory
    /// If creating a PowerShell client, PowerShell has a different idea about what the current directory is to the .NET runtime.
    /// </summary>
    public interface ICurrentDirectoryResolver
    {
        /// <summary>
        /// Gets the current directory.
        /// </summary>
        /// <returns>Current directory</returns>
        string GetCurrentDirectory();
    }
}
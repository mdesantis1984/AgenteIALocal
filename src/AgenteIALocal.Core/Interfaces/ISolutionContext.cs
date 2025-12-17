using System.Collections.Generic;

namespace AgenteIALocal.Core.Interfaces
{
    /// <summary>
    /// Read-only access to solution and projects metadata.
    /// </summary>
    public interface ISolutionContext
    {
        /// <summary>
        /// Display name of the solution.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Full path to the solution file (.sln).
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Read-only list of projects in the solution.
        /// </summary>
        IReadOnlyList<IProjectInfo> Projects { get; }
    }

    public interface IProjectInfo
    {
        string Name { get; }
        string Path { get; }
        string Language { get; }
    }
}

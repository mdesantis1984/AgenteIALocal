using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgenteIALocal.Core.Interfaces;

namespace AgenteIALocal.Infrastructure.Workspace
{
    public class VisualStudioSolutionContext : ISolutionContext
    {
        public VisualStudioSolutionContext()
        {
        }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public IReadOnlyList<IProjectInfo> Projects { get; private set; } = Array.Empty<IProjectInfo>();

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Keep existing fallback behavior
            try
            {
                var assemblyDir = AppContext.BaseDirectory;
                var solutionFile = FindSolutionFileUpwards(assemblyDir);
                if (string.IsNullOrEmpty(solutionFile))
                {
                    Name = null;
                    Path = null;
                    Projects = Array.Empty<IProjectInfo>();
                    return Task.CompletedTask;
                }

                Path = solutionFile;
                Name = System.IO.Path.GetFileNameWithoutExtension(solutionFile);

                var projects = ParseProjectsFromSolution(solutionFile);
                Projects = projects;
            }
            catch
            {
                Name = null;
                Path = null;
                Projects = Array.Empty<IProjectInfo>();
            }

            return Task.CompletedTask;
        }

        public static ISolutionContext CreateBestEffort(object vsServices = null)
        {
            // If VS SDK types are available and a solution service is provided, prefer the SDK adapter.
            try
            {
                if (vsServices != null && VsSdkAvailability.IsVsSdkPresent())
                {
                    var vsSolution = vsServices;
                    var adapterType = Type.GetType("AgenteIALocal.Infrastructure.Workspace.VsSdkSolutionContextAdapter, AgenteIALocal.Infrastructure");
                    if (adapterType != null)
                    {
                        var adapter = Activator.CreateInstance(adapterType, new[] { vsSolution }) as ISolutionContext;
                        return adapter ?? new VisualStudioSolutionContext();
                    }
                }
            }
            catch
            {
                // ignore and fall back
            }

            return new VisualStudioSolutionContext();
        }

        private static string FindSolutionFileUpwards(string startDirectory)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                var slnFiles = dir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                if (slnFiles.Length > 0)
                {
                    // Prefer the first .sln found
                    return slnFiles[0].FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }

        private static IReadOnlyList<IProjectInfo> ParseProjectsFromSolution(string solutionFile)
        {
            var list = new List<IProjectInfo>();
            var lines = File.ReadAllLines(solutionFile);

            foreach (var line in lines)
            {
                if (!line.StartsWith("Project(", StringComparison.Ordinal))
                    continue;

                var parts = line.Split('=');
                if (parts.Length < 2) continue;

                var rhs = parts[1].Trim();
                var tokens = SplitCsvPreservingQuotes(rhs);
                if (tokens.Length >= 2)
                {
                    var projectName = TrimQuotes(tokens[0]);
                    var projectPath = TrimQuotes(tokens[1]);

                    var solutionDir = System.IO.Path.GetDirectoryName(solutionFile);
                    var absolutePath = projectPath;
                    if (!System.IO.Path.IsPathRooted(projectPath) && !string.IsNullOrEmpty(solutionDir))
                    {
                        absolutePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(solutionDir, projectPath));
                    }

                    var language = InferLanguageFromProjectFile(absolutePath);
                    list.Add(new ProjectInfoStub(projectName, absolutePath, language));
                }
            }

            return list;
        }

        private static string[] SplitCsvPreservingQuotes(string input)
        {
            var results = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            foreach (var ch in input)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    results.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
                results.Add(current.ToString().Trim());

            return results.ToArray();
        }

        private static string TrimQuotes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Trim().Trim('"').Trim();
        }

        private static string InferLanguageFromProjectFile(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) return null;
            var ext = System.IO.Path.GetExtension(projectPath)?.ToLowerInvariant();
            if (ext == ".csproj") return "C#";
            if (ext == ".vbproj") return "VB";
            return null;
        }

        private class ProjectInfoStub : IProjectInfo
        {
            public ProjectInfoStub(string name, string path, string language)
            {
                Name = name;
                Path = path;
                Language = language;
            }

            public string Name { get; }
            public string Path { get; }
            public string Language { get; }
        }
    }
}

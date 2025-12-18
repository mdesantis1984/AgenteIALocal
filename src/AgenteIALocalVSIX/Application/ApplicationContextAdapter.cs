using System;

namespace AgenteIALocalVSIX.Application
{
    // Thin adapter that exposes read-only workspace info to the VSIX UI.
    internal class ApplicationContextAdapter
    {
        private readonly object workspaceContext;

        public ApplicationContextAdapter(object workspaceContext)
        {
            this.workspaceContext = workspaceContext;
        }

        public string GetSolutionName()
        {
            try
            {
                var solProp = workspaceContext?.GetType().GetProperty("Name");
                if (solProp != null)
                {
                    var name = solProp.GetValue(workspaceContext) as string;
                    return name ?? "(no solution)";
                }

                // Try Solution property
                var solutionProp = workspaceContext?.GetType().GetProperty("Solution");
                if (solutionProp != null)
                {
                    var sol = solutionProp.GetValue(workspaceContext);
                    var nameProp = sol?.GetType().GetProperty("Name");
                    var name = nameProp?.GetValue(sol) as string;
                    return name ?? "(no solution)";
                }
            }
            catch { }

            return "(no solution)";
        }

        public int GetProjectCount()
        {
            try
            {
                var solutionProp = workspaceContext?.GetType().GetProperty("Solution");
                var sol = solutionProp?.GetValue(workspaceContext);
                if (sol != null)
                {
                    var projectsProp = sol.GetType().GetProperty("Projects");
                    var projects = projectsProp?.GetValue(sol) as System.Collections.IEnumerable;
                    if (projects != null)
                    {
                        int count = 0;
                        var enumerator = projects.GetEnumerator();
                        while (enumerator.MoveNext()) count++;
                        return count;
                    }

                    // If Projects is null try to see if there's a Count property on solution
                    var countProp = sol.GetType().GetProperty("Count");
                    if (countProp != null)
                    {
                        var val = countProp.GetValue(sol);
                        if (val is int) return (int)val;
                    }
                }
            }
            catch { }

            return 0;
        }
    }
}

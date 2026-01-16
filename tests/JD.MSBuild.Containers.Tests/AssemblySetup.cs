using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace JD.MSBuild.Containers.Tests;

/// <summary>
/// Assembly-level initialization for test execution.
/// </summary>
public static class AssemblySetup
{
    /// <summary>
    /// Registers MSBuild assemblies at module initialization time to ensure they are discoverable
    /// for MSBuild task types during test execution.
    /// </summary>
    [ModuleInitializer]
    public static void RegisterMsBuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}

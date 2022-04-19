#nullable disable
using System.Collections.Specialized;

namespace Bench;

public class BenchProject
{
    public string Name { get; set; }
    
    public string CsProjectName { get; set; }
    
    public string[] PublishParams { get; set; }
    
    public Dictionary<string,string> EnvVariables { get; set; }

    public bool? IsBaseLine { get; set; }
    
    public int Port => 8091;
    
    public void SetEnvVariables(StringDictionary envVariables)
    {
        foreach (var kvp in EnvVariables)
        {
            envVariables.Add(kvp.Key, kvp.Value);
        }
    }

    public string GetCsProjectName => string.IsNullOrEmpty(CsProjectName) ? "SingleFilePublish.csproj" : $"{CsProjectName}.csproj";
    
    public string GetPublishPath => Path.Combine(@".\Publish\", Name);
    
    public string GetExePath => Path.Combine(GetPublishPath, string.IsNullOrEmpty(CsProjectName) ? "SingleFilePublish.exe" : $"{CsProjectName}.exe");
}

public class BenchResult
{
    public BenchProject Project { get; set; }
    
    public double PublishTimeSec { get; set; }
    
    public double PublishDirectorySizeMB { get; set; }
    
    public double ProgramSizeMB { get; set; }
    
    public double StartupTimeMs { get; set; }
    
    public double AppStartupTimeMs { get; set; }
    
    public double StartupMemoryUsageMB { get; set; }

    public double StressTestCompleteMaxTimeMs { get; set; }
    
    public double StressTestCompleteAvgTimeMs { get; set; }
    
    public double StressTestCompleteMinTimeMs { get; set; }
    
    public double StressTestSingleMaxTimeMs { get; set; }
    
    public double StressTestSingleAvgTimeMs { get; set; }
    
    public double StressTestSingleMinTimeMs { get; set; }
    
    public double StressTestCompleteMaxMemoryUsageMB { get; set; }
    
    public double StressTestCompleteAvgMemoryUsageMB { get; set; }
    
    public double StressTestCompleteMinMemoryUsageMB { get; set; }
    
    public double StressTestCompleteMaxCpuUsage { get; set; }
    
    public double StressTestCompleteAvgCpuUsage { get; set; }
    
    public double StressTestCompleteMinCpuUsage { get; set; }
    public double StressTestQps { get; set; }
    public double StressTestSingleP95TimeMs { get; set; }
    public double StressTestSingleP99TimeMs { get; set; }
}
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bench;

var benchProjectsJson = File.ReadAllText(@".\bench.json");
var benchProjects = JsonSerializer.Deserialize<BenchProject[]>(benchProjectsJson);
if (benchProjects is null) throw new InvalidOperationException("bench.json is invalid");

Environment.CurrentDirectory = @"E:\MyCode\SingleFilePublish";

// clean up
if (Directory.Exists(@".\Publish"))
{
    Directory.Delete(@".\Publish", true);
}

// build stress
var arguments = @"publish .\HttpStress\HttpStress.csproj -o .\Publish\HttpStress";
Process.Start("dotnet", arguments).WaitForExit();

var benchResults = new List<BenchResult>();
foreach (var project in benchProjects)
{
    Console.WriteLine($"Start {project.Name}");
    benchResults.Add(DoBench(project));
}

WriteResults(benchResults);
Console.WriteLine("Benchmark finished");
Console.ReadLine();

void WriteResults(List<BenchResult> results)
{
    var fileName = $"./Result-{DateTime.Now:yyMMddHHmmss}";
    // write results
    File.WriteAllText($"{fileName}.json",
        JsonSerializer.Serialize(results, new JsonSerializerOptions {WriteIndented = true}));
    
    var csvResult = new StringBuilder("\uFEFF");
    csvResult.Append("项目,发布耗时(s),目录大小(mb),程序大小,启动耗时,应用启动耗时,应用启动内存,压测QPS,压测耗时-最大(ms),压测耗时-平均,压测耗时-最小,");
    csvResult.Append("单次请求耗时-最大,单次请求耗时-平均,单次请求耗时-最小,单次请求耗时-95%,单次请求耗时-99%,压测内存-最大,压测内存-平均,压测内存-最小,");
    csvResult.Append("压测CPU-最大,压测CPU-平均,压测CPU-最小\r\n");
    foreach(var item in results)
    {
        csvResult.Append($"{item.Project.Name},");
        csvResult.Append($"={item.PublishTimeSec:F},");
        csvResult.Append($"={item.PublishDirectorySizeMB:F},");
        csvResult.Append($"={item.ProgramSizeMB:F},");
        csvResult.Append($"={item.StartupTimeMs:F},");
        csvResult.Append($"={item.AppStartupTimeMs:F},");
        csvResult.Append($"={item.StartupMemoryUsageMB:F},");
        csvResult.Append($"={item.StressTestQps:F},");
        csvResult.Append($"={item.StressTestCompleteMaxTimeMs:F},");
        csvResult.Append($"={item.StressTestCompleteAvgTimeMs:F},");
        csvResult.Append($"={item.StressTestCompleteMinTimeMs:F},");
        csvResult.Append($"={item.StressTestSingleMaxTimeMs:F},");
        csvResult.Append($"={item.StressTestSingleAvgTimeMs:F},");
        csvResult.Append($"={item.StressTestSingleMinTimeMs:F},");
        csvResult.Append($"=\'{item.StressTestSingleP95TimeMs:F}\',");
        csvResult.Append($"=\'{item.StressTestSingleP99TimeMs:F}\',");
        csvResult.Append($"={item.StressTestCompleteMaxMemoryUsageMB:F},");
        csvResult.Append($"={item.StressTestCompleteAvgMemoryUsageMB:F},");
        csvResult.Append($"={item.StressTestCompleteMinMemoryUsageMB:F},");
        csvResult.Append($"={item.StressTestCompleteMaxCpuUsage:F},");
        csvResult.Append($"={item.StressTestCompleteAvgCpuUsage:F},");
        csvResult.Append($"={item.StressTestCompleteMinCpuUsage:F}");
        csvResult.Append("\r\n");
    }

    File.WriteAllText($"{fileName}.csv", csvResult.ToString());
}


BenchResult DoBench(BenchProject project)
{
    var result = new BenchResult
    {
        Project = project
    };

    try
    {
        DoPublishBench(project, result);
        DoStartupTimeBench(project, result);
        DoStressTestBench(project, result);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
    return result;
}

void DoPublishBench(BenchProject project, BenchResult result)
{
    var timeList = new List<long>();
    var directorySizeList = new List<long>();
    var fileSizeList = new List<long>();
    var index = 5;
    while (index-- > 0)
    {
        // clean up
        if (Directory.Exists($@".\Publish\{project.Name}"))
        {
            Directory.Delete($@".\Publish\{project.Name}", true);
        }
        if (Directory.Exists(@".\SingleFilePublish\bin"))
        {
            Directory.Delete(@".\SingleFilePublish\bin", true);
        }
        if (Directory.Exists(@".\SingleFilePublish\obj"))
        {
            Directory.Delete(@".\SingleFilePublish\obj", true);
        }

        // publish
        var arguments =
            @$"publish .\SingleFilePublish\{project.GetCsProjectName} -o .\Publish\{project.Name} {(string.Join(' ', project.PublishParams))}";
        var sw = Stopwatch.StartNew();
        Process.Start("dotnet", arguments).WaitForExit();
        timeList.Add(sw.ElapsedMilliseconds);
        directorySizeList.Add(project.GetPublishPath.GetDirectorySize());
        fileSizeList.Add(project.GetPublishPath.GetDirectorySize(".pdb"));
    }

    result.PublishTimeSec = timeList.Average() / 1000.0;
    result.PublishDirectorySizeMB = directorySizeList.Average() / 1000.0 / 1000.0;
    result.ProgramSizeMB = fileSizeList.Average() / 1000.0 / 1000.0;
}

void DoStartupTimeBench(BenchProject project, BenchResult result)
{
    var startupTimeList = new List<long>();
    var appStartupTimeList = new List<long>();
    var startupMemoryUsageList = new List<double>();
    var index = 5;

    // step2: startup time
    var startInfo = new ProcessStartInfo
    {
        FileName = project.GetExePath,
        RedirectStandardOutput = true
    };
    project.SetEnvVariables(startInfo.EnvironmentVariables);
    while (index-- > 0)
    {
        var process = new Process
        {
            StartInfo = startInfo,
        };
        var processStartTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        process.Start();
        var startupTime = process.StandardOutput.ReadLine();
        var appStartupTime = process.StandardOutput.ReadLine();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var prcName = process.ProcessName;
            var rawValue = new PerformanceCounter("Process", "Working Set - Private", prcName);
            startupMemoryUsageList.Add(rawValue.NextValue());
        }

        process.Kill();
        startupTimeList.Add(long.Parse(startupTime!) - processStartTimestamp);
        appStartupTimeList.Add(long.Parse(appStartupTime!) - processStartTimestamp);
    }

    result.StartupTimeMs = startupTimeList.Average();
    result.AppStartupTimeMs = appStartupTimeList.Average();
    result.StartupMemoryUsageMB = startupMemoryUsageList.Average() / 1000.0 / 1000.0;
    ;
}

void DoStressTestBench(BenchProject project, BenchResult result)
{
    var index = 5;
    var startInfo = new ProcessStartInfo
    {
        FileName = project.GetExePath,
        RedirectStandardOutput = true,
        EnvironmentVariables =
        {
            {"ASPNETCORE_URLS", $"http://localhost:{project.Port}"},
            {"System.GC.HeapAffinitizeMask", "3C"}
        },
        Arguments = "3C" // bind cpu 2,3,4,5 core
    };
    project.SetEnvVariables(startInfo.EnvironmentVariables);

    var stressStartInfo = new ProcessStartInfo()
    {
        FileName = @".\Publish\HttpStress\HttpStress.exe",
        RedirectStandardOutput = true,
        EnvironmentVariables =
        {
            {"DOTNET_ReadyToRun", "0"},
            {"DOTNET_TieredPGO", "1"},
            {"DOTNET_TC_CallCounting", "0"},
            {"DOTNET_TC_QuickJitForLoops", "1"},
            {"System.GC.HeapAffinitizeMask", "FC0"}
        },
        Arguments = $"http://localhost:{project.Port} FC0" // 4032 bind cpu 6,7,8,9,10,11 core
    };

    var memoryUsageList = new List<double>();
    var cpuUsageList = new List<double>();
    var totalTimeList = new List<double>();
    var singleRequestTimesMin = new List<double>();
    var singleRequestTimesMax = new List<double>();
    var singleRequestTimesAvg = new List<double>();
    var qpsList = new List<double>();
    var p95List = new List<double>();
    var p99List = new List<double>();
    while (index-- > 0)
    {
        var cancelSource = new CancellationTokenSource();
        var token = cancelSource.Token;
        // wait for the app to be ready
        var process = new Process
        {
            StartInfo = startInfo,
        };
        process.Start();
        _ = process.StandardOutput.ReadLine();
        _ = process.StandardOutput.ReadLine();
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                Console.WriteLine(args.Data);
            }
        };


        var memoryUsage = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);
        var cpuUsage = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
        new Thread(() =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                while (token.IsCancellationRequested == false)
                {
                    memoryUsageList.Add(memoryUsage.NextValue());
                    cpuUsageList.Add(cpuUsage.NextValue());
                    Thread.Sleep(50);
                }
            }
        }).Start();


        // start stress
        var stressProcess = new Process
        {
            StartInfo = stressStartInfo
        };
        stressProcess.Start();
        totalTimeList.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        singleRequestTimesMin.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        singleRequestTimesAvg.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        singleRequestTimesMax.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        qpsList.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        p95List.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));
        p99List.Add(double.Parse(stressProcess.StandardOutput.ReadLine()!));

        cancelSource.Cancel();
        Thread.Sleep(500);

        process.Kill();
        stressProcess.Kill();
    }

    result.StressTestCompleteMaxTimeMs = totalTimeList.Max();
    result.StressTestCompleteMinTimeMs = totalTimeList.Min();
    result.StressTestCompleteAvgTimeMs = totalTimeList.Average();
    result.StressTestSingleMaxTimeMs = singleRequestTimesMax.Average();
    result.StressTestSingleAvgTimeMs = singleRequestTimesAvg.Average();
    result.StressTestSingleMinTimeMs = singleRequestTimesMin.Average();
    result.StressTestSingleP95TimeMs = p95List.Average();
    result.StressTestSingleP99TimeMs = p99List.Average();
    result.StressTestCompleteMaxMemoryUsageMB = memoryUsageList.Max() / 1000.0 / 1000.0;
    result.StressTestCompleteAvgMemoryUsageMB = memoryUsageList.Average() / 1000.0 / 1000.0;
    result.StressTestCompleteMinMemoryUsageMB = memoryUsageList.Min() / 1000.0 / 1000.0;
    var validCpuUsageList = cpuUsageList.Where(x => x > 0).ToList();
    result.StressTestCompleteMaxCpuUsage = validCpuUsageList.Max();
    result.StressTestCompleteAvgCpuUsage = validCpuUsageList.Average();
    result.StressTestCompleteMinCpuUsage = validCpuUsageList.Min();
    result.StressTestQps = qpsList.Average();
}
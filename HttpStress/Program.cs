//Stress Test, Use httpclient 50 connection and 50 threads request http://localhost:8080/

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;

var url = "http://localhost:5000";
if (args.Length > 0)
{
    url = args[0];
}

if (args.Length > 1)
{
    Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)long.Parse(args[1],NumberStyles.HexNumber);
}

ThreadPool.SetMinThreads(100, 50);
var httpClients = Enumerable.Range(0, 50).Select(i =>
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
    httpClient.DefaultRequestHeaders.Add("keep-alive", "true");
    _ = httpClient.GetAsync(url).Result;
    return httpClient;
}).ToArray();

// warm up
await Parallel.ForEachAsync(Enumerable.Range(0, 100000), new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (i, token) =>
{
    var httpClient = httpClients[i % 50];
    var response = await httpClient.GetAsync(url);
    _ = await response.Content.ReadAsStringAsync();
});

var count = 200000;
// test
var totalTime = Stopwatch.StartNew();
var singleRequestTimes = new ConcurrentBag<long>();
await Parallel.ForEachAsync(Enumerable.Range(0, count), new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (i, token) =>
{
    var httpClient = httpClients[i % 50];
    var sw = Stopwatch.StartNew();
    var response = await httpClient.GetAsync(url);
    _ = await response.Content.ReadAsStringAsync();
    singleRequestTimes.Add(sw.ElapsedMilliseconds);
});
var totalTimeMs = totalTime.ElapsedMilliseconds;
Console.WriteLine(totalTimeMs);
Console.WriteLine(singleRequestTimes.Min());
Console.WriteLine(singleRequestTimes.Average());
Console.WriteLine(singleRequestTimes.Max());
Console.WriteLine(count / (totalTimeMs / 1000.0));
// calculate p95 p99
var singleRequestTimesSortMin = singleRequestTimes.OrderBy(x => x).ToList();
var p95 = singleRequestTimesSortMin[(int)Math.Ceiling(singleRequestTimesSortMin.Count * 0.95) - 1];
var p99 = singleRequestTimesSortMin[(int)Math.Ceiling(singleRequestTimesSortMin.Count * 0.99) - 1];
Console.WriteLine(p95);
Console.WriteLine(p99);
Console.ReadLine();
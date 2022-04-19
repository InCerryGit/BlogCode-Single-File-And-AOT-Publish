using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

Console.WriteLine(DateTimeOffset.Now.ToUnixTimeMilliseconds());

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();


var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine(DateTimeOffset.Now.ToUnixTimeMilliseconds());
    if (args.Length > 0 && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
    {
        Process.GetCurrentProcess().ProcessorAffinity = (IntPtr) long.Parse(args[0], NumberStyles.HexNumber);
    }

});

app.Run();
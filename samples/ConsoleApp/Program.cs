// See https://aka.ms/new-console-template for more information
using System.Text.Json;

Console.WriteLine("===================================");
Console.WriteLine("  Console App Sample");
Console.WriteLine("  JD.MSBuild.Containers Demo");
Console.WriteLine("===================================");
Console.WriteLine();

var info = new
{
    application = "ConsoleApp",
    version = "1.0.0",
    timestamp = DateTime.UtcNow,
    environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
    framework = Environment.Version.ToString(),
    platform = Environment.OSVersion.ToString()
};

Console.WriteLine("Application Information:");
Console.WriteLine(JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine();

Console.WriteLine("Processing tasks...");
for (int i = 1; i <= 5; i++)
{
    Console.WriteLine($"Task {i}/5: Processing...");
    await Task.Delay(500);
    Console.WriteLine($"Task {i}/5: Complete");
}

Console.WriteLine();
Console.WriteLine("All tasks completed successfully!");
Console.WriteLine("Application exiting...");


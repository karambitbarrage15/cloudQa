using Microsoft.Extensions.Logging;
using ResilientFormAutomation.Core;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Contains("--help") || args.Contains("-h"))
        {
            PrintHelp();
            return 0;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("==================================================");
        logger.LogInformation(" CloudQA Task 1: Resilient Form Automation (C#)  ");
        logger.LogInformation("==================================================");

        var settings = ConfigurationLoader.Load(args);

        logger.LogInformation("Configured Settings:");
        logger.LogInformation("  URL       : {Url}", settings.PageUrl);
        logger.LogInformation("  Headless  : {Headless}", settings.Headless);
        logger.LogInformation("  FirstName : {Fn}", settings.TestData.FirstName);
        logger.LogInformation("  Gender    : {Gender}", settings.TestData.Gender);
        logger.LogInformation("  State     : {State}", settings.TestData.State);
        logger.LogInformation("");

        var automator = new FormAutomator(settings, logger);
        var report = automator.Run();

        Console.WriteLine();
        if (report.OverallSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(">>> TEST RESULT: PASS <<<");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(report.ScreenshotPath))
            {
                Console.WriteLine($"Screenshot saved at: {report.ScreenshotPath}");
            }
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(">>> TEST RESULT: FAIL <<<");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(report.ErrorMessage))
            {
                Console.WriteLine($"Error: {report.ErrorMessage}");
            }
            if (!string.IsNullOrEmpty(report.ScreenshotPath))
            {
                Console.WriteLine($"Failure Screenshot: {report.ScreenshotPath}");
            }
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Usage:
  dotnet run --project ResilientFormAutomation.Cli [OPTIONS]

Options:
  -u, --url <URL>           Target web page URL (Default: https://app.cloudqa.io/home/AutomationPracticeForm)
  --headless                Run Chrome in headless mode (default: false)
  --no-headless             Run Chrome with browser UI visible
  --firstname <name>        First Name value to fill (Default: Jane)
  --gender <Male|Female>    Gender radio button option to select (Default: Female)
  --state <country/state>   State dropdown option to select (Default: Australia)
  --timeout <seconds>       Timeout in seconds per field resolution (Default: 15)
  -h, --help                Show this help message

Configuration File:
  Settings can also be modified in 'appsettings.json' without passing CLI arguments.
");
    }
}

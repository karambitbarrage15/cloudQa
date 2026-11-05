using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ResilientFormAutomation.Core.Config;

public static class ConfigurationLoader
{
    public static AutomationSettings Load(string[]? args = null, string? jsonPath = null)
    {
        var settings = new AutomationSettings();

        // 1. Try loading from appsettings.json if present
        var configFile = jsonPath ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(configFile))
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(Path.GetFileName(configFile), optional: true, reloadOnChange: false);

            var config = builder.Build();
            config.GetSection("AutomationSettings").Bind(settings);
        }

        // 2. Command-line argument overrides
        if (args != null)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if ((arg == "--url" || arg == "-u") && i + 1 < args.Length)
                {
                    settings.PageUrl = args[++i];
                }
                else if (arg == "--headless")
                {
                    settings.Headless = true;
                }
                else if (arg == "--no-headless")
                {
                    settings.Headless = false;
                }
                else if (arg == "--firstname" && i + 1 < args.Length)
                {
                    settings.TestData.FirstName = args[++i];
                }
                else if (arg == "--gender" && i + 1 < args.Length)
                {
                    settings.TestData.Gender = args[++i];
                }
                else if (arg == "--state" && i + 1 < args.Length)
                {
                    settings.TestData.State = args[++i];
                }
                else if (arg == "--timeout" && i + 1 < args.Length && int.TryParse(args[++i], out var t))
                {
                    settings.TimeoutSeconds = t;
                }
            }
        }

        return settings;
    }
}

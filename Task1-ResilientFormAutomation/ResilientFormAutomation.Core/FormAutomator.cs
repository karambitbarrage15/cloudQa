using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ResilientFormAutomation.Core.Actions;
using ResilientFormAutomation.Core.Config;
using ResilientFormAutomation.Core.Locators;

namespace ResilientFormAutomation.Core;

public record FieldExecutionRecord(
    string FieldName,
    string InputValue,
    bool Success,
    string StrategyUsed,
    double Confidence,
    string Message,
    long ElapsedMilliseconds);

public class AutomationReport
{
    public string TargetUrl { get; set; } = string.Empty;
    public bool OverallSuccess { get; set; }
    public long TotalElapsedMilliseconds { get; set; }
    public List<FieldExecutionRecord> Fields { get; set; } = new();
    public string? ScreenshotPath { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// High-level resilient form test automation engine.
/// Coordinates WebDriver, the SmartLocatorChain, and field action handlers.
/// </summary>
public class FormAutomator
{
    private readonly AutomationSettings _settings;
    private readonly ILogger? _logger;
    private readonly SmartLocatorChain _locatorChain;

    public FormAutomator(AutomationSettings settings, ILogger? logger = null)
    {
        _settings = settings;
        _logger = logger;
        _locatorChain = new SmartLocatorChain(logger);
    }

    /// <summary>
    /// Executes the resilient test suite against the configured or specified URL.
    /// </summary>
    public AutomationReport Run(IWebDriver? externalDriver = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var report = new AutomationReport { TargetUrl = _settings.PageUrl };
        var ownsDriver = externalDriver == null;
        var driver = externalDriver ?? CreateWebDriver();

        try
        {
            _logger?.LogInformation("=================================================");
            _logger?.LogInformation("Starting Resilient Form Automation Test");
            _logger?.LogInformation("Target URL : {Url}", _settings.PageUrl);
            _logger?.LogInformation("Headless   : {Headless}", _settings.Headless);
            _logger?.LogInformation("=================================================");

            driver.Navigate().GoToUrl(_settings.PageUrl);

            // Give page 1 second to stabilize if loading dynamic scripts
            Thread.Sleep(1000);

            // 1. Process First Name
            var fnRecord = ExecuteField(
                driver,
                FieldDescriptor.FirstName(_settings.TestData.FirstName),
                new TextFieldAction());
            report.Fields.Add(fnRecord);

            // 2. Process Gender
            var genderRecord = ExecuteField(
                driver,
                FieldDescriptor.Gender(_settings.TestData.Gender),
                new RadioButtonAction());
            report.Fields.Add(genderRecord);

            // 3. Process State
            var stateRecord = ExecuteField(
                driver,
                FieldDescriptor.State(_settings.TestData.State),
                new DropdownAction());
            report.Fields.Add(stateRecord);

            report.OverallSuccess = report.Fields.All(f => f.Success);

            // Take summary screenshot
            report.ScreenshotPath = CaptureScreenshot(driver, "run_completion");
        }
        catch (Exception ex)
        {
            report.OverallSuccess = false;
            report.ErrorMessage = ex.Message;
            _logger?.LogError(ex, "Automation failed with unhandled exception.");
            report.ScreenshotPath = CaptureScreenshot(driver, "run_failure");
        }
        finally
        {
            stopwatch.Stop();
            report.TotalElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            if (ownsDriver)
            {
                driver.Quit();
                driver.Dispose();
            }
        }

        PrintSummary(report);
        return report;
    }

    private FieldExecutionRecord ExecuteField(IWebDriver driver, FieldDescriptor descriptor, IFormAction action)
    {
        var sw = Stopwatch.StartNew();
        var locResult = _locatorChain.Resolve(
            driver,
            descriptor,
            timeoutSeconds: _settings.TimeoutSeconds,
            highlight: _settings.HighlightElements);

        if (!locResult.Success || locResult.Element == null)
        {
            sw.Stop();
            return new FieldExecutionRecord(
                descriptor.CanonicalName,
                descriptor.ExpectedValue ?? string.Empty,
                false,
                "None",
                0.0,
                $"Failed to locate field: {locResult.Details}",
                sw.ElapsedMilliseconds);
        }

        var actResult = action.Execute(driver, locResult.Element, descriptor.ExpectedValue ?? string.Empty);
        sw.Stop();

        if (_settings.ActionDelayMilliseconds > 0)
        {
            Thread.Sleep(_settings.ActionDelayMilliseconds);
        }

        return new FieldExecutionRecord(
            descriptor.CanonicalName,
            descriptor.ExpectedValue ?? string.Empty,
            actResult.Success,
            locResult.StrategyName,
            locResult.Confidence,
            actResult.Message,
            sw.ElapsedMilliseconds);
    }

    public IWebDriver CreateWebDriver()
    {
        var options = new ChromeOptions();
        if (_settings.Headless)
        {
            options.AddArgument("--headless=new");
        }
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--window-size=1400,900");
        options.AddArgument("--disable-notifications");

        return new ChromeDriver(options);
    }

    private string? CaptureScreenshot(IWebDriver driver, string prefix)
    {
        try
        {
            if (driver is ITakesScreenshot ts)
            {
                var screenshotsDir = Path.Combine(AppContext.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(screenshotsDir);
                var filename = $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                var fullPath = Path.Combine(screenshotsDir, filename);
                ts.GetScreenshot().SaveAsFile(fullPath);
                return fullPath;
            }
        }
        catch { }
        return null;
    }

    private void PrintSummary(AutomationReport report)
    {
        _logger?.LogInformation("-------------------------------------------------");
        _logger?.LogInformation("EXECUTION SUMMARY:");
        _logger?.LogInformation("Target URL : {Url}", report.TargetUrl);
        _logger?.LogInformation("Overall    : {Status}", report.OverallSuccess ? "PASS" : "FAIL");
        _logger?.LogInformation("Duration   : {Ms} ms", report.TotalElapsedMilliseconds);
        foreach (var f in report.Fields)
        {
            _logger?.LogInformation("  [{Status}] {Name} -> Val: '{Val}' | Strategy: {Strat} ({Conf:P0}) | {Ms}ms | {Msg}",
                f.Success ? "PASS" : "FAIL",
                f.FieldName,
                f.InputValue,
                f.StrategyUsed,
                f.Confidence,
                f.ElapsedMilliseconds,
                f.Message);
        }
        _logger?.LogInformation("-------------------------------------------------");
    }
}

using OpenQA.Selenium;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Result of an element resolution attempt by a locator strategy.
/// </summary>
public class LocatorResult
{
    public IWebElement? Element { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Details { get; set; } = string.Empty;
    public bool Success => Element != null;

    public static LocatorResult Failed(string strategy, string reason) => new()
    {
        Element = null,
        StrategyName = strategy,
        Confidence = 0.0,
        Details = reason
    };

    public static LocatorResult Succeeded(IWebElement element, string strategy, double confidence, string details) => new()
    {
        Element = element,
        StrategyName = strategy,
        Confidence = confidence,
        Details = details
    };
}

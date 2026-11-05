using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Orchestrates the chain of locator strategies in prioritized order.
/// Provides fallback, diagnostic logging, and self-healing telemetry.
/// </summary>
public class SmartLocatorChain
{
    private readonly List<ISmartLocatorStrategy> _strategies;
    private readonly ILogger? _logger;

    public SmartLocatorChain(ILogger? logger = null)
    {
        _logger = logger;
        _strategies = new List<ISmartLocatorStrategy>
        {
            new LabelAssociationStrategy(),
            new AttributeSemanticStrategy(),
            new ProximityHeuristicStrategy(),
            new ShadowDomAndSlotStrategy(),
            new JsScoringFallbackStrategy()
        };
    }

    /// <summary>
    /// Locates the form element using the strategy chain with explicit wait and self-healing fallback.
    /// </summary>
    public LocatorResult Resolve(IWebDriver driver, FieldDescriptor descriptor, int timeoutSeconds = 10, bool highlight = true)
    {
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutSeconds))
        {
            PollingInterval = TimeSpan.FromMilliseconds(250)
        };
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

        _logger?.LogInformation("==> Locating field '{FieldName}' (Type: {Type}) across {Count} strategies...",
            descriptor.CanonicalName, descriptor.Type, _strategies.Count);

        try
        {
            var result = wait.Until(_ =>
            {
                foreach (var strategy in _strategies)
                {
                    try
                    {
                        var res = strategy.FindElement(driver, descriptor);
                        if (res.Success && res.Element != null)
                        {
                            return res;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug("Strategy '{Name}' threw: {Message}", strategy.Name, ex.Message);
                    }
                }
                return null;
            });

            if (result != null && result.Element != null)
            {
                _logger?.LogInformation("    [SUCCESS] Resolved '{FieldName}' via '{Strategy}' (Confidence: {Confidence:P0}) - {Details}",
                    descriptor.CanonicalName, result.StrategyName, result.Confidence, result.Details);

                if (highlight)
                {
                    HighlightElement(driver, result.Element);
                }

                return result;
            }
        }
        catch (WebDriverTimeoutException)
        {
            _logger?.LogError("    [FAILED] Timeout searching for '{FieldName}' after {Seconds}s across all strategies.",
                descriptor.CanonicalName, timeoutSeconds);
        }

        return LocatorResult.Failed("Chain Exhaustion", $"Could not locate field '{descriptor.CanonicalName}' using any strategy.");
    }

    private static void HighlightElement(IWebDriver driver, IWebElement element)
    {
        try
        {
            if (driver is IJavaScriptExecutor js)
            {
                js.ExecuteScript(
                    "arguments[0].style.outline = '3px solid #28a745'; arguments[0].style.transition = 'all 0.3s';",
                    element);
            }
        }
        catch
        {
            // Ignore highlighting errors
        }
    }
}

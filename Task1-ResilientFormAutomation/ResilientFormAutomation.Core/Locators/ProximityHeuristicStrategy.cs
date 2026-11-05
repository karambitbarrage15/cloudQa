using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy 3: Visual and DOM proximity heuristics.
/// Identifies any text node matching target synonyms (in &lt;span&gt;, &lt;div&gt;, &lt;p&gt;, &lt;td&gt;, etc.)
/// and locates the nearest interactive control within the same row/container or adjacent in DOM.
/// </summary>
public class ProximityHeuristicStrategy : ISmartLocatorStrategy
{
    public string Name => "Proximity-Heuristic Strategy";

    public LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        var textTags = new[] { "span", "div", "p", "td", "th", "h4", "h5", "h6", "strong", "b" };
        var candidateNodes = new List<(IWebElement textNode, string text, double relevance)>();

        foreach (var tag in textTags)
        {
            var elements = driver.FindElements(By.TagName(tag));
            foreach (var el in elements)
            {
                try
                {
                    if (!el.Displayed) continue;
                    var text = el.Text?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text) || text.Length > 60) continue;

                    foreach (var syn in descriptor.Synonyms)
                    {
                        if (string.Equals(text, syn, StringComparison.OrdinalIgnoreCase))
                        {
                            candidateNodes.Add((el, text, 1.0));
                            break;
                        }
                        if (text.Contains(syn, StringComparison.OrdinalIgnoreCase))
                        {
                            candidateNodes.Add((el, text, 0.75));
                            break;
                        }
                    }
                }
                catch (StaleElementReferenceException) { }
            }
        }

        var orderedCandidates = candidateNodes.OrderByDescending(c => c.relevance).ToList();

        foreach (var (node, text, relevance) in orderedCandidates)
        {
            var targetControl = FindNearestCompatibleControl(node, descriptor.Type);
            if (targetControl != null)
            {
                return LocatorResult.Succeeded(
                    targetControl,
                    Name,
                    relevance * 0.85,
                    $"Located nearest control to text node '{text}'");
            }
        }

        return LocatorResult.Failed(Name, "No compatible control found in proximity to candidate text nodes.");
    }

    private static IWebElement? FindNearestCompatibleControl(IWebElement node, ControlType type)
    {
        try
        {
            // Check immediate parent container
            var containerXPath = "./ancestor::*[contains(@class, 'form-group') or contains(@class, 'row') or contains(@class, 'col') or self::tr or self::div][1]";
            var containers = node.FindElements(By.XPath(containerXPath));
            var container = containers.FirstOrDefault();

            if (container != null)
            {
                var controls = GetControlsOfType(container, type);
                var match = controls.FirstOrDefault(c => c.Displayed);
                if (match != null) return match;
            }

            // Check following sibling or following control
            var followingQuery = type switch
            {
                ControlType.TextInput => "./following::input[not(@type) or @type='text' or @type='email' or @type='tel'][1]",
                ControlType.RadioButton => "./following::input[@type='radio'][1]",
                ControlType.Dropdown => "./following::select[1]",
                _ => "./following::*[self::input or self::select][1]"
            };

            var followings = node.FindElements(By.XPath(followingQuery));
            return followings.FirstOrDefault(f => f.Displayed);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<IWebElement> GetControlsOfType(IWebElement parent, ControlType type)
    {
        var xpath = type switch
        {
            ControlType.TextInput => ".//input[not(@type) or @type='text' or @type='email' or @type='search' or @type='tel'] | .//textarea",
            ControlType.RadioButton => ".//input[@type='radio']",
            ControlType.Dropdown => ".//select",
            _ => ".//input | .//select"
        };

        return parent.FindElements(By.XPath(xpath));
    }
}

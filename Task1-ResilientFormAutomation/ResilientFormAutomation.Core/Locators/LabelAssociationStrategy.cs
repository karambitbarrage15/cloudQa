using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy 1: Finds form controls via semantic label association.
/// Handles:
///   1. Standard &lt;label for="targetId"&gt;
///   2. Nested controls: &lt;label&gt;First Name &lt;input/&gt;&lt;/label&gt;
///   3. Shared container: &lt;div class="form-group"&gt;&lt;label&gt;...&lt;/label&gt;&lt;input/&gt;&lt;/div&gt;
///   4. Trailing siblings &lt;label&gt;First Name&lt;/label&gt;&lt;input/&gt;
/// </summary>
public class LabelAssociationStrategy : ISmartLocatorStrategy
{
    public string Name => "Label-Association Strategy";

    public LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        var allLabels = driver.FindElements(By.TagName("label"));
        var candidates = new List<(IWebElement label, string labelText, int synonymPriority)>();

        foreach (var lbl in allLabels)
        {
            try
            {
                if (!lbl.Displayed) continue;
                var text = lbl.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    // Check inner text or aria-label on label
                    text = lbl.GetAttribute("innerText")?.Trim() ?? string.Empty;
                }

                for (int i = 0; i < descriptor.Synonyms.Count; i++)
                {
                    var syn = descriptor.Synonyms[i];
                    if (MatchesLabelText(text, syn))
                    {
                        candidates.Add((lbl, text, i));
                        break;
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
                // Ignore transient DOM updates
            }
        }

        // Sort candidates by synonym priority
        var ordered = candidates.OrderBy(c => c.synonymPriority).ToList();

        foreach (var (label, matchedText, priority) in ordered)
        {
            // Case 1: Label has 'for' attribute
            var forAttr = label.GetAttribute("for");
            if (!string.IsNullOrWhiteSpace(forAttr))
            {
                var target = FindByIdSafe(driver, forAttr);
                if (target != null && IsCompatible(target, descriptor.Type))
                {
                    return LocatorResult.Succeeded(
                        target,
                        Name,
                        0.98,
                        $"Found via <label for='{forAttr}'> matching text '{matchedText}'");
                }
            }

            // Case 2: Nested control within label
            var nested = FindNestedControl(label, descriptor.Type);
            if (nested != null)
            {
                return LocatorResult.Succeeded(
                    nested,
                    Name,
                    0.95,
                    $"Found nested control inside <label> matching '{matchedText}'");
            }

            // Case 3: Sibling or container control (form-group, row, etc.)
            var containerControl = FindInContainerOrAdjacent(label, descriptor.Type);
            if (containerControl != null)
            {
                return LocatorResult.Succeeded(
                    containerControl,
                    Name,
                    0.90,
                    $"Found control in adjacent container for label '{matchedText}'");
            }
        }

        return LocatorResult.Failed(Name, $"No interactive element resolved for label synonyms: {string.Join(", ", descriptor.Synonyms)}");
    }

    private static bool MatchesLabelText(string actual, string synonym)
    {
        if (string.Equals(actual, synonym, StringComparison.OrdinalIgnoreCase)) return true;

        var cleanActual = CleanString(actual);
        var cleanSyn = CleanString(synonym);

        if (cleanActual == cleanSyn) return true;
        if (cleanActual.StartsWith(cleanSyn) || cleanActual.EndsWith(cleanSyn)) return true;

        return false;
    }

    private static string CleanString(string s)
    {
        return new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    private static IWebElement? FindByIdSafe(IWebDriver driver, string id)
    {
        try
        {
            var el = driver.FindElement(By.Id(id));
            return el.Displayed ? el : null;
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    private static IWebElement? FindNestedControl(IWebElement label, ControlType type)
    {
        try
        {
            var tagQuery = type switch
            {
                ControlType.TextInput => ".//input[not(@type) or @type='text' or @type='email' or @type='tel']",
                ControlType.RadioButton => ".//input[@type='radio']",
                ControlType.Dropdown => ".//select",
                _ => ".//input | .//select"
            };

            var elements = label.FindElements(By.XPath(tagQuery));
            return elements.FirstOrDefault(e => e.Displayed);
        }
        catch
        {
            return null;
        }
    }

    private static IWebElement? FindInContainerOrAdjacent(IWebElement label, ControlType type)
    {
        try
        {
            // Look up to 3 parent levels to find container (e.g. form-group, div, tr)
            var containerXPath = "./ancestor::*[contains(@class, 'form-group') or contains(@class, 'field') or contains(@class, 'control') or contains(@class, 'row') or self::tr or self::div][1]";
            var containers = label.FindElements(By.XPath(containerXPath));
            var container = containers.FirstOrDefault();

            if (container != null)
            {
                var query = type switch
                {
                    ControlType.TextInput => ".//input[not(@type) or @type='text' or @type='email' or @type='search' or @type='tel']",
                    ControlType.RadioButton => ".//input[@type='radio']",
                    ControlType.Dropdown => ".//select",
                    _ => ".//input | .//select"
                };

                var matches = container.FindElements(By.XPath(query));
                var match = matches.FirstOrDefault(m => m.Displayed);
                if (match != null) return match;
            }

            // Fallback: immediate following sibling or nearest following control
            var followingQuery = type switch
            {
                ControlType.TextInput => "./following::input[not(@type) or @type='text' or @type='email' or @type='tel'][1]",
                ControlType.RadioButton => "./following::input[@type='radio'][1]",
                ControlType.Dropdown => "./following::select[1]",
                _ => "./following::*[self::input or self::select][1]"
            };

            var followings = label.FindElements(By.XPath(followingQuery));
            return followings.FirstOrDefault(f => f.Displayed);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsCompatible(IWebElement element, ControlType type)
    {
        var tag = element.TagName.ToLowerInvariant();
        var inputType = element.GetAttribute("type")?.ToLowerInvariant() ?? "text";

        return type switch
        {
            ControlType.TextInput => tag == "input" && (inputType == "text" || inputType == "email" || inputType == "search" || inputType == "tel") || tag == "textarea",
            ControlType.RadioButton => tag == "input" && inputType == "radio",
            ControlType.Dropdown => tag == "select" || element.GetAttribute("role") == "combobox" || element.GetAttribute("role") == "listbox",
            _ => true
        };
    }
}

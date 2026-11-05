using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy 2: Finds controls based on semantic attribute matching (name, placeholder, aria-label, data-testid, etc.)
/// without requiring a traditional &lt;label&gt; tag.
/// </summary>
public class AttributeSemanticStrategy : ISmartLocatorStrategy
{
    public string Name => "Attribute-Semantic Strategy";

    public LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        var targetTags = descriptor.Type switch
        {
            ControlType.Dropdown => new[] { "select", "div", "button" },
            ControlType.RadioButton => new[] { "input" },
            _ => new[] { "input", "textarea" }
        };

        var candidateElements = new List<(IWebElement element, double score, string reason)>();

        foreach (var tag in targetTags)
        {
            var elements = driver.FindElements(By.TagName(tag));
            foreach (var el in elements)
            {
                try
                {
                    if (!el.Displayed) continue;
                    if (!IsTypeCompatible(el, descriptor.Type)) continue;

                    var (score, reason) = ScoreElementAttributes(el, descriptor);
                    if (score > 0.4)
                    {
                        candidateElements.Add((el, score, reason));
                    }
                }
                catch (StaleElementReferenceException) { }
            }
        }

        var best = candidateElements.OrderByDescending(c => c.score).FirstOrDefault();
        if (best.element != null)
        {
            return LocatorResult.Succeeded(best.element, Name, best.score, best.reason);
        }

        return LocatorResult.Failed(Name, "No matching element found via attribute semantics.");
    }

    private static (double score, string reason) ScoreElementAttributes(IWebElement el, FieldDescriptor descriptor)
    {
        var attributesToInspect = new[]
        {
            ("aria-label", 0.95),
            ("placeholder", 0.90),
            ("name", 0.85),
            ("data-testid", 0.85),
            ("data-qa", 0.85),
            ("data-cy", 0.85),
            ("id", 0.80),
            ("title", 0.75)
        };

        double maxScore = 0.0;
        string bestReason = string.Empty;

        foreach (var (attrName, weight) in attributesToInspect)
        {
            var attrVal = el.GetAttribute(attrName);
            if (string.IsNullOrWhiteSpace(attrVal)) continue;

            // Check exact synonym match
            foreach (var syn in descriptor.Synonyms)
            {
                if (string.Equals(attrVal, syn, StringComparison.OrdinalIgnoreCase))
                {
                    var score = weight;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestReason = $"Exact match on {attrName}='{attrVal}' (synonym: '{syn}')";
                    }
                }
                else if (attrVal.Contains(syn, StringComparison.OrdinalIgnoreCase))
                {
                    var score = weight * 0.85;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestReason = $"Partial match on {attrName}='{attrVal}' contains '{syn}'";
                    }
                }
            }

            // Check possible substrings (e.g. fname, firstname)
            foreach (var substr in descriptor.PossibleAttributeSubstrings)
            {
                var cleanAttrVal = CleanString(attrVal);
                var cleanSubstr = CleanString(substr);
                if (cleanAttrVal.Contains(cleanSubstr))
                {
                    var score = weight * 0.80;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestReason = $"Substring match on {attrName}='{attrVal}' contains '{substr}'";
                    }
                }
            }
        }

        return (maxScore, bestReason);
    }

    private static bool IsTypeCompatible(IWebElement el, ControlType type)
    {
        var tag = el.TagName.ToLowerInvariant();
        var inputType = el.GetAttribute("type")?.ToLowerInvariant() ?? "text";

        return type switch
        {
            ControlType.TextInput => tag == "textarea" || (tag == "input" && inputType is "text" or "email" or "search" or "tel" or ""),
            ControlType.RadioButton => tag == "input" && inputType == "radio",
            ControlType.Dropdown => tag == "select" || el.GetAttribute("role") is "combobox" or "listbox",
            _ => true
        };
    }

    private static string CleanString(string s) =>
        new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}

using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy 5: Multi-signal in-page JavaScript scoring engine.
/// Evaluates every potential input candidate on the page across 5 heuristic dimensions:
///   1. Semantic distance to matching text nodes
///   2. Attribute similarity (ID, Name, Class, Placeholder, Title, ARIA)
///   3. Type compatibility
///   4. Negative penalty filtering (prevents matching "Last Name" or "Middle Name" for "First Name")
///   5. Element visibility and interactive state
/// </summary>
public class JsScoringFallbackStrategy : ISmartLocatorStrategy
{
    public string Name => "JS Multi-Signal Scoring Engine";

    public LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return LocatorResult.Failed(Name, "Driver does not support JavaScript execution.");
        }

        var controlTag = descriptor.Type switch
        {
            ControlType.Dropdown => "select, [role='combobox'], [role='listbox']",
            ControlType.RadioButton => "input[type='radio']",
            _ => "input:not([type='radio']):not([type='checkbox']):not([type='submit']):not([type='button']), textarea"
        };

        // Negative indicator words to prevent confusion (e.g., "last" when looking for "first")
        var negativeIndicators = descriptor.CanonicalName.ToLowerInvariant() switch
        {
            "first name" => new[] { "last", "lname", "surname", "middle", "user", "nick" },
            "gender" => new[] { "hobbies", "interest", "newsletter", "terms" },
            "state" => new[] { "city", "zip", "postal", "street", "address" },
            _ => Array.Empty<string>()
        };

        var scoringScript = @"
            const targetName = arguments[0];
            const synonyms = arguments[1].map(s => s.toLowerCase());
            const substrings = arguments[2].map(s => s.toLowerCase());
            const selector = arguments[3];
            const negativeWords = arguments[4].map(w => w.toLowerCase());

            const elements = Array.from(document.querySelectorAll(selector));
            let bestCandidate = null;
            let maxScore = -1;
            let breakdown = '';

            function isVisible(el) {
                const style = window.getComputedStyle(el);
                return style.display !== 'none' &&
                       style.visibility !== 'hidden' &&
                       style.opacity !== '0' &&
                       el.offsetWidth > 0 &&
                       el.offsetHeight > 0;
            }

            for (const el of elements) {
                if (!isVisible(el)) continue;

                let score = 0;
                const matches = [];

                // Attributes inspection
                const attrs = {
                    id: el.id || '',
                    name: el.name || '',
                    placeholder: el.placeholder || '',
                    ariaLabel: el.getAttribute('aria-label') || '',
                    title: el.title || '',
                    className: el.className || ''
                };

                // Check for negative words first (heavy penalty)
                let hasNegative = false;
                for (const neg of negativeWords) {
                    for (const [k, v] of Object.entries(attrs)) {
                        if (v.toLowerCase().includes(neg)) {
                            hasNegative = true;
                            break;
                        }
                    }
                    if (hasNegative) break;
                }

                if (hasNegative) {
                    continue; // Skip negative matches (e.g. Last Name)
                }

                // Score attributes against synonyms and substrings
                for (const [k, v] of Object.entries(attrs)) {
                    const lowerVal = v.toLowerCase();
                    for (const syn of synonyms) {
                        if (lowerVal === syn) {
                            score += 40;
                            matches.push(`exact ${k}='${v}'`);
                        } else if (lowerVal.includes(syn)) {
                            score += 25;
                            matches.push(`partial ${k}='${v}'`);
                        }
                    }

                    for (const sub of substrings) {
                        if (lowerVal.includes(sub)) {
                            score += 20;
                            matches.push(`substring ${k}='${v}' contains '${sub}'`);
                        }
                    }
                }

                // Check associated label or nearest preceding text
                let labelText = '';
                if (el.id) {
                    const lbl = document.querySelector(`label[for='${el.id}']`);
                    if (lbl) labelText = lbl.innerText || '';
                }
                if (!labelText) {
                    const parentGroup = el.closest('.form-group, .field, div, tr');
                    if (parentGroup) {
                        const lbl = parentGroup.querySelector('label, span, b');
                        if (lbl) labelText = lbl.innerText || '';
                    }
                }

                if (labelText) {
                    const lowerLbl = labelText.toLowerCase();
                    for (const syn of synonyms) {
                        if (lowerLbl.includes(syn)) {
                            score += 35;
                            matches.push(`nearby text '${labelText.trim()}'`);
                            break;
                        }
                    }
                }

                if (score > maxScore && score > 20) {
                    maxScore = score;
                    bestCandidate = el;
                    breakdown = matches.join('; ');
                }
            }

            if (bestCandidate) {
                return {
                    element: bestCandidate,
                    score: Math.min(maxScore / 100, 1.0),
                    details: breakdown
                };
            }

            return null;
        ";

        try
        {
            var result = js.ExecuteScript(
                scoringScript,
                descriptor.CanonicalName,
                descriptor.Synonyms.ToArray(),
                descriptor.PossibleAttributeSubstrings.ToArray(),
                controlTag,
                negativeIndicators);

            if (result is Dictionary<string, object> dict && dict.TryGetValue("element", out var elObj) && elObj is IWebElement el)
            {
                var score = dict.TryGetValue("score", out var s) ? Convert.ToDouble(s) : 0.75;
                var details = dict.TryGetValue("details", out var d) ? d.ToString()! : "Highest composite score";
                return LocatorResult.Succeeded(el, Name, score, details);
            }
        }
        catch (Exception ex)
        {
            return LocatorResult.Failed(Name, $"JS evaluation exception: {ex.Message}");
        }

        return LocatorResult.Failed(Name, "JS scoring found no candidates meeting confidence threshold.");
    }
}

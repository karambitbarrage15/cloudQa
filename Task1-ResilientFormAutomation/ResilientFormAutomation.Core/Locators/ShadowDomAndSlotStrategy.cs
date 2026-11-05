using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy 4: Traverses Shadow DOM roots and Web Component slots.
/// Handles elements projected into &lt;slot&gt; or encapsulated inside shadow roots.
/// </summary>
public class ShadowDomAndSlotStrategy : ISmartLocatorStrategy
{
    public string Name => "Shadow-DOM & Slot Strategy";

    public LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor)
    {
        if (driver is not IJavaScriptExecutor js)
        {
            return LocatorResult.Failed(Name, "Driver does not support JavaScript execution.");
        }

        var controlFilter = descriptor.Type switch
        {
            ControlType.Dropdown => "select",
            ControlType.RadioButton => "input[type='radio']",
            _ => "input:not([type='radio']):not([type='checkbox']):not([type='submit']), textarea"
        };

        var script = @"
            const targetSynonyms = arguments[0];
            const targetSubstrings = arguments[1];
            const selector = arguments[2];

            function getAllElements(root) {
                let list = [];
                const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, null, false);
                while (walker.nextNode()) {
                    const node = walker.currentNode;
                    list.push(node);
                    if (node.shadowRoot) {
                        list = list.concat(getAllElements(node.shadowRoot));
                    }
                }
                return list;
            }

            const allElements = getAllElements(document.body);
            const candidates = [];

            for (const el of allElements) {
                if (!el.matches || !el.matches(selector)) continue;

                let score = 0;
                let matchReason = '';

                // Check attributes (name, id, slot, placeholder, aria-label)
                for (const attr of ['name', 'id', 'slot', 'placeholder', 'aria-label']) {
                    const val = el.getAttribute(attr);
                    if (!val) continue;

                    for (const syn of targetSynonyms) {
                        if (val.toLowerCase().includes(syn.toLowerCase())) {
                            score = Math.max(score, 0.9);
                            matchReason = `Shadow/Slot match on ${attr}='${val}' containing '${syn}'`;
                        }
                    }

                    for (const sub of targetSubstrings) {
                        if (val.toLowerCase().includes(sub.toLowerCase())) {
                            score = Math.max(score, 0.85);
                            matchReason = `Shadow/Slot match on ${attr}='${val}' containing '${sub}'`;
                        }
                    }
                }

                // Check parent section or slot attribute
                const parentSlot = el.closest('[slot]');
                if (parentSlot) {
                    const slotName = parentSlot.getAttribute('slot') || '';
                    for (const sub of targetSubstrings) {
                        if (slotName.toLowerCase().includes(sub.toLowerCase())) {
                            score = Math.max(score, 0.95);
                            matchReason = `Inside slotted container slot='${slotName}'`;
                        }
                    }
                }

                if (score > 0) {
                    candidates.push({ element: el, score: score, reason: matchReason });
                }
            }

            candidates.sort((a, b) => b.score - a.score);
            return candidates.length > 0 ? candidates[0] : null;
        ";

        try
        {
            var result = js.ExecuteScript(
                script,
                descriptor.Synonyms.ToArray(),
                descriptor.PossibleAttributeSubstrings.ToArray(),
                controlFilter);

            if (result is Dictionary<string, object> dict && dict.TryGetValue("element", out var elObj) && elObj is IWebElement el)
            {
                var score = dict.TryGetValue("score", out var s) ? Convert.ToDouble(s) : 0.85;
                var reason = dict.TryGetValue("reason", out var r) ? r.ToString()! : "Shadow DOM candidate";
                return LocatorResult.Succeeded(el, Name, score, reason);
            }
        }
        catch (Exception ex)
        {
            return LocatorResult.Failed(Name, $"Shadow DOM search exception: {ex.Message}");
        }

        return LocatorResult.Failed(Name, "No compatible element found in shadow DOM or slotted components.");
    }
}

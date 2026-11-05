using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace ResilientFormAutomation.Core.Actions;

/// <summary>
/// Resilient interaction for dropdown controls.
/// Handles:
///   1. Standard HTML &lt;select&gt; elements via Selenium SelectElement
///   2. Fuzzy text and value matching (handles extra whitespace, case variance)
///   3. Custom combobox / listbox UI widgets (div/button triggers with popup lists)
///   4. Reactive SPA event dispatch
/// </summary>
public class DropdownAction : IFormAction
{
    public ActionResult Execute(IWebDriver driver, IWebElement element, string desiredOption)
    {
        try
        {
            ScrollIntoView(driver, element);

            var tag = element.TagName.ToLowerInvariant();
            if (tag == "select")
            {
                return HandleNativeSelect(driver, element, desiredOption);
            }
            else
            {
                return HandleCustomDropdown(driver, element, desiredOption);
            }
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Failed to select dropdown option: {ex.Message}");
        }
    }

    private static ActionResult HandleNativeSelect(IWebDriver driver, IWebElement selectElement, string desiredOption)
    {
        var select = new SelectElement(selectElement);
        var options = select.Options;

        // Pass 1: Exact text match (case-insensitive, trimmed)
        foreach (var opt in options)
        {
            var text = opt.Text.Trim();
            if (text.Equals(desiredOption, StringComparison.OrdinalIgnoreCase))
            {
                select.SelectByText(opt.Text);
                DispatchChangeEvent(driver, selectElement);
                return new ActionResult(true, $"Selected '{opt.Text.Trim()}' from native <select>.");
            }
        }

        // Pass 2: Exact value match
        foreach (var opt in options)
        {
            var val = opt.GetAttribute("value")?.Trim() ?? string.Empty;
            if (val.Equals(desiredOption, StringComparison.OrdinalIgnoreCase))
            {
                select.SelectByValue(val);
                DispatchChangeEvent(driver, selectElement);
                return new ActionResult(true, $"Selected value='{val}' from native <select>.");
            }
        }

        // Pass 3: Contains / substring match
        foreach (var opt in options)
        {
            var text = opt.Text.Trim();
            if (text.Contains(desiredOption, StringComparison.OrdinalIgnoreCase) ||
                desiredOption.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                select.SelectByText(opt.Text);
                DispatchChangeEvent(driver, selectElement);
                return new ActionResult(true, $"Selected partial match '{opt.Text.Trim()}' from native <select>.");
            }
        }

        // Pass 4: Fallback to first non-empty option if specified option not found
        var fallbackOpt = options.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.Text) && !o.Text.Contains("Select", StringComparison.OrdinalIgnoreCase));
        if (fallbackOpt != null)
        {
            select.SelectByText(fallbackOpt.Text);
            DispatchChangeEvent(driver, selectElement);
            return new ActionResult(true, $"Target '{desiredOption}' not in options list; fell back to available option '{fallbackOpt.Text.Trim()}'.");
        }

        return new ActionResult(false, $"None of the {options.Count} options matched '{desiredOption}'.");
    }

    private static ActionResult HandleCustomDropdown(IWebDriver driver, IWebElement trigger, string desiredOption)
    {
        // Click trigger to expand options
        trigger.Click();

        // Search for popup option elements (li, div, [role='option'])
        var optionXPaths = new[]
        {
            $"//*[contains(@role, 'option') and contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{desiredOption.ToLowerInvariant()}')]",
            $"//li[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{desiredOption.ToLowerInvariant()}')]",
            $"//div[contains(@class, 'option') and contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{desiredOption.ToLowerInvariant()}')]"
        };

        foreach (var xpath in optionXPaths)
        {
            var matches = driver.FindElements(By.XPath(xpath));
            var visible = matches.FirstOrDefault(m => m.Displayed);
            if (visible != null)
            {
                visible.Click();
                return new ActionResult(true, $"Selected custom dropdown option '{desiredOption}'.");
            }
        }

        return new ActionResult(false, $"Could not find matching option '{desiredOption}' in custom dropdown popup.");
    }

    private static void DispatchChangeEvent(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            var script = @"
                const el = arguments[0];
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            ";
            try { js.ExecuteScript(script, element); } catch { }
        }
    }

    private static void ScrollIntoView(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            try { js.ExecuteScript("arguments[0].scrollIntoView({ behavior: 'smooth', block: 'center' });", element); } catch { }
        }
    }
}

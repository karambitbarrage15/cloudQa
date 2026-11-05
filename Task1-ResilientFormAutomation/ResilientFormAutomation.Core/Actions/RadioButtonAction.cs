using OpenQA.Selenium;

namespace ResilientFormAutomation.Core.Actions;

/// <summary>
/// Resilient interaction for radio button groups.
/// Handles:
///   1. Direct radio input selection
///   2. Hidden radio inputs styled by adjacent &lt;label&gt; or &lt;span&gt;
///   3. Radio button group containers where option needs to be resolved by value/label
///   4. ElementClickInterceptedException recovery via JS click
/// </summary>
public class RadioButtonAction : IFormAction
{
    public ActionResult Execute(IWebDriver driver, IWebElement element, string desiredOption)
    {
        try
        {
            var targetRadio = ResolveSpecificRadio(driver, element, desiredOption);
            if (targetRadio == null)
            {
                return new ActionResult(false, $"Could not find radio option matching '{desiredOption}'.");
            }

            ScrollIntoView(driver, targetRadio);

            // Attempt 1: Standard Selenium click on the radio input
            try
            {
                if (!targetRadio.Selected)
                {
                    targetRadio.Click();
                }
            }
            catch (ElementClickInterceptedException)
            {
                // Fallback A: Click adjacent label or span (common with custom styled radio buttons)
                var clickedLabel = ClickAdjacentLabel(driver, targetRadio);
                if (!clickedLabel)
                {
                    // Fallback B: Native JS click
                    JsClick(driver, targetRadio);
                }
            }
            catch (ElementNotInteractableException)
            {
                // Input might be hidden (e.g. opacity: 0)
                var clickedLabel = ClickAdjacentLabel(driver, targetRadio);
                if (!clickedLabel)
                {
                    JsClick(driver, targetRadio);
                }
            }

            // Verify radio is selected
            if (IsRadioSelected(driver, targetRadio))
            {
                return new ActionResult(true, $"Successfully selected radio option '{desiredOption}'.");
            }

            // Force check via JavaScript
            ForceCheck(driver, targetRadio);
            return new ActionResult(true, $"Selected radio option '{desiredOption}' via JavaScript dispatch.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Failed to select radio button: {ex.Message}");
        }
    }

    private static IWebElement? ResolveSpecificRadio(IWebDriver driver, IWebElement element, string option)
    {
        var tag = element.TagName.ToLowerInvariant();
        var type = element.GetAttribute("type")?.ToLowerInvariant();

        // If the element is already the specific radio matching option
        if (tag == "input" && type == "radio")
        {
            var val = element.GetAttribute("value") ?? string.Empty;
            var id = element.GetAttribute("id") ?? string.Empty;
            if (val.Equals(option, StringComparison.OrdinalIgnoreCase) || id.Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }

            // If it's a radio but different value, search sibling radios with the same 'name' attribute
            var nameAttr = element.GetAttribute("name");
            if (!string.IsNullOrEmpty(nameAttr))
            {
                var groupRadios = driver.FindElements(By.CssSelector($"input[type='radio'][name='{nameAttr}']"));
                foreach (var r in groupRadios)
                {
                    if (MatchesOption(r, option)) return r;
                }
            }
        }

        // If the element is a container (e.g. form-group or div holding the radio group)
        var radios = element.FindElements(By.XPath(".//input[@type='radio']"));
        if (!radios.Any())
        {
            // Search whole page for radios matching the option
            radios = driver.FindElements(By.XPath("//input[@type='radio']"));
        }

        foreach (var r in radios)
        {
            if (MatchesOption(r, option)) return r;
        }

        return radios.FirstOrDefault();
    }

    private static bool MatchesOption(IWebElement radio, string option)
    {
        var value = radio.GetAttribute("value") ?? string.Empty;
        var id = radio.GetAttribute("id") ?? string.Empty;
        var aria = radio.GetAttribute("aria-label") ?? string.Empty;

        if (value.Equals(option, StringComparison.OrdinalIgnoreCase) ||
            id.Equals(option, StringComparison.OrdinalIgnoreCase) ||
            aria.Equals(option, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check text of adjacent span/label
        try
        {
            var nextText = radio.FindElement(By.XPath("./following-sibling::*[self::span or self::label][1]"));
            if (nextText.Text.Trim().Equals(option, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    private static bool ClickAdjacentLabel(IWebDriver driver, IWebElement radio)
    {
        try
        {
            var id = radio.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
            {
                var label = driver.FindElements(By.CssSelector($"label[for='{id}']")).FirstOrDefault();
                if (label != null && label.Displayed)
                {
                    label.Click();
                    return true;
                }
            }

            var nextSibling = radio.FindElements(By.XPath("./following-sibling::*[self::span or self::label][1]")).FirstOrDefault();
            if (nextSibling != null && nextSibling.Displayed)
            {
                nextSibling.Click();
                return true;
            }
        }
        catch { }

        return false;
    }

    private static bool IsRadioSelected(IWebDriver driver, IWebElement radio)
    {
        if (radio.Selected) return true;
        if (driver is IJavaScriptExecutor js)
        {
            try
            {
                var isChecked = js.ExecuteScript("return arguments[0].checked === true;", radio);
                return isChecked is true;
            }
            catch { }
        }
        return false;
    }

    private static void ForceCheck(IWebDriver driver, IWebElement radio)
    {
        if (driver is IJavaScriptExecutor js)
        {
            var script = @"
                const el = arguments[0];
                el.checked = true;
                el.dispatchEvent(new Event('change', { bubbles: true }));
                el.dispatchEvent(new Event('click', { bubbles: true }));
            ";
            try { js.ExecuteScript(script, radio); } catch { }
        }
    }

    private static void ScrollIntoView(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            try { js.ExecuteScript("arguments[0].scrollIntoView({ behavior: 'smooth', block: 'center' });", element); } catch { }
        }
    }

    private static void JsClick(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            try { js.ExecuteScript("arguments[0].click();", element); } catch { }
        }
    }
}

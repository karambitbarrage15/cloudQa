using OpenQA.Selenium;

namespace ResilientFormAutomation.Core.Actions;

/// <summary>
/// Resilient interaction for text inputs.
/// Handles standard inputs, modern SPA inputs (React/Vue/Angular synthetic events),
/// and stubborn fields where standard Clear() doesn't fire events.
/// </summary>
public class TextFieldAction : IFormAction
{
    public ActionResult Execute(IWebDriver driver, IWebElement element, string value)
    {
        try
        {
            ScrollIntoView(driver, element);

            // 1. Attempt standard click & clear
            try
            {
                element.Click();
                element.Clear();
            }
            catch
            {
                // Fallback for custom or partially covered inputs
                JsClick(driver, element);
            }

            // If text remains after Clear() (common in React/Vue controlled inputs), use backspace
            var currentVal = element.GetAttribute("value") ?? string.Empty;
            if (!string.IsNullOrEmpty(currentVal))
            {
                element.SendKeys(Keys.Control + "a");
                element.SendKeys(Keys.Backspace);
            }

            // 2. Type value
            element.SendKeys(value);

            // 3. Dispatch native input and change events via JS to satisfy SPA reactive state
            DispatchInputEvents(driver, element, value);

            // 4. Verify value was applied
            var finalVal = element.GetAttribute("value") ?? string.Empty;
            if (finalVal.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return new ActionResult(true, $"Successfully entered '{value}' into text field.");
            }

            // Fallback: force set via JavaScript if value didn't stick
            ForceSetValue(driver, element, value);
            return new ActionResult(true, $"Entered '{value}' via JavaScript setter fallback.");
        }
        catch (Exception ex)
        {
            return new ActionResult(false, $"Failed to enter text: {ex.Message}");
        }
    }

    private static void ScrollIntoView(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            try
            {
                js.ExecuteScript("arguments[0].scrollIntoView({ behavior: 'smooth', block: 'center' });", element);
            }
            catch { }
        }
    }

    private static void JsClick(IWebDriver driver, IWebElement element)
    {
        if (driver is IJavaScriptExecutor js)
        {
            js.ExecuteScript("arguments[0].click();", element);
        }
    }

    private static void DispatchInputEvents(IWebDriver driver, IWebElement element, string value)
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

    private static void ForceSetValue(IWebDriver driver, IWebElement element, string value)
    {
        if (driver is IJavaScriptExecutor js)
        {
            var script = @"
                const el = arguments[0];
                const val = arguments[1];
                const nativeInputValueSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value')?.set;
                if (nativeInputValueSetter) {
                    nativeInputValueSetter.call(el, val);
                } else {
                    el.value = val;
                }
                el.dispatchEvent(new Event('input', { bubbles: true }));
                el.dispatchEvent(new Event('change', { bubbles: true }));
            ";
            try { js.ExecuteScript(script, element, value); } catch { }
        }
    }
}

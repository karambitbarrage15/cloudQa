using OpenQA.Selenium;

namespace ResilientFormAutomation.Core.Actions;

public record ActionResult(bool Success, string Message, string? StrategyUsed = null);

public interface IFormAction
{
    ActionResult Execute(IWebDriver driver, IWebElement element, string value);
}

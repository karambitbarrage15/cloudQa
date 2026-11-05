using OpenQA.Selenium;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Core.Locators;

/// <summary>
/// Strategy contract for locating elements resiliently.
/// </summary>
public interface ISmartLocatorStrategy
{
    string Name { get; }
    LocatorResult FindElement(IWebDriver driver, FieldDescriptor descriptor);
}

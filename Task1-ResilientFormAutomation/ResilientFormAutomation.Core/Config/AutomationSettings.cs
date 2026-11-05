namespace ResilientFormAutomation.Core.Config;

/// <summary>
/// Configuration settings for the resilient automation suite.
/// Supports loading from appsettings.json and CLI override.
/// </summary>
public class AutomationSettings
{
    public string PageUrl { get; set; } = "https://app.cloudqa.io/home/AutomationPracticeForm";
    public bool Headless { get; set; } = false;
    public int TimeoutSeconds { get; set; } = 15;
    public bool HighlightElements { get; set; } = true;
    public int ActionDelayMilliseconds { get; set; } = 300;
    public FormTestData TestData { get; set; } = new();
}

public class FormTestData
{
    public string FirstName { get; set; } = "Jane";
    public string Gender { get; set; } = "Female";
    public string State { get; set; } = "Australia";
}

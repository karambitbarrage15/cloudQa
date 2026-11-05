using NUnit.Framework;
using ResilientFormAutomation.Core;
using ResilientFormAutomation.Core.Config;

namespace ResilientFormAutomation.Tests;

[TestFixture]
public class ResilientFormTests
{
    private static string GetFixtureUrl(string htmlFileName)
    {
        var fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", htmlFileName);
        Assert.That(File.Exists(fixturePath), Is.True, $"Fixture file not found at: {fixturePath}");
        var uri = new Uri(fixturePath);
        return uri.AbsoluteUri;
    }

    [Test]
    public void Test01_LiveCloudQAPracticeForm_ShouldAutomateSuccessfully()
    {
        var settings = new AutomationSettings
        {
            PageUrl = "https://app.cloudqa.io/home/AutomationPracticeForm",
            Headless = true,
            TimeoutSeconds = 15,
            TestData = new FormTestData
            {
                FirstName = "Jane",
                Gender = "Female",
                State = "Australia"
            }
        };

        var automator = new FormAutomator(settings);
        var report = automator.Run();

        Assert.That(report.OverallSuccess, Is.True, $"Automation failed: {report.ErrorMessage}");
        Assert.That(report.Fields.Count, Is.EqualTo(3));
        Assert.That(report.Fields.All(f => f.Success), Is.True);
    }

    [Test]
    public void Test02_MutatedPage1_RandomizedIdsAndClasses_ShouldPass()
    {
        var url = GetFixtureUrl("MutatedPage1_AttributeMutation.html");
        var settings = new AutomationSettings
        {
            PageUrl = url,
            Headless = true,
            TimeoutSeconds = 5,
            TestData = new FormTestData
            {
                FirstName = "Arthur Dent",
                Gender = "Female",
                State = "Australia"
            }
        };

        var automator = new FormAutomator(settings);
        var report = automator.Run();

        Assert.That(report.OverallSuccess, Is.True, $"Mutated Page 1 failed: {report.ErrorMessage}");
        Assert.That(report.Fields[0].Success, Is.True);
        Assert.That(report.Fields[1].Success, Is.True);
        Assert.That(report.Fields[2].Success, Is.True);
    }

    [Test]
    public void Test03_MutatedPage2_ReorderedAndNestedLabels_ShouldPass()
    {
        var url = GetFixtureUrl("MutatedPage2_ReorderedAndStructuralChanges.html");
        var settings = new AutomationSettings
        {
            PageUrl = url,
            Headless = true,
            TimeoutSeconds = 5,
            TestData = new FormTestData
            {
                FirstName = "Ada Lovelace",
                Gender = "Female",
                State = "Australia"
            }
        };

        var automator = new FormAutomator(settings);
        var report = automator.Run();

        Assert.That(report.OverallSuccess, Is.True, $"Mutated Page 2 failed: {report.ErrorMessage}");
        Assert.That(report.Fields[0].Success, Is.True);
        Assert.That(report.Fields[1].Success, Is.True);
        Assert.That(report.Fields[2].Success, Is.True);
    }

    [Test]
    public void Test04_MutatedPage3_SimilarDistractorFields_ShouldTargetCorrectFields()
    {
        var url = GetFixtureUrl("MutatedPage3_SimilarDistractorFields.html");
        var settings = new AutomationSettings
        {
            PageUrl = url,
            Headless = true,
            TimeoutSeconds = 5,
            TestData = new FormTestData
            {
                FirstName = "Grace Hopper",
                Gender = "Female",
                State = "Australia"
            }
        };

        var automator = new FormAutomator(settings);
        var report = automator.Run();

        Assert.That(report.OverallSuccess, Is.True, $"Mutated Page 3 failed: {report.ErrorMessage}");
        Assert.That(report.Fields[0].Success, Is.True);
        Assert.That(report.Fields[1].Success, Is.True);
        Assert.That(report.Fields[2].Success, Is.True);
    }

    [Test]
    public void Test05_MutatedPage4_AccessibleAttributesWithoutLabels_ShouldPass()
    {
        var url = GetFixtureUrl("MutatedPage4_NoLabelsAttributeOnly.html");
        var settings = new AutomationSettings
        {
            PageUrl = url,
            Headless = true,
            TimeoutSeconds = 5,
            TestData = new FormTestData
            {
                FirstName = "Alan Turing",
                Gender = "Male",
                State = "Australia"
            }
        };

        var automator = new FormAutomator(settings);
        var report = automator.Run();

        Assert.That(report.OverallSuccess, Is.True, $"Mutated Page 4 failed: {report.ErrorMessage}");
        Assert.That(report.Fields[0].Success, Is.True);
        Assert.That(report.Fields[1].Success, Is.True);
        Assert.That(report.Fields[2].Success, Is.True);
    }
}

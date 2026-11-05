namespace ResilientFormAutomation.Core.Config;

/// <summary>
/// Type of input control to interact with.
/// </summary>
public enum ControlType
{
    TextInput,
    RadioButton,
    Dropdown
}

/// <summary>
/// Semantic descriptor for a target form field.
/// Allows the automation to locate fields based on meaning, labels, and synonyms
/// rather than rigid IDs or fixed DOM paths.
/// </summary>
public class FieldDescriptor
{
    public string CanonicalName { get; set; } = string.Empty;
    public ControlType Type { get; set; }
    public List<string> Synonyms { get; set; } = new();
    public List<string> PossibleAttributeSubstrings { get; set; } = new();
    public string? ExpectedValue { get; set; }

    public static FieldDescriptor FirstName(string? valueToEnter = "Jane") => new()
    {
        CanonicalName = "First Name",
        Type = ControlType.TextInput,
        Synonyms = new List<string> { "First Name", "First name", "first name", "Given Name", "Forename" },
        PossibleAttributeSubstrings = new List<string> { "fname", "firstname", "first_name", "first-name" },
        ExpectedValue = valueToEnter
    };

    public static FieldDescriptor Gender(string? optionToSelect = "Female") => new()
    {
        CanonicalName = "Gender",
        Type = ControlType.RadioButton,
        Synonyms = new List<string> { "Gender", "Sex" },
        PossibleAttributeSubstrings = new List<string> { "gender", "sex" },
        ExpectedValue = optionToSelect
    };

    public static FieldDescriptor State(string? optionToSelect = "Australia") => new()
    {
        CanonicalName = "State",
        Type = ControlType.Dropdown,
        Synonyms = new List<string> { "State", "Country", "State / Province", "Region", "Province" },
        PossibleAttributeSubstrings = new List<string> { "state", "country", "province", "region" },
        ExpectedValue = optionToSelect
    };
}

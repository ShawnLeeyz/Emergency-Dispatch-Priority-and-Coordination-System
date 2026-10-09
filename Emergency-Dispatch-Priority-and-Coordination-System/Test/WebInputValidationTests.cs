using System.ComponentModel.DataAnnotations;
using DispatchWeb.Pages.Cases;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Test;

/// <summary>Checks the validation rules used by the real case-creation Razor Page.</summary>
[TestClass]
public sealed class WebInputValidationTests
{
    [TestMethod]
    [TestCategory("Invalid input")]
    public void CreateCaseInput_MissingRequiredValues_ReturnsUserFacingValidationMessages()
    {
        var input = ValidInput();
        input.CallerName = string.Empty;
        input.IncidentType = string.Empty;
        input.Description = string.Empty;
        input.Location = string.Empty;
        input.RequiredUnitTypes = [];

        var results = Validate(input);

        Assert.IsGreaterThanOrEqualTo(5, results.Count);
        Assert.IsTrue(results.Any(item => item.MemberNames.Contains(nameof(input.CallerName))));
        Assert.IsTrue(results.Any(item => item.MemberNames.Contains(nameof(input.IncidentType))));
        Assert.IsTrue(results.Any(item => item.MemberNames.Contains(nameof(input.Description))));
        Assert.IsTrue(results.Any(item => item.MemberNames.Contains(nameof(input.Location))));
        Assert.IsTrue(results.Any(item =>
            item.ErrorMessage == "Select at least one response department."));
        Assert.IsFalse(results.Any(item =>
            item.ErrorMessage?.Contains("Exception", StringComparison.OrdinalIgnoreCase) == true ||
            item.ErrorMessage?.Contains("stack trace", StringComparison.OrdinalIgnoreCase) == true));
    }

    [TestMethod]
    [TestCategory("Boundary")]
    [DataRow(-90d, -180d)]
    [DataRow(90d, 180d)]
    public void CreateCaseInput_CoordinateBoundaries_AreAccepted(double latitude, double longitude)
    {
        var input = ValidInput();
        input.Latitude = latitude;
        input.Longitude = longitude;

        var results = Validate(input);

        Assert.IsEmpty(results);
    }

    [TestMethod]
    [TestCategory("Invalid input")]
    [DataRow(-91d, 0d)]
    [DataRow(91d, 0d)]
    [DataRow(0d, -181d)]
    [DataRow(0d, 181d)]
    public void CreateCaseInput_CoordinatesOutsideBoundaries_AreRejected(double latitude, double longitude)
    {
        var input = ValidInput();
        input.Latitude = latitude;
        input.Longitude = longitude;

        var results = Validate(input);

        Assert.HasCount(1, results);
        Assert.IsTrue(results[0].MemberNames.Contains(
            latitude is < -90 or > 90 ? nameof(input.Latitude) : nameof(input.Longitude)));
    }

    private static CreateModel.InputModel ValidInput() => new()
    {
        CallerName = "Validation Caller",
        CallerPhone = "021 555 0199",
        IncidentType = "Medical assistance",
        Description = "A valid test incident description.",
        Location = "1 Test Street",
        Latitude = -36.8485,
        Longitude = 174.7633,
        Severity = Severity.Medium,
        RequiredUnitTypes = [ResponseUnitType.Medical]
    };

    private static List<ValidationResult> Validate(CreateModel.InputModel input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }
}

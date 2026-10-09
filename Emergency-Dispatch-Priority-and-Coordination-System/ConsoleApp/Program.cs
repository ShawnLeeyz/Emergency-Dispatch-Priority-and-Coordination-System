using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

// Runs one small dispatch example without starting the website.
var dispatchCase = new Case("Demo caller", "0210000000", "Medical emergency", "Person found unconscious", "123 Main Street", Severity.High, [ResponseUnitType.Medical]);
var priority = new KeywordSeverityPriority().Calculate(dispatchCase);
var unit = new Unit("MED-DEMO", ResponseUnitType.Medical, "Central Hospital", 2);
var assignmentService = new UnitAssignmentService();
// Select the closest unit, calculate its distance and assign it to the case.
var selectedUnit = assignmentService.SelectClosestAvailable([unit], dispatchCase);
var assigned = selectedUnit is not null && dispatchCase.Assign(
    selectedUnit, assignmentService.CalculateDistanceKilometres(dispatchCase, selectedUnit));

Console.WriteLine($"{dispatchCase.CaseNumber}: {priority}; assigned={assigned}; case status={dispatchCase.Status}");

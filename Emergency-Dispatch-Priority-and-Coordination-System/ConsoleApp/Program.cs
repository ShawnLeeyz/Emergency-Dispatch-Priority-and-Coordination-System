using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

var dispatchCase = new Case("Demo caller", "0210000000", "Medical emergency", "Person found unconscious", "123 Main Street", Severity.High, [ResponseUnitType.Medical]);
var priority = new KeywordSeverityPriority().Calculate(dispatchCase);
var unit = new Unit("MED-DEMO", ResponseUnitType.Medical, "Central Hospital", 2);
var assigned = new UnitAssignmentService().TryAssign(unit, dispatchCase);

Console.WriteLine($"{dispatchCase.CaseNumber}: {priority}; assigned={assigned}; case status={dispatchCase.Status}");

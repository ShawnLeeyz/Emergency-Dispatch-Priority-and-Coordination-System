using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

Console.WriteLine("========================================");
Console.WriteLine("Testing UnitAssignmentService and KeywordSeverityPriority");
Console.WriteLine("========================================\n");

// Test 1: KeywordSeverityPriority - High Priority
Console.WriteLine("Test 1: KeywordSeverityPriority - High Priority Keywords");
var keywordService = new KeywordSeverityPriority();
var highPriorityCase = new Case(
    callerId: 1,
    notice: "Emergency - Unconscious person",
    description: "Person found unconscious at downtown street. Requires immediate medical attention.",
    location: "123 Main Street",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.High,
    priorityType: Case.PriorityType.Low
);
var priority = keywordService.Calculate(highPriorityCase);
Console.WriteLine($"Description: {highPriorityCase.description}");
Console.WriteLine($"Calculated Priority: {priority}");
Console.WriteLine($"Expected: High | Result: {(priority == Case.PriorityType.High ? "✓ PASS" : "✗ FAIL")}\n");

// Test 2: KeywordSeverityPriority - Medium Priority
Console.WriteLine("Test 2: KeywordSeverityPriority - Medium Priority Keywords");
var mediumPriorityCase = new Case(
    callerId: 2,
    notice: "Injury Report",
    description: "Multiple people injured in a vehicle accident on Highway 5. Ambulance needed.",
    location: "Highway 5, Mile 42",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.Medium,
    priorityType: Case.PriorityType.Low
);
priority = keywordService.Calculate(mediumPriorityCase);
Console.WriteLine($"Description: {mediumPriorityCase.description}");
Console.WriteLine($"Calculated Priority: {priority}");
Console.WriteLine($"Expected: Medium | Result: {(priority == Case.PriorityType.Medium ? "✓ PASS" : "✗ FAIL")}\n");

// Test 3: KeywordSeverityPriority - Low Priority
Console.WriteLine("Test 3: KeywordSeverityPriority - Low Priority (No Keywords)");
var lowPriorityCase = new Case(
    callerId: 3,
    notice: "Break-in Report",
    description: "Possible break-in at residential home. Motion detected by security system.",
    location: "456 Oak Avenue",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.Low,
    priorityType: Case.PriorityType.Low
);
priority = keywordService.Calculate(lowPriorityCase);
Console.WriteLine($"Description: {lowPriorityCase.description}");
Console.WriteLine($"Calculated Priority: {priority}");
Console.WriteLine($"Expected: Low | Result: {(priority == Case.PriorityType.Low ? "✓ PASS" : "✗ FAIL")}\n");

// Test 4: UnitAssignmentService - Successful Assignment
Console.WriteLine("Test 4: UnitAssignmentService - Successful Unit Assignment");
var unitAssignmentService = new UnitAssignmentService();
var ambulance = new Unit(Unit.UnitType.ambulance, "Central Hospital", 2);
var caseForAssignment = new Case(
    callerId: 4,
    notice: "Medical Emergency",
    description: "Patient with severe chest pain requires immediate transport",
    location: "Downtown Medical Clinic",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.High,
    priorityType: Case.PriorityType.High
);

Console.WriteLine($"Before Assignment:");
Console.WriteLine($"  Unit Status: {ambulance.status}");
Console.WriteLine($"  Case Status: {caseForAssignment.status}");

bool assigned = unitAssignmentService.TryAssign(ambulance, caseForAssignment);
Console.WriteLine($"\nTryAssign Result: {assigned}");
Console.WriteLine($"After Assignment:");
Console.WriteLine($"  Unit Status: {ambulance.status}");
Console.WriteLine($"  Case Status: {caseForAssignment.status}");
Console.WriteLine($"  Expected: Both true/Assigned/InProgress | Result: {(assigned && ambulance.status == Unit.UnitStatus.Assigned && caseForAssignment.status == Case.StatusType.InProgress ? "✓ PASS" : "✗ FAIL")}\n");

// Test 5: UnitAssignmentService - Failed Assignment (Already Assigned)
Console.WriteLine("Test 5: UnitAssignmentService - Failed Assignment (Unit Already Assigned)");
var police = new Unit(Unit.UnitType.police, "Central Station", 2);
var caseForAssignment2 = new Case(
    callerId: 5,
    notice: "Crime in Progress",
    description: "Robbery at convenience store with weapon. Police unit needed immediately.",
    location: "7-Eleven, 789 Elm Street",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.High,
    priorityType: Case.PriorityType.High
);

// First assignment
unitAssignmentService.TryAssign(police, caseForAssignment2);
Console.WriteLine($"First assignment: Unit status is now {police.status}");

// Try to assign again (should fail)
var caseForAssignment3 = new Case(
    callerId: 6,
    notice: "Another Crime",
    description: "Another incident reported",
    location: "Another Location",
    status: Case.StatusType.Open,
    severityType: Case.SeverityType.Medium,
    priorityType: Case.PriorityType.Medium
);

bool secondAssigned = unitAssignmentService.TryAssign(police, caseForAssignment3);
Console.WriteLine($"Second assignment attempt: {secondAssigned}");
Console.WriteLine($"Expected: false | Result: {(secondAssigned == false ? "✓ PASS" : "✗ FAIL")}\n");

Console.WriteLine("========================================");
Console.WriteLine("All tests completed!");
Console.WriteLine("========================================");


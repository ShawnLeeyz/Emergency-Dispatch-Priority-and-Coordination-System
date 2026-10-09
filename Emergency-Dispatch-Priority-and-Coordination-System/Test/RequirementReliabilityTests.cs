using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

/// <summary>Verifies final non-functional requirements that need database-backed evidence.</summary>
[TestClass]
public sealed class RequirementReliabilityTests
{
    [TestMethod]
    [TestCategory("Reliability")]
    public void NFR_R04_OneHundredOpenCases_RemainAvailableAndUpdatableAcrossRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange and act: submit 100 cases without releasing the limited Police units.
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var firstService = CreateService(firstDatabase);
            for (var index = 1; index <= 100; index++)
            {
                firstService.CreateAndDispatch(new CreateCaseRequest(
                    $"Reliability Caller {index}", $"021 700 {index:0000}", "Police assistance",
                    $"Reliability case {index}", $"{index} Test Street", Severity.Medium,
                    [ResponseUnitType.Police], -36.8485 + index / 100_000d, 174.7633), "load-test");
            }

            var submitted = new SqliteCaseRepository(firstDatabase).GetAll().ToArray();

            // Reopen the database to prove the collection remains displayable after restart.
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var reopenedCases = new SqliteCaseRepository(reopenedDatabase);
            var restored = reopenedCases.GetAll().ToArray();
            var activeCountBeforeSignOff = restored.Count(item => item.Status == CaseStatus.InProgress);

            Assert.HasCount(100, submitted);
            Assert.HasCount(100, restored);
            Assert.IsTrue(restored.All(item => item.Status is CaseStatus.Open or CaseStatus.InProgress));
            Assert.IsGreaterThan(0, activeCountBeforeSignOff);

            // Update the workload by signing off one assigned unit. The released unit should
            // immediately move to a compatible waiting case without losing any case records.
            var activeCase = restored.First(item => item.Status == CaseStatus.InProgress);
            var activeUnit = activeCase.AssignedUnits.Single();
            CreateService(reopenedDatabase).SignOffUnit(
                activeCase.Id, activeUnit.Id, ResponseUnitType.Police, "pol01");
            var updated = reopenedCases.GetAll().ToArray();

            // Assert
            Assert.HasCount(100, updated);
            Assert.HasCount(1, updated.Where(item => item.Status == CaseStatus.Closed));
            Assert.HasCount(activeCountBeforeSignOff,
                updated.Where(item => item.Status == CaseStatus.InProgress));
            Assert.HasCount(99 - activeCountBeforeSignOff,
                updated.Where(item => item.Status == CaseStatus.Open));
            Assert.IsTrue(updated.All(item => !string.IsNullOrWhiteSpace(item.CallerName)));
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static DispatchService CreateService(SqliteDatabase database) =>
        new(new SqliteCaseRepository(database), new SqliteDepartmentRepository(database),
            new KeywordSeverityPriority(), new SqliteDispatchNotifier(database),
            new SqliteAuditRepository(database), new UnitAssignmentService());

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-reliability-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}

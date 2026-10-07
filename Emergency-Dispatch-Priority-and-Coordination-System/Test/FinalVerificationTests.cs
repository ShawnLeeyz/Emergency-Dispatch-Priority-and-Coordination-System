using System.Diagnostics;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Test;

[TestClass]
public sealed class FinalVerificationTests
{
    [TestMethod]
    [TestCategory("Database performance")]
    public void DatabaseBackedMultiDepartmentDispatch_CompletesWithinTwoSeconds()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange - database creation is outside the measurement because it happens at application start.
            var database = new SqliteDatabase($"Data Source={databasePath}");
            var service = CreateService(database);
            var request = new CreateCaseRequest(
                "Performance Caller", "021 555 0160", "Building fire",
                "Fire with possible injuries and police assistance required.", "20 Queen Street",
                Severity.High, [ResponseUnitType.Fire, ResponseUnitType.Medical, ResponseUnitType.Police],
                -36.8500, 174.7650);
            var timer = Stopwatch.StartNew();

            // Act
            var dispatchCase = service.CreateAndDispatch(request, "dispatch01");
            timer.Stop();

            // Assert
            Assert.IsLessThan(TimeSpan.FromSeconds(2), timer.Elapsed,
                $"Database-backed dispatch took {timer.Elapsed.TotalMilliseconds:N0} ms.");
            Assert.HasCount(3, dispatchCase.Assignments);
            Assert.AreEqual(CaseStatus.InProgress, dispatchCase.Status);
            Assert.HasCount(3, new SqliteDispatchNotifier(database).GetAll());
            Assert.HasCount(4, new SqliteAuditRepository(database).GetAll());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void FullDatabaseLifecycle_RemainsCorrectAcrossMultipleRestarts()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var firstService = CreateService(firstDatabase);

            // Act - create, restart, partially sign off, restart, then complete the case.
            var created = firstService.CreateAndDispatch(new CreateCaseRequest(
                "Lifecycle Caller", "021 555 0170", "Collision",
                "Collision with injuries", "30 Queen Street", Severity.High,
                [ResponseUnitType.Police, ResponseUnitType.Medical], -36.8510, 174.7640), "dispatch02");

            var secondDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var secondCases = new SqliteCaseRepository(secondDatabase);
            var afterCreate = secondCases.Get(created.Id)!;
            var police = afterCreate.Assignments.Single(item => item.Unit.Type == ResponseUnitType.Police).Unit;
            CreateService(secondDatabase).SignOffUnit(afterCreate.Id, police.Id, police.Type, "pol01");

            var thirdDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var thirdCases = new SqliteCaseRepository(thirdDatabase);
            var afterPartialSignOff = thirdCases.Get(created.Id)!;
            var medical = afterPartialSignOff.Assignments.Single(item => item.Unit.Type == ResponseUnitType.Medical).Unit;
            CreateService(thirdDatabase).SignOffUnit(afterPartialSignOff.Id, medical.Id, medical.Type, "med01");

            var finalDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var completed = new SqliteCaseRepository(finalDatabase).Get(created.Id)!;

            // Assert
            Assert.AreEqual(CaseStatus.Closed, completed.Status);
            Assert.HasCount(2, completed.Assignments);
            Assert.IsTrue(completed.Assignments.All(item => item.SignedOffAt.HasValue));
            Assert.IsTrue(completed.Assignments.All(item => item.DistanceKilometres > 0));
            Assert.IsTrue(completed.Assignments.All(item => item.Unit.Availability == UnitAvailability.Available));
            Assert.IsTrue(completed.Assignments.All(item => item.Unit.AssignedCaseId is null));

            var events = new SqliteAuditRepository(finalDatabase).GetAll()
                .Where(item => item.CaseId == created.Id).ToArray();
            Assert.HasCount(6, events);
            Assert.HasCount(2, events.Where(item => item.EventType == AuditEventTypes.UnitAssigned));
            Assert.HasCount(2, events.Where(item => item.EventType == AuditEventTypes.UnitSignedOff));
            Assert.HasCount(1, events.Where(item => item.EventType == AuditEventTypes.CaseClosed));
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
        Path.Combine(Path.GetTempPath(), $"dispatch-final-verification-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}

using DispatchWeb.Authentication;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;
using Microsoft.Data.Sqlite;

namespace Test;

[TestClass]
public sealed class SecurityTests
{
    [TestMethod]
    public void PasswordHasher_UsesDifferentSaltAndVerifiesOnlyTheCorrectPassword()
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act
        var first = hasher.Create("test-password");
        var second = hasher.Create("test-password");

        // Assert
        Assert.AreNotEqual("test-password", first.Hash);
        Assert.AreNotEqual(first.Salt, second.Salt);
        Assert.AreNotEqual(first.Hash, second.Hash);
        Assert.IsTrue(hasher.Verify("test-password", first.Hash, first.Salt, first.Iterations));
        Assert.IsFalse(hasher.Verify("wrong-password", first.Hash, first.Salt, first.Iterations));
    }

    [TestMethod]
    public void StoredAccount_CanAuthenticateAfterDatabaseRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var firstDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var firstStore = new DemoAccountStore(
                new SqliteUserAccountRepository(firstDatabase), new PasswordHasher());

            // Act
            var firstLogin = firstStore.Validate("dispatch01", "dispatch-demo");
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var reopenedStore = new DemoAccountStore(
                new SqliteUserAccountRepository(reopenedDatabase), new PasswordHasher());
            var secondLogin = reopenedStore.Validate("dispatch01", "dispatch-demo");
            var rejectedLogin = reopenedStore.Validate("dispatch01", "incorrect");

            // Assert
            Assert.IsNotNull(firstLogin);
            Assert.IsNotNull(secondLogin);
            Assert.AreEqual(DemoRoles.Dispatcher, secondLogin.Role);
            Assert.IsNull(rejectedLogin);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void Database_StoresPasswordHashInsteadOfPlaintextPassword()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var database = new SqliteDatabase($"Data Source={databasePath}");
            _ = new DemoAccountStore(new SqliteUserAccountRepository(database), new PasswordHasher());

            // Act
            using var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT PasswordHash, PasswordSalt FROM UserAccounts WHERE Username = 'dispatch01';";
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            var storedHash = reader.GetString(0);
            var storedSalt = reader.GetString(1);

            // Assert
            Assert.AreNotEqual("dispatch-demo", storedHash);
            Assert.IsGreaterThan(20, storedHash.Length);
            Assert.IsGreaterThan(10, storedSalt.Length);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [TestMethod]
    public void SensitiveCaseFields_AreEncryptedInSQLiteAndDecryptAfterRestart()
    {
        var databasePath = NewDatabasePath();
        try
        {
            // Arrange
            var database = new SqliteDatabase($"Data Source={databasePath}");
            var cases = new SqliteCaseRepository(database);
            var departments = new SqliteDepartmentRepository(database);
            var service = new DispatchService(cases, departments,
                new KeywordSeverityPriority(), new SqliteDispatchNotifier(database));

            // Act
            var created = service.CreateAndDispatch(new CreateCaseRequest(
                "Private Caller", "021 999 1234", "Medical assistance", "Private medical details",
                "88 Confidential Road", Severity.High, [ResponseUnitType.Medical]));
            var storedCaller = ReadStoredCaller(databasePath, created.Id);
            var reopenedDatabase = new SqliteDatabase($"Data Source={databasePath}");
            var restored = new SqliteCaseRepository(reopenedDatabase).Get(created.Id);

            // Assert
            StringAssert.StartsWith(storedCaller, "ENC1:");
            Assert.AreNotEqual("Private Caller", storedCaller);
            Assert.IsNotNull(restored);
            Assert.AreEqual("Private Caller", restored.CallerName);
            Assert.AreEqual("021 999 1234", restored.CallerPhone);
            Assert.AreEqual("Private medical details", restored.Description);
            Assert.AreEqual("88 Confidential Road", restored.Location);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static string ReadStoredCaller(string databasePath, Guid caseId)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CallerName FROM Cases WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", caseId.ToString());
        return (string)command.ExecuteScalar()!;
    }

    private static string NewDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"dispatch-security-tests-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".key")) File.Delete(path + ".key");
    }
}

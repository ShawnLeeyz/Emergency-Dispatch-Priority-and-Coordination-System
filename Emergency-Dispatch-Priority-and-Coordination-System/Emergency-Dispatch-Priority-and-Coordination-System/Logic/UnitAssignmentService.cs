using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>Selects the closest available unit using straight-line geographic distance.</summary>
public sealed class UnitAssignmentService : IUnitAssignmentService
{
    public Unit? SelectClosestAvailable(IEnumerable<Unit> units, Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(dispatchCase);

        // Unit identifier provides a repeatable result when two units are equally distant.
        return units
            .Where(unit => unit.Availability == UnitAvailability.Available)
            .OrderBy(unit => CalculateDistanceKilometres(dispatchCase, unit))
            .ThenBy(unit => unit.Identifier, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public double CalculateDistanceKilometres(Case dispatchCase, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(dispatchCase);
        ArgumentNullException.ThrowIfNull(unit);

        // Haversine calculates straight-line distance over the earth using latitude and longitude.
        // It is deterministic and needs no external map service, which suits this local prototype.
        const double earthRadiusKilometres = 6371;
        var latitudeDifference = ToRadians(unit.Latitude - dispatchCase.Latitude);
        var longitudeDifference = ToRadians(unit.Longitude - dispatchCase.Longitude);
        var caseLatitude = ToRadians(dispatchCase.Latitude);
        var unitLatitude = ToRadians(unit.Latitude);

        var value = Math.Pow(Math.Sin(latitudeDifference / 2), 2) +
                    Math.Cos(caseLatitude) * Math.Cos(unitLatitude) *
                    Math.Pow(Math.Sin(longitudeDifference / 2), 2);
        value = Math.Clamp(value, 0, 1);
        var angle = 2 * Math.Atan2(Math.Sqrt(value), Math.Sqrt(1 - value));
        return earthRadiusKilometres * angle;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public enum UnitAvailability { Available, Unavailable }

/// <summary>Represents one response unit and controls its availability and current case.</summary>
public sealed class Unit
{
    public Guid Id { get; }
    public string Identifier { get; }
    public ResponseUnitType Type { get; }
    public string Location { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public int PersonnelCount { get; private set; }
    public UnitAvailability Availability { get; private set; } = UnitAvailability.Available;
    public Guid? AssignedCaseId { get; private set; }

    public Unit(string identifier, ResponseUnitType type, string location, int personnelCount,
        double latitude = -36.8485, double longitude = 174.7633)
        : this(Guid.NewGuid(), identifier, type, location, personnelCount,
            UnitAvailability.Available, null, latitude, longitude)
    {
    }

    internal Unit(Guid id, string identifier, ResponseUnitType type, string location, int personnelCount,
        UnitAvailability availability, Guid? assignedCaseId, double latitude, double longitude)
    {
        Id = id;
        Identifier = string.IsNullOrWhiteSpace(identifier) ? throw new ArgumentException("Identifier is required.", nameof(identifier)) : identifier.Trim();
        Type = type;
        Location = string.IsNullOrWhiteSpace(location) ? throw new ArgumentException("Location is required.", nameof(location)) : location.Trim();
        PersonnelCount = personnelCount > 0 ? personnelCount : throw new ArgumentOutOfRangeException(nameof(personnelCount));
        UpdateCoordinates(latitude, longitude);
        Availability = availability;
        AssignedCaseId = assignedCaseId;
    }

    public void UpdateDetails(string location, int personnelCount)
        => UpdateDetails(location, personnelCount, Latitude, Longitude);

    public void UpdateDetails(string location, int personnelCount, double latitude, double longitude)
    {
        // Availability is intentionally not editable; assignments control it.
        Location = string.IsNullOrWhiteSpace(location) ? throw new ArgumentException("Location is required.", nameof(location)) : location.Trim();
        PersonnelCount = personnelCount > 0 ? personnelCount : throw new ArgumentOutOfRangeException(nameof(personnelCount));
        UpdateCoordinates(latitude, longitude);
    }

    internal bool TryAssign(Case dispatchCase)
    {
        // A unit can hold only one active case at a time.
        if (Availability != UnitAvailability.Available) return false;
        Availability = UnitAvailability.Unavailable;
        AssignedCaseId = dispatchCase.Id;
        return true;
    }

    internal void Release()
    {
        // Sign-off returns the unit to the available pool.
        Availability = UnitAvailability.Available;
        AssignedCaseId = null;
    }

    private void UpdateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
    }
}

namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public sealed class Department
{
    private readonly List<Unit> _units = [];
    public ResponseUnitType Type { get; }
    public string Name { get; }
    public IReadOnlyCollection<Unit> Units => _units.AsReadOnly();

    public Department(ResponseUnitType type, string name, IEnumerable<Unit> units)
    {
        Type = type;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Department name is required.", nameof(name)) : name.Trim();
        _units.AddRange(units ?? throw new ArgumentNullException(nameof(units)));
    }
}

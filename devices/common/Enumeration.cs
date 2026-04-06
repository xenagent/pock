using System.Reflection;

namespace Devices.Common;

public abstract class Enumeration : IComparable
{
    public int Id { get; }
    public string Name { get; }

    protected Enumeration(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString() => Name;

    public override bool Equals(object? obj)
        => obj is Enumeration other && Id == other.Id;

    public override int GetHashCode() => Id;

    public int CompareTo(object? other)
        => Id.CompareTo(((Enumeration)other!).Id);

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                 .Select(f => f.GetValue(null))
                 .Cast<T>();
}

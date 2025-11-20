public enum Faction
{
    Player,
    Enemy
}

public interface IFactionOwner
{
    Faction Faction { get; }
}

public static class FactionOwnerExtensions
{
    public static bool IsHostileTo(this IFactionOwner source, IFactionOwner other)
    {
        if (source == null || other == null)
        {
            return false;
        }

        return source.Faction != other.Faction;
    }
}

using System;
using System.Collections.Generic;

[Serializable]
public struct GTag : IEquatable<GTag>
{
    public readonly string Value;

    public GTag(string v) => Value = Normalize(v);

    public bool IsA(GTag other)
    {
        if (string.IsNullOrEmpty(Value) || string.IsNullOrEmpty(other.Value))
            return false;
        return Value == other.Value || Value.StartsWith(other.Value + ".", StringComparison.Ordinal);
    }

    public override string ToString() => Value ?? "";
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    public override bool Equals(object obj) => obj is GTag t && Equals(t);
    public bool Equals(GTag other) => Value == other.Value;

    // Allow: GTag t = "gameplay.projectile";
    public static implicit operator GTag(string s) => new GTag(s);

    // Allow: string s = someGTag;
    public static implicit operator string(GTag t) => t.Value;

    // (Optional) If you like explicit cast syntax too:
    public static explicit operator GTag?(string s) => new GTag(s);

    private static string Normalize(string s) => s?.Trim().ToLowerInvariant();
}

[Serializable]
public class GTagSet
{
    public List<string> Tags = new List<string>();

    public int Count => Tags?.Count ?? 0;
    public IEnumerable<GTag> AsTags()
    {
        foreach (var tag in Tags)
            yield return new GTag(tag);
    }

    public bool Matches(GTag query)
    {
        foreach (var tag in AsTags())
        {
            if (tag.IsA(query))
                return true;
        }
        return false;
    }
}

[Serializable]
public struct GTagQuery
{
    public GTagSet All;
    public GTagSet Any;
    public GTagSet None;

    public bool Matches(IEnumerable<GTag> targetTags)
    {
        // Expand target to list for multiple passes
        var list = targetTags is List<GTag> l ? l : new List<GTag>(targetTags);

        // ALL
        if (All != null)
            foreach (var a in All.AsTags())
            {
                bool hit = false;
                foreach (var t in list) { if (t.IsA(a)) { hit = true; break; } }
                if (!hit) return false;
            }

        // ANY
        if (Any != null && Any.Count > 0)
        {
            bool anyHit = false;
            foreach (var a in Any.AsTags())
            {
                foreach (var t in list) { if (t.IsA(a)) { anyHit = true; break; } }
                if (anyHit) break;
            }
            if (!anyHit) return false;
        }

        // NONE
        if (None != null)
            foreach (var n in None.AsTags())
            {
                foreach (var t in list) { if (t.IsA(n)) return false; }
            }

        return true;
    }
}
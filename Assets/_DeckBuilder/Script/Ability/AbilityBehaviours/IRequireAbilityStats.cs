using System.Collections.Generic;

public interface IRequireAbilityStats
{
    IEnumerable<AbilityStatKey> GetRequiredStatKeys();
}


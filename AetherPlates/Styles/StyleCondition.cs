using FFXIVHudPlugin.AetherPlates.Data;

namespace FFXIVHudPlugin.AetherPlates.Styles;

public enum StyleConditionType
{
    IsBoss = 0,
    IsTarget = 1,
    IsPartyMember = 2,
    IsAllianceMember = 3,
    IsHostile = 4,
    IsFriendly = 5,
    DistanceLessThan = 6,
}

[Serializable]
public sealed class StyleCondition
{
    public StyleConditionType Type { get; set; }
    public float Value { get; set; }

    public bool Matches(NameplateContext context)
    {
        return this.Type switch
        {
            StyleConditionType.IsBoss => context.IsBoss,
            StyleConditionType.IsTarget => context.IsTarget,
            StyleConditionType.IsPartyMember => context.IsPartyMember,
            StyleConditionType.IsAllianceMember => context.IsAllianceMember,
            StyleConditionType.IsHostile => context.IsHostile,
            StyleConditionType.IsFriendly => context.IsFriendly,
            StyleConditionType.DistanceLessThan => context.Distance <= this.Value,
            _ => false,
        };
    }
}

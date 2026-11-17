using System;

namespace Content.Shared._CMU14.Medical.Wounds;

public static class WoundSizeProfile
{
    public static WoundSize FromDamage(float damage)
    {
        if (damage >= 60f)
            return WoundSize.Massive;
        if (damage >= 35f)
            return WoundSize.Gaping;
        if (damage >= 15f)
            return WoundSize.Deep;
        return WoundSize.Small;
    }

    public static TimeSpan BandageDelay(WoundSize size) => size switch
    {
        WoundSize.Small => TimeSpan.FromSeconds(1.0),
        WoundSize.Deep => TimeSpan.FromSeconds(1.0),
        WoundSize.Gaping => TimeSpan.FromSeconds(1.0),
        WoundSize.Massive => TimeSpan.FromSeconds(1.0),
        _ => TimeSpan.FromSeconds(1.0),
    };

    public static int BandagesRequired(WoundSize size) => size switch
    {
        WoundSize.Small => 1,
        WoundSize.Deep => 1,
        WoundSize.Gaping => 1,
        WoundSize.Massive => 1,
        _ => 1,
    };

    public static float BleedMultiplier(WoundSize size) => size switch
    {
        WoundSize.Small => 1.0f,
        WoundSize.Deep => 1.0f,
        WoundSize.Gaping => 1.0f,
        WoundSize.Massive => 1.0f,
        _ => 1.0f,
    };
}

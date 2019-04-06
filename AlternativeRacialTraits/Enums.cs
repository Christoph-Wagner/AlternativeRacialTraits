using System;

namespace AlternativeRacialTraits
{
    [Flags]
    enum BuffFlags
    {
        IsFromSpell = 0x1,
        HiddenInUi = 0x2,
        StayOnDeath = 0x8,
        RemoveOnRest = 0x10,
        RemoveOnResurrect = 0x20,
        Harmful = 0x40
    }

    internal enum CasterSpellProgression
    {
        FullCaster,
        ThreeQuartersCaster,
        HalfCaster,
        UnknownCaster
    }
}
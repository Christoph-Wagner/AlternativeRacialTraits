using System;
using System.Collections.Generic;
using Harmony12;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace AlternativeRacialTraits
{
    [HarmonyPatch(typeof(ApplyClassMechanics), "Apply", typeof(LevelUpState), typeof(UnitDescriptor))]
    static class ApplyClassMechanics_Apply_Patch
    {
        internal static readonly List<Action<LevelUpState, UnitDescriptor>> onChargenApply = new List<Action<LevelUpState, UnitDescriptor>>();

        static ApplyClassMechanics_Apply_Patch() => Main.ApplyPatch(typeof(ApplyClassMechanics_Apply_Patch), "Favored Class and Traits during character creation");

        static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit)
        {
            try
            {
                if (state.NextLevel == 1)
                {
                    foreach (var action in onChargenApply) action(state, unit);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
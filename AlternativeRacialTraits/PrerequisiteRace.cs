using JetBrains.Annotations;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace AlternativeRacialTraits
{
    public class PrerequisiteRace : Prerequisite
    {
        [NotNull]
        public BlueprintRace Race;

        public override bool Check(
            FeatureSelectionState selectionState,
            UnitDescriptor unit,
            LevelUpState state) => unit.Blueprint.Race == Race;

        public override string GetUIText() => $"{Race.Name}";
    }
}
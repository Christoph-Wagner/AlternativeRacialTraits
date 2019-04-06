using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.UnitLogic.FactLogic;

namespace AlternativeRacialTraits
{
    public class RacialTraits
    {
        static LibraryScriptableObject library => Main.library;
        private const string featureGuid = "82002b1f2fc7496184ca254012c77d20";
        private const FeatureGroup featureGroup = FeatureGroup.Racial;
        static bool loaded;

        internal static void Load()
        {
            
            if (loaded) return;
            loaded = true;
            
            var alternateTraitSelection = Helpers.CreateFeatureSelection("alternateRacialTrait", "Alternative Racial Trait",
                "Race traits are keyed to specific races or ethnicities. In order to select a race trait, your character must be of the trait’s race or ethnicity.",
                featureGuid, null, featureGroup);
            
            
            
            
            var halfElfReq = Main.objects.halfElf.PrerequisiteFeature();
            var choices = new List<BlueprintFeature> {CreateNoAlternateTrait()};
            choices.AddRange(GetHalfOrcAlternatives());
            alternateTraitSelection.SetFeatures(choices);
            
            ApplyClassMechanics_Apply_Patch.onChargenApply.Add((state, unit) =>
            {
                alternateTraitSelection.AddSelection(state, unit, 1);
            });
            
            foreach (var race in Main.objects.races)
            {
                foreach (var feature in race.Features)
                {
                    if (feature.AssetGuid != "c99f3405d1ef79049bd90678a666e1d7")
                        continue;
                    Main.Logger.Log($"{feature.GetType()} - {feature.AssetGuid}");
                }
            }
        }
        static BlueprintFeature CreateNoAlternateTrait()
        {
            var feat = Helpers.CreateFeature("KeepNormalRacialTrait",
                "Keep normal racial traits.", "Choose this to skip choosing and alternate racial trait.",
                "dd610d38cff0449cae63fd026813ae96",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"),
                FeatureGroup.None);
            feat.HideInUI = true;
            feat.Ranks = 10;
            return feat;
        }
        private static List<BlueprintFeature> GetHalfOrcAlternatives()
        {
            
            var choices = new List<BlueprintFeature>();
            if (Main.library.BlueprintsByAssetId == null) 
                return choices;
            
            var halfOrcReq = Main.objects.halfOrc.PrerequisiteFeature();
            var halfOrcFerocity = (BlueprintFeature)Main.library.BlueprintsByAssetId["c99f3405d1ef79049bd90678a666e1d7"];
            var halfOrcIntimidating = (BlueprintFeature)Main.library.BlueprintsByAssetId["885f478dff2e39442a0f64ceea6339c9"];
            var orcWeaponFamiliarity = (BlueprintFeature)Main.library.BlueprintsByAssetId["6ab6c271d1558344cbc746350243d17d"];
            choices.Add(Helpers.CreateFeature("BestialTrait", "Bestial",
                "The orc blood of some half-orcs manifests in the form of particularly prominent orc features, exacerbating their bestial appearances but improving their already keen senses. They gain a +2 racial bonus on Perception checks. This racial trait replaces orc ferocity.",
                "1b511ad5f74940be8245aa39347bd0bc",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"), // Iron Will
                featureGroup,
                halfOrcReq,
                StatType.SkillPerception.CreateAddStatBonus(2, ModifierDescriptor.Racial),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcFerocity)));
            
            choices.Add(Helpers.CreateFeature("BurningAssuranceTrait", "Burning Assurance",
                "Half-orcs acquire as a result of prejudice, and their self-confidence puts others at ease. Desert half-orcs with this racial trait gain a +2 racial bonus on Diplomacy checks. This replaces Intimidating.",
                "51f68f1cd4a5456b9bf296d06dc27260",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"), // Iron Will
                featureGroup,
                halfOrcReq,
                StatType.CheckDiplomacy.CreateAddStatBonus(2, ModifierDescriptor.Racial),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcIntimidating)));
            
            
            var LongswordProficiency = (BlueprintFeature)Main.library.BlueprintsByAssetId["62e27ffd9d53e14479f73da29760f64e"];
            var HeavyFlailProficiency = (BlueprintFeature)Main.library.BlueprintsByAssetId["a22e30bd35fbb704cab2d7e3c00717c1"];
            var FlailProficiency = (BlueprintFeature)Main.library.BlueprintsByAssetId["6d273f46bce2e0f47a0958810dc4c7d9"];
            
            // This is supposed to add whip proficiencies, switched to flails as whips are not in the game. 
            choices.Add(Helpers.CreateFeature("CityRaisedTrait", "City-Raised",
                "Half-orcs with this trait know little of their orc ancestry and were raised among humans and other half-orcs in a large city. City-raised half-orcs are proficient with flails and longswords, and receive a +2 racial bonus on Knowledge (World) checks. This racial trait replaces weapon familiarity.",
                "6fe6d4b506c64b30802c66196914cdb5",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"), // Iron Will
                featureGroup,
                halfOrcReq,
                Helpers.Create<AddFeatureOnApply>(r => r.Feature = LongswordProficiency),
                Helpers.Create<AddFeatureOnApply>(r => r.Feature = HeavyFlailProficiency),
                Helpers.Create<AddFeatureOnApply>(r => r.Feature = FlailProficiency),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = orcWeaponFamiliarity)));
            
            
            choices.Add(Helpers.CreateFeature("HatredTrait", "Hatred",
                "Half-orcs raised among orcs must prove themselves against their people’s enemies. Half-orcs with this racial trait gain a +1 racial bonus on attack rolls against humanoids of the dwarf, elf, and human subtypes because of their special training against these hated foes. This racial trait replaces intimidating and orc ferocity.",
                "439a22ed96a04f6f84b5d918839fd982",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"), // Iron Will
                featureGroup,
                halfOrcReq,
                GetAttackBonusVs(Main.objects.elf),
                GetAttackBonusVs(Main.objects.dwarf),
                GetAttackBonusVs(Main.objects.human),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = orcWeaponFamiliarity),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcIntimidating)
                ));
            
            choices.Add(Helpers.CreateFeature("SacredTattooTrait", "Sacred Tattoo",
                "Many half-orcs decorate themselves with tattoos, piercings, and ritual scarification, which they consider sacred markings. Half-orcs with this racial trait gain a +1 luck bonus on all saving throws. This racial trait replaces orc ferocity.",
                "49f1e221df5a44c2936d4746bd4f0e42",
                Helpers.GetIcon("175d1577bb6c9a04baf88eec99c66334"), // Iron Will
                featureGroup,
                halfOrcReq,
                StatType.SaveReflex.CreateAddStatBonus(1, ModifierDescriptor.Luck),
                StatType.SaveWill.CreateAddStatBonus(1, ModifierDescriptor.Luck),
                StatType.SaveFortitude.CreateAddStatBonus(1, ModifierDescriptor.Luck),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcFerocity)
            ));

            var endurance = (BlueprintFeature) Main.library.BlueprintsByAssetId["54ee847996c25cd4ba8773d7b8555174"];
            choices.Add(Helpers.CreateFeature("ShamansApprenticeTrait", "Shaman’s Apprentice",
                "Only the most stalwart survive the years of harsh treatment that an apprenticeship to an orc shaman entails. Half-orcs with this trait gain Endurance as a bonus feat. This racial trait replaces the intimidating trait.",
                "8f69e743803e49338aef56f35978ae08",
                endurance.Icon,
                featureGroup,
                halfOrcReq,
                Helpers.Create<AddFeatureOnApply>(r => r.Feature = endurance),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcIntimidating)
            ));
            const string bite1d4Id = "35dfad6517f401145af54111be04d6cf";
            var bite = library.CopyAndAdd<BlueprintItemWeapon>(bite1d4Id, "Orc Bite",
                "33ce8f8951204def9be2b001288a2198");
            bite.Type.DamageType.Physical.Form = PhysicalDamageForm.Piercing;

            choices.Add(Helpers.CreateFeature("ToothyTrait", "Toothy",
                "Some half-orcs’ tusks are large and sharp, granting a bite attack. This is a primary natural attack that deals 1d4 points of piercing damage. This racial trait replaces orc ferocity.",
                "bbd4dcd3a8114e2b9ad382443bbb2065",
                endurance.Icon,
                featureGroup,
                halfOrcReq,
                Helpers.Create<AddAdditionalLimb>(r => r.Weapon = bite),
                Helpers.Create<RemoveFeatureOnApply>(r => r.Feature = halfOrcFerocity)
            ));

            
            
            return choices;
        }

        private static AttackBonusAgainstFactOwner GetAttackBonusVs(BlueprintRace race)
        {
            var hatredBonus = Helpers.Create<AttackBonusAgainstFactOwner>();
            hatredBonus.CheckedFact = race;
            hatredBonus.Bonus = 1;
            hatredBonus.Descriptor = ModifierDescriptor.Racial;
            return hatredBonus;
        }
    }
}
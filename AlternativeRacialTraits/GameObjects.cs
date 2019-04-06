using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Localization;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using ProBuilder2.Common;

namespace AlternativeRacialTraits
{
    public class GameObjects
    {
        public BlueprintFeatureSelection skillFocusFeat;
        public List<BlueprintCharacterClass> classes;
        public List<BlueprintCharacterClass> prestigeClasses;
        public BlueprintCharacterClass sorcererClass, magusClass, dragonDiscipleClass;
        public BlueprintArchetype eldritchScionArchetype;
        const String basicFeatSelection = "247a4068296e8be42890143f451b4b45";
        public const String magusFeatSelection = "66befe7b24c42dd458952e3c47c93563";
        public BlueprintRace human, halfElf, halfOrc, elf, dwarf, halfling, gnome, aasimar, tiefling;
        public BlueprintRace[] races;
        public LocalizedString tenMinPerLevelDuration,
            minutesPerLevelDuration,
            hourPerLevelDuration,
            roundsPerLevelDuration,
            oneRoundDuration;

        public LocalizedString reflexHalfDamage, savingThrowNone;

        public BlueprintSpellList wizardSpellList,
            magusSpellList,
            druidSpellList,
            clericSpellList,
            paladinSpellList,
            inquisitorSpellList,
            alchemistSpellList,
            bardSpellList;

        public BlueprintItemWeapon touchWeapon { get; private set; }

        public readonly List<BlueprintAbility> allSpells = new List<BlueprintAbility>();
        public readonly List<BlueprintAbility> modSpells = new List<BlueprintAbility>();
        public readonly List<BlueprintAbility> spellsWithResources = new List<BlueprintAbility>();
        public readonly List<BlueprintItemEquipmentUsable> modScrolls = new List<BlueprintItemEquipmentUsable>();

        public readonly List<BlueprintLoot> allLoots = new List<BlueprintLoot>();
        public readonly List<BlueprintUnitLoot> allUnitLoots = new List<BlueprintUnitLoot>();

        public BlueprintFeatureSelection bloodlineSelection { get; private set; }

        public BlueprintWeaponEnchantment ghostTouch { get; private set; }

        public void Load()
        {
            var library = Main.library;

            // For some reason, Eldritch Scion is a class and an archetype.
            const string eldritchScionClassId = "f5b8c63b141b2f44cbb8c2d7579c34f5";
            classes = library.Root.Progression.CharacterClasses.Where(c => c.AssetGuid != eldritchScionClassId)
                .ToList();
            prestigeClasses = classes.Where(c => c.PrestigeClass).ToList();
            sorcererClass = GetClass("b3a505fb61437dc4097f43c3f8f9a4cf");
            magusClass = GetClass("45a4607686d96a1498891b3286121780");
            dragonDiscipleClass = GetClass("72051275b1dbb2d42ba9118237794f7c");
            eldritchScionArchetype =
                magusClass.Archetypes.First(a => a.AssetGuid == "d078b2ef073f2814c9e338a789d97b73");

            human = library.Get<BlueprintRace>("0a5d473ead98b0646b94495af250fdc4");
            halfElf = library.Get<BlueprintRace>("b3646842ffbd01643ab4dac7479b20b0");
            halfOrc = library.Get<BlueprintRace>("1dc20e195581a804890ddc74218bfd8e");
            elf = library.Get<BlueprintRace>("25a5878d125338244896ebd3238226c8");
            dwarf = library.Get<BlueprintRace>("c4faf439f0e70bd40b5e36ee80d06be7");
            halfling = library.Get<BlueprintRace>("b0c3ef2729c498f47970bb50fa1acd30");
            gnome = library.Get<BlueprintRace>("ef35a22c9a27da345a4528f0d5889157");
            aasimar = library.Get<BlueprintRace>("b7f02ba92b363064fb873963bec275ee");
            tiefling = library.Get<BlueprintRace>("5c4e42124dc2b4647af6e36cf2590500");

            races = new[] {human, halfElf, halfOrc, elf, dwarf, halfling, gnome, aasimar, tiefling};
            skillFocusFeat = library.Get<BlueprintFeatureSelection>("c9629ef9eebb88b479b2fbc5e836656a");

            tenMinPerLevelDuration =
                library.Get<BlueprintAbility>("5b77d7cc65b8ab74688e74a37fc2f553").LocalizedDuration; // barkskin
            minutesPerLevelDuration =
                library.Get<BlueprintAbility>("ef768022b0785eb43a18969903c537c4").LocalizedDuration; // shield
            hourPerLevelDuration =
                library.Get<BlueprintAbility>("9e1ad5d6f87d19e4d8883d63a6e35568").LocalizedDuration; // mage armor
            roundsPerLevelDuration =
                library.Get<BlueprintAbility>("486eaff58293f6441a5c2759c4872f98").LocalizedDuration; // haste
            oneRoundDuration =
                library.Get<BlueprintAbility>("2c38da66e5a599347ac95b3294acbe00").LocalizedDuration; // true strike
            reflexHalfDamage =
                library.Get<BlueprintAbility>("2d81362af43aeac4387a3d4fced489c3").LocalizedSavingThrow; // fireball
            savingThrowNone =
                library.Get<BlueprintAbility>("4ac47ddb9fa1eaf43a1b6809980cfbd2").LocalizedSavingThrow; // magic missle

            wizardSpellList = library.Get<BlueprintSpellList>("ba0401fdeb4062f40a7aa95b6f07fe89");
            magusSpellList = library.Get<BlueprintSpellList>("4d72e1e7bd6bc4f4caaea7aa43a14639");
            druidSpellList = library.Get<BlueprintSpellList>("bad8638d40639d04fa2f80a1cac67d6b");
            clericSpellList = library.Get<BlueprintSpellList>("8443ce803d2d31347897a3d85cc32f53");
            paladinSpellList = library.Get<BlueprintSpellList>("9f5be2f7ea64fe04eb40878347b147bc");
            inquisitorSpellList = library.Get<BlueprintSpellList>("57c894665b7895c499b3dce058c284b3");
            alchemistSpellList = library.Get<BlueprintSpellList>("f60d0cd93edc65c42ad31e34a905fb2f");
            bardSpellList = library.Get<BlueprintSpellList>("25a5013493bdcf74bb2424532214d0c8");

            touchWeapon = library.Get<BlueprintItemWeapon>("bb337517547de1a4189518d404ec49d4"); // TouchItem

            bloodlineSelection = library.Get<BlueprintFeatureSelection>("24bef8d1bee12274686f6da6ccbc8914");

            ghostTouch = library.Get<BlueprintWeaponEnchantment>("47857e1a5a3ec1a46adf6491b1423b4f");

            // Note: we can't easily scan all class spell lists, because some spells are
            // only added via special lists, like the ice version of burning hands.
            foreach (var blueprint in Main.library.GetAllBlueprints())
            {
                switch (blueprint)
                {
                    case BlueprintAbility spell when spell.Type == AbilityType.Spell:
                        // Tiefling racial SLAs are marked as spells rather than SLAs.
                        // (We can find them by the presence of the resource logic.)
                        if (spell.GetComponent<AbilityResourceLogic>() != null)
                        {
                            spellsWithResources.Add(spell);
                        }
                        else
                        {
                            allSpells.Add(spell);
                        }
                        break;
                    case BlueprintLoot loot:
                        allLoots.Add(loot);
                        break;
                    case BlueprintUnitLoot unitLoot:
                        allUnitLoots.Add(unitLoot);
                        break;
                }
            }

            BlueprintCharacterClass GetClass(String assetId) => classes.First(c => c.AssetGuid == assetId);
        }
    }
}
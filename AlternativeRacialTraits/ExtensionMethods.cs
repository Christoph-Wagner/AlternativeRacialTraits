using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using UnityEngine;

namespace AlternativeRacialTraits
{
    static class ExtensionMethods
    {
        public static V PutIfAbsent<K, V>(this IDictionary<K, V> self, K key, V value) where V : class
        {
            V oldValue;
            if (!self.TryGetValue(key, out oldValue))
            {
                self.Add(key, value);
                return value;
            }
            return oldValue;
        }

        public static V PutIfAbsent<K, V>(this IDictionary<K, V> self, K key, Func<V> ifAbsent) where V : class
        {
            V value;
            if (!self.TryGetValue(key, out value))
            {
                self.Add(key, value = ifAbsent());
                return value;
            }
            return value;
        }

        public static T[] AddToArray<T>(this T[] array, T value)
        {
            var len = array.Length;
            var result = new T[len + 1];
            Array.Copy(array, result, len);
            result[len] = value;
            return result;
        }

        public static T[] AddToArray<T>(this T[] array, params T[] values)
        {
            var len = array.Length;
            var valueLen = values.Length;
            var result = new T[len + valueLen];
            Array.Copy(array, result, len);
            Array.Copy(values, 0, result, len, valueLen);
            return result;
        }


        public static T[] AddToArray<T>(this T[] array, IEnumerable<T> values) => AddToArray(array, values.ToArray());

        public static T[] RemoveFromArray<T>(this T[] array, T value)
        {
            var list = array.ToList();
            return list.Remove(value) ? list.ToArray() : array;
        }

        public static string StringJoin<T>(this IEnumerable<T> array, Func<T, string> map, string separator = " ") => string.Join(separator, array.Select(map));

        static readonly FastSetter blueprintScriptableObject_set_AssetId = Helpers.CreateFieldSetter<BlueprintScriptableObject>("m_AssetGuid");

#if DEBUG
        static readonly Dictionary<String, BlueprintScriptableObject> assetsByName = new Dictionary<String, BlueprintScriptableObject>();

        internal static readonly List<BlueprintScriptableObject> newAssets = new List<BlueprintScriptableObject>();
#endif

        public static void AddAsset(this LibraryScriptableObject library, BlueprintScriptableObject blueprint, String guid)
        {
            blueprintScriptableObject_set_AssetId(blueprint, guid);
            // Sanity check that we don't stop on our own GUIDs or someone else's.
            if (library.BlueprintsByAssetId.TryGetValue(guid, out var existing))
            {
                throw new InvalidOperationException($"Duplicate AssetId, existing entry ID: {guid}, name: {existing.name}, type: {existing.GetType().Name}");
            }
            if (guid == "")
            {
                throw new InvalidOperationException($"Missing AssetId: {guid}, name: {existing.name}, type: {existing.GetType().Name}");
            }
#if DEBUG
            newAssets.Add(blueprint);
#endif
            library.GetAllBlueprints().Add(blueprint);
            library.BlueprintsByAssetId[guid] = blueprint;
        }

        public static void SetFeatures(this BlueprintFeatureSelection selection, IEnumerable<BlueprintFeature> features)
        {
            SetFeatures(selection, features.ToArray());
        }

        public static void SetFeatures(this BlueprintFeatureSelection selection, params BlueprintFeature[] features)
        {
            selection.AllFeatures = selection.Features = features;
        }

        public static void InsertComponent(this BlueprintScriptableObject obj, int index, BlueprintComponent component)
        {
            var components = obj.ComponentsArray.ToList();
            components.Insert(index, component);
            obj.SetComponents(components);
        }

        public static void AddComponent(this BlueprintScriptableObject obj, BlueprintComponent component)
        {
            obj.SetComponents(obj.ComponentsArray.AddToArray(component));
        }

        public static void RemoveComponent(this BlueprintScriptableObject obj, BlueprintComponent component)
        {
            obj.SetComponents(obj.ComponentsArray.RemoveFromArray(component));
        }

        public static void AddComponents(this BlueprintScriptableObject obj, IEnumerable<BlueprintComponent> components) => AddComponents(obj, components.ToArray());

        public static void AddComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
        {
            var c = obj.ComponentsArray.ToList();
            c.AddRange(components);
            obj.SetComponents(c.ToArray());
        }

        public static void SetComponents(this BlueprintScriptableObject obj, params BlueprintComponent[] components)
        {
            // Fix names of components. Generally this doesn't matter, but if they have serialization state,
            // then their name needs to be unique.
            var names = new HashSet<string>();
            foreach (var c in components)
            {
                if (string.IsNullOrEmpty(c.name))
                {
                    c.name = $"${c.GetType().Name}";
                }
                if (!names.Add(c.name))
                {
                    Compatibility.CheckComponent(obj, c);
                    string name;
                    for (int i = 0; !names.Add(name = $"{c.name}${i}"); i++) ;
                    c.name = name;
                }
                Log.Validate(c, obj);
            }

            obj.ComponentsArray = components;
        }

        public static void SetComponents(this BlueprintScriptableObject obj, IEnumerable<BlueprintComponent> components)
        {
            SetComponents(obj, components.ToArray());
        }

        public static void AddAsset(this LibraryScriptableObject library, BlueprintScriptableObject blueprint, String guid1, String guid2)
        {
            library.AddAsset(blueprint, Helpers.MergeIds(guid1, guid2));
        }

        public static T Get<T>(this LibraryScriptableObject library, String assetId) where T : BlueprintScriptableObject
        {
            return (T)library.BlueprintsByAssetId[assetId];
        }

        public static T TryGet<T>(this LibraryScriptableObject library, String assetId) where T : BlueprintScriptableObject
        {
            BlueprintScriptableObject result;
            if (library.BlueprintsByAssetId.TryGetValue(assetId, out result))
            {
                return (T)result;
            }
            return null;
        }

        public static T CopyAndAdd<T>(this LibraryScriptableObject library, String assetId, String newName, String newAssetId, String newAssetId2 = null) where T : BlueprintScriptableObject
        {
            return CopyAndAdd(library, Get<T>(library, assetId), newName, newAssetId, newAssetId2);
        }

        public static T CopyAndAdd<T>(this LibraryScriptableObject library, T original, String newName, String newAssetId, String newAssetId2 = null) where T : BlueprintScriptableObject
        {
            var clone = UnityEngine.Object.Instantiate(original);
            clone.name = newName;
            var id = newAssetId2 != null ? Helpers.MergeIds(newAssetId, newAssetId2) : newAssetId;
            AddAsset(library, clone, id);
            return clone;
        }

        static readonly FastSetter blueprintUnitFact_set_Description = Helpers.CreateFieldSetter<BlueprintUnitFact>("m_Description");
        static readonly FastSetter blueprintUnitFact_set_Icon = Helpers.CreateFieldSetter<BlueprintUnitFact>("m_Icon");
        static readonly FastSetter blueprintUnitFact_set_DisplayName = Helpers.CreateFieldSetter<BlueprintUnitFact>("m_DisplayName");
        static readonly FastGetter blueprintUnitFact_get_Description = Helpers.CreateFieldGetter<BlueprintUnitFact>("m_Description");
        static readonly FastGetter blueprintUnitFact_get_DisplayName = Helpers.CreateFieldGetter<BlueprintUnitFact>("m_DisplayName");

        public static void SetNameDescriptionIcon(this BlueprintUnitFact feature, String displayName, String description, Sprite icon)
        {
            SetNameDescription(feature, displayName, description);
            feature.SetIcon(icon);
        }

        public static void SetNameDescriptionIcon(this BlueprintUnitFact feature, BlueprintUnitFact other)
        {
            SetNameDescription(feature, other);
            feature.SetIcon(other.Icon);
        }

        public static void SetNameDescription(this BlueprintUnitFact feature, String displayName, String description)
        {
            feature.SetName(Helpers.CreateString(feature.name + ".Name", displayName));
            feature.SetDescription(description);
        }

        public static void SetNameDescription(this BlueprintUnitFact feature, BlueprintUnitFact other)
        {
            blueprintUnitFact_set_DisplayName(feature, other.GetName());
            blueprintUnitFact_set_Description(feature, other.GetDescription());
        }

        public static LocalizedString GetName(this BlueprintUnitFact fact) => (LocalizedString)blueprintUnitFact_get_DisplayName(fact);
        public static LocalizedString GetDescription(this BlueprintUnitFact fact) => (LocalizedString)blueprintUnitFact_get_Description(fact);

        public static void SetIcon(this BlueprintUnitFact feature, Sprite icon)
        {
            blueprintUnitFact_set_Icon(feature, icon);
        }

        public static void SetName(this BlueprintUnitFact feature, LocalizedString name)
        {
            blueprintUnitFact_set_DisplayName(feature, name);
        }

        public static void SetName(this BlueprintUnitFact feature, String name)
        {
            blueprintUnitFact_set_DisplayName(feature, Helpers.CreateString(feature.name + ".Name", name));
        }

        public static void SetDescription(this BlueprintUnitFact feature, String description)
        {
            blueprintUnitFact_set_Description(feature, Helpers.CreateString(feature.name + ".Description", description));
        }
        public static bool HasFeatureWithId(this LevelEntry level, String id)
        {
            return level.Features.Any(f => HasFeatureWithId(f, id));
        }

        public static bool HasFeatureWithId(this BlueprintUnitFact fact, String id)
        {
            if (fact.AssetGuid == id) return true;
            foreach (var c in fact.ComponentsArray)
            {
                var addFacts = c as AddFacts;
                if (addFacts != null) return addFacts.Facts.Any(f => HasFeatureWithId(f, id));
            }
            return false;
        }

        public static CasterSpellProgression GetCasterSpellProgression(this BlueprintSpellbook spellbook)
        {
            var spellsPerDay = spellbook.SpellsPerDay;
            if (spellsPerDay.GetCount(6, 3).HasValue)
            {
                return CasterSpellProgression.FullCaster;
            }
            else if (spellsPerDay.GetCount(7, 3).HasValue)
            {
                return CasterSpellProgression.ThreeQuartersCaster;
            }
            else if (spellsPerDay.GetCount(10, 3).HasValue)
            {
                return CasterSpellProgression.HalfCaster;
            }
            return CasterSpellProgression.UnknownCaster;
        }

        public static CasterSpellProgression GetCasterSpellProgression(this BlueprintSpellList spellList)
        {
            if (spellList.GetSpells(9).Count > 0)
            {
                return CasterSpellProgression.FullCaster;
            }
            else if (spellList.GetSpells(6).Count > 0)
            {
                return CasterSpellProgression.ThreeQuartersCaster;
            }
            else if (spellList.GetSpells(4).Count > 0)
            {
                return CasterSpellProgression.HalfCaster;
            }
            return CasterSpellProgression.UnknownCaster;
        }

        static readonly FastSetter blueprintArchetype_set_Icon = Helpers.CreateFieldSetter<BlueprintArchetype>("m_Icon");

        public static void SetIcon(this BlueprintArchetype self, Sprite icon)
        {
            blueprintArchetype_set_Icon(self, icon);
        }

        public static void AddToSpellList(this BlueprintAbility spell, BlueprintSpellList spellList, int level)
        {
            var comp = Helpers.Create<SpellListComponent>();
            comp.SpellLevel = level;
            comp.SpellList = spellList;
            spell.AddComponent(comp);
            spellList.SpellsByLevel[level].Spells.Add(spell);
            if (spellList == Main.objects.wizardSpellList)
            {
                var school = spell.School;
                var specialistList = specialistSchoolList.Value[(int)school];
                specialistList?.SpellsByLevel[level].Spells.Add(spell);
                var thassilonianList = thassilonianSchoolList.Value[(int)school];
                thassilonianList?.SpellsByLevel[level].Spells.Add(spell);
            }
        }

        static readonly Lazy<BlueprintSpellList[]> specialistSchoolList = new Lazy<BlueprintSpellList[]>(() =>
        {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            var library = Main.library;
            result[(int)SpellSchool.Abjuration] = library.Get<BlueprintSpellList>("c7a55e475659a944f9229d89c4dc3a8e");
            result[(int)SpellSchool.Conjuration] = library.Get<BlueprintSpellList>("69a6eba12bc77ea4191f573d63c9df12");
            result[(int)SpellSchool.Divination] = library.Get<BlueprintSpellList>("d234e68b3d34d124a9a2550fdc3de9eb");
            result[(int)SpellSchool.Enchantment] = library.Get<BlueprintSpellList>("c72836bb669f0c04680c01d88d49bb0c");
            result[(int)SpellSchool.Evocation] = library.Get<BlueprintSpellList>("79e731172a2dc1f4d92ba229c6216502");
            result[(int)SpellSchool.Illusion] = library.Get<BlueprintSpellList>("d74e55204daa9b14993b2e51ae861501");
            result[(int)SpellSchool.Necromancy] = library.Get<BlueprintSpellList>("5fe3acb6f439db9438db7d396f02c75c");
            result[(int)SpellSchool.Transmutation] = library.Get<BlueprintSpellList>("becbcfeca9624b6469319209c2a6b7f1");
            return result;
        });


        static readonly Lazy<BlueprintSpellList[]> thassilonianSchoolList = new Lazy<BlueprintSpellList[]>(() =>
        {
            var result = new BlueprintSpellList[(int)SpellSchool.Universalist + 1];
            var library = Main.library;
            result[(int)SpellSchool.Abjuration] = library.Get<BlueprintSpellList>("280dd5167ccafe449a33fbe93c7a875e");
            result[(int)SpellSchool.Conjuration] = library.Get<BlueprintSpellList>("5b154578f228c174bac546b6c29886ce");
            result[(int)SpellSchool.Enchantment] = library.Get<BlueprintSpellList>("ac551db78c1baa34eb8edca088be13cb");
            result[(int)SpellSchool.Evocation] = library.Get<BlueprintSpellList>("17c0bfe5b7c8ac3449da655cdcaed4e7");
            result[(int)SpellSchool.Illusion] = library.Get<BlueprintSpellList>("c311aed33deb7a346ab715baef4a0572");
            result[(int)SpellSchool.Necromancy] = library.Get<BlueprintSpellList>("5c08349132cb6b04181797f58ccf38ae");
            result[(int)SpellSchool.Transmutation] = library.Get<BlueprintSpellList>("f3a8f76b1d030a64084355ba3eea369a");
            return result;
        });

        public static void FixDomainSpell(this BlueprintAbility spell, int level, string spellListId)
        {
            var spellList = Main.library.Get<BlueprintSpellList>(spellListId);
            var spells = spellList.SpellsByLevel.First(s => s.SpellLevel == level).Spells;
            spells.Clear();
            spells.Add(spell);
        }

        // Similar to `metamagic.DefaultCost()`, but returns the result before Bag of Tricks
        // modifies it to 0.
        public static int OriginalCost(this Metamagic metamagic)
        {
            // Inline this so Bag of Tricks can't mutate it.
            switch (metamagic)
            {
                case Metamagic.Empower:
                    return 2;
                case Metamagic.Maximize:
                    return 3;
                case Metamagic.Quicken:
                    return 4;
                case Metamagic.Extend:
                    return 1;
                case Metamagic.Heighten:
                    return 0;
                case Metamagic.Reach:
                    return 1;
            }
            UberDebug.LogError($"Unknown metamagic: {metamagic}");
            return 0;
        }

        public static bool HasAreaEffect(this BlueprintAbility spell)
        {
            return spell.AoERadius.Meters > 0f || spell.ProjectileType != AbilityProjectileType.Simple;
        }

        public static void AddSelection(this BlueprintFeatureSelection feat, LevelUpState state, UnitDescriptor unit, int level)
        {
            // TODO: we may want to add the selection feat to the unit.
            // (But I don't think Respec mod will be able to clear it out if we do that.)
            // unit.AddFact(feat);
            state.AddSelection(null, feat, feat, level);
        }

        public static void SetIcon(this BlueprintAbilityResource resource, Sprite icon) => setIcon(resource, icon);

        static readonly FastSetter setIcon = Helpers.CreateFieldSetter<BlueprintAbilityResource>("m_Icon");
        static readonly FastSetter setMaxAmount = Helpers.CreateFieldSetter<BlueprintAbilityResource>("m_MaxAmount");
        internal static readonly FastGetter getMaxAmount = Helpers.CreateFieldGetter<BlueprintAbilityResource>("m_MaxAmount");
        //static readonly Type blueprintAbilityResource_Amount = Harmony12.AccessTools.Inner(typeof(BlueprintAbilityResource), "Amount");

        public static void SetIncreasedByLevel(this BlueprintAbilityResource resource, int baseValue, int levelIncrease, BlueprintCharacterClass[] classes, BlueprintArchetype[] archetypes = null)
        {
            var amount = getMaxAmount(resource);
            Helpers.SetField(amount, "BaseValue", baseValue);
            Helpers.SetField(amount, "IncreasedByLevel", true);
            Helpers.SetField(amount, "LevelIncrease", levelIncrease);
            Helpers.SetField(amount, "Class", classes);
            var emptyArchetypes = Array.Empty<BlueprintArchetype>();
            Helpers.SetField(amount, "Archetypes", archetypes ?? emptyArchetypes);

            // Enusre arrays are at least initialized to empty.
            var field = "ClassDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, Array.Empty<BlueprintCharacterClass>());
            field = "ArchetypesDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyArchetypes);

            setMaxAmount(resource, amount);
        }

        public static void SetFixedResource(this BlueprintAbilityResource resource, int baseValue)
        {
            var amount = getMaxAmount(resource);
            Helpers.SetField(amount, "BaseValue", baseValue);

            // Enusre arrays are at least initialized to empty.
            var emptyClasses = Array.Empty<BlueprintCharacterClass>();
            var emptyArchetypes = Array.Empty<BlueprintArchetype>();
            var field = "Class";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyClasses);
            field = "ClassDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyClasses);
            field = "Archetypes";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyArchetypes);
            field = "ArchetypesDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyArchetypes);

            setMaxAmount(resource, amount);
        }

        public static void SetIncreasedByStat(this BlueprintAbilityResource resource, int baseValue, StatType stat)
        {
            var amount = getMaxAmount(resource);
            Helpers.SetField(amount, "BaseValue", baseValue);
            Helpers.SetField(amount, "IncreasedByStat", true);
            Helpers.SetField(amount, "ResourceBonusStat", stat);

            // Enusre arrays are at least initialized to empty.
            var emptyClasses = Array.Empty<BlueprintCharacterClass>();
            var emptyArchetypes = Array.Empty<BlueprintArchetype>();
            var field = "Class";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyClasses);
            field = "ClassDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyClasses);
            field = "Archetypes";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyArchetypes);
            field = "ArchetypesDiv";
            if (Helpers.GetField(amount, field) == null) Helpers.SetField(amount, field, emptyArchetypes);

            setMaxAmount(resource, amount);
        }

        public static void SetIncreasedByLevelStartPlusDivStep(this BlueprintAbilityResource resource, int baseValue,
            int startingLevel, int startingIncrease, int levelStep, int perStepIncrease, int minClassLevelIncrease, float otherClassesModifier,
            BlueprintCharacterClass[] classes, BlueprintArchetype[] archetypes = null)
        {
            var amount = getMaxAmount(resource);
            Helpers.SetField(amount, "BaseValue", baseValue);
            Helpers.SetField(amount, "IncreasedByLevelStartPlusDivStep", true);
            Helpers.SetField(amount, "StartingLevel", startingLevel);
            Helpers.SetField(amount, "StartingIncrease", startingIncrease);
            Helpers.SetField(amount, "LevelStep", levelStep);
            Helpers.SetField(amount, "PerStepIncrease", perStepIncrease);
            Helpers.SetField(amount, "MinClassLevelIncrease", minClassLevelIncrease);
            Helpers.SetField(amount, "OtherClassesModifier", otherClassesModifier);

            Helpers.SetField(amount, "ClassDiv", classes);
            var emptyArchetypes = Array.Empty<BlueprintArchetype>();
            Helpers.SetField(amount, "ArchetypesDiv", archetypes ?? emptyArchetypes);

            // Enusre arrays are at least initialized to empty.
            var fieldName = "Class";
            if (Helpers.GetField(amount, fieldName) == null) Helpers.SetField(amount, fieldName, Array.Empty<BlueprintCharacterClass>());
            fieldName = "Archetypes";
            if (Helpers.GetField(amount, fieldName) == null) Helpers.SetField(amount, fieldName, emptyArchetypes);

            setMaxAmount(resource, amount);
        }

        internal static LocalizedString GetText(this DurationRate duration)
        {
            switch (duration)
            {
                case DurationRate.Rounds:
                    return Main.objects.roundsPerLevelDuration;
                case DurationRate.Minutes:
                    return Main.objects.minutesPerLevelDuration;
                case DurationRate.TenMinutes:
                    return Main.objects.tenMinPerLevelDuration;
                case DurationRate.Hours:
                    return Main.objects.hourPerLevelDuration;
            }
            throw new NotImplementedException($"DurationRate: {duration}");
        }

        internal static IEnumerable<BlueprintComponent> WithoutSpellComponents(this IEnumerable<BlueprintComponent> components)
        {
            return components.Where(c => !(c is SpellComponent) && !(c is SpellListComponent));
        }

        internal static int GetCost(this BlueprintAbility.MaterialComponentData material)
        {
            var item = material?.Item;
            return item == null ? 0 : item.Cost * material.Count;
        }

        internal static BuffFlags GetBuffFlags(this BlueprintBuff buff)
        {
            return (BuffFlags)(int)Helpers.GetField(buff, "m_Flags");
        }

        internal static void SetBuffFlags(this BlueprintBuff buff, BuffFlags flags)
        {
            Helpers.SetField(buff, "m_Flags", (int)flags);
        }

        public static AddConditionImmunity CreateImmunity(this UnitCondition condition)
        {
            var b = Helpers.Create<AddConditionImmunity>();
            b.Condition = condition;
            return b;
        }

        public static AddCondition CreateAddCondition(this UnitCondition condition)
        {
            var a = Helpers.Create<AddCondition>();
            a.Condition = condition;
            return a;
        }

        public static BuffDescriptorImmunity CreateBuffImmunity(this SpellDescriptor spell)
        {
            var b = Helpers.Create<BuffDescriptorImmunity>();
            b.Descriptor = spell;
            return b;
        }

        public static SpellImmunityToSpellDescriptor CreateSpellImmunity(this SpellDescriptor spell)
        {
            var s = Helpers.Create<SpellImmunityToSpellDescriptor>();
            s.Descriptor = spell;
            return s;
        }
    }
}
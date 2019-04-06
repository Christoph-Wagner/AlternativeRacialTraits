using System;
using System.Collections.Generic;
using System.Globalization;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.UI;
using Kingmaker.UnitLogic.FactLogic;
using UnityEngine;

namespace AlternativeRacialTraits
{
    public static class Helpers
    {
        // Returns a GUID that merges guid1 and guid2.
        //
        // Very often we're deriving something from existing game data (e.g. bloodlines)
        // For that code to be extensible to new bloodlines, we need to be able to combine
        // the GUIDs in the game assets with a GUID that is unique for that feat, and
        // get a new GUID that is stable across game reloads.
        //
        // These GUIDs are also nice in that they don't depend on order in which we create
        // our new Assets.
        //
        // Essentially, this prevents us from inadvertantly break existing saves that
        // use features from the mod.
        internal static String MergeIds(String guid1, String guid2, String guid3 = null)
        {
            // It'd be nice if these GUIDs were already in integer form.
            var id = BigInteger.Parse(guid1, NumberStyles.HexNumber);
            id ^= BigInteger.Parse(guid2, NumberStyles.HexNumber);
            if (guid3 != null)
            {
                id ^= BigInteger.Parse(guid3, NumberStyles.HexNumber);
            }
            return id.ToString("x32");
        }
        public static BlueprintFeature CreateFeature(String name, String displayName, String description, String guid, Sprite icon,
            FeatureGroup group, params BlueprintComponent[] components)
        {
            var feat = Create<BlueprintFeature>();
            SetFeatureInfo(feat, name, displayName, description, guid, icon, group, components);
            return feat;
        }
        public static BlueprintFeatureSelection CreateFeatureSelection(String name, String displayName,
            String description, String guid, Sprite icon, FeatureGroup group, params BlueprintComponent[] components)
        {
            var feat = Create<BlueprintFeatureSelection>();
            SetFeatureInfo(feat, name, displayName, description, guid, icon, group, components);
            feat.Group = group;
            return feat;
        }
        public static PrerequisiteFeature PrerequisiteFeature(this BlueprintFeature feat, bool any = false)
        {
            var result = Create<PrerequisiteFeature>();
            result.Feature = feat;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }
        public static PrerequisiteRace PrerequisiteRace(BlueprintRace race, bool any = false)
        {
            var result = Create<PrerequisiteRace>();
            result.Race = race;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }
        public static void SetFeatureInfo(BlueprintFeature feat, String name, String displayName, String description, String guid, Sprite icon,
            FeatureGroup group, params BlueprintComponent[] components)
        {
            feat.name = name;
            feat.SetComponents(components);
            feat.Groups = new [] { group };
            feat.SetNameDescriptionIcon(displayName, description, icon);
            Main.library.AddAsset(feat, guid);
        }
        public static void SetField(object obj, string name, object value)
        {
            Harmony12.AccessTools.Field(obj.GetType(), name).SetValue(obj, value);
        }

        public static void SetLocalizedStringField(BlueprintScriptableObject obj, string name, string value)
        {
            Harmony12.AccessTools.Field(obj.GetType(), name).SetValue(obj, Helpers.CreateString($"{obj.name}.{name}", value));
        }
        public static object GetField(object obj, string name)
        {
            return Harmony12.AccessTools.Field(obj.GetType(), name).GetValue(obj);
        }

        public static object GetField(Type type, object obj, string name)
        {
            return Harmony12.AccessTools.Field(type, name).GetValue(obj);
        }

        public static T GetField<T>(object obj, string name)
        {
            return (T)Harmony12.AccessTools.Field(obj.GetType(), name).GetValue(obj);
        }
        internal static Sprite GetIcon(string assetId)
        {
            var asset = (IUIDataProvider)Main.library.BlueprintsByAssetId[assetId];
            return asset.Icon;
        }
        public static PrerequisiteNoFeature PrerequisiteNoFeature(this BlueprintFeature feat, bool any = false)
        {
            var result = Create<PrerequisiteNoFeature>();
            result.Feature = feat;
            result.Group = any ? Prerequisite.GroupType.Any : Prerequisite.GroupType.All;
            return result;
        }
        public static AddStatBonus CreateAddStatBonus(this StatType stat, int value, ModifierDescriptor descriptor)
        {
            var addStat = Create<AddStatBonus>();
            addStat.Stat = stat;
            addStat.Value = value;
            addStat.Descriptor = descriptor;
            return addStat;
        }
        
        public static T Create<T>(Action<T> init = null) where T : ScriptableObject
        {
            var result = ScriptableObject.CreateInstance<T>();
            init?.Invoke(result);
            return result;
        }
        internal static LocalizedString CreateString(string key, string value)
        {
            // See if we used the text previously.
            // (It's common for many features to use the same localized text.
            // In that case, we reuse the old entry instead of making a new one.)
            if (TextToLocalizedString.TryGetValue(value, out var localized))
            {
                return localized;
            }
            var strings = LocalizationManager.CurrentPack.Strings;
            if (strings.TryGetValue(key, out var oldValue) && value != oldValue)
            {
                Log.Write($"Info: duplicate localized string `{key}`, different text.");
            }
            strings[key] = value;
            localized = new LocalizedString();
            localizedString_m_Key(localized, key);
            TextToLocalizedString[value] = localized;
            return localized;
        }

        // All localized strings created in this mod, mapped to their localized key. Populated by CreateString.
        static readonly Dictionary<String, LocalizedString> TextToLocalizedString = new Dictionary<string, LocalizedString>();
        static FastSetter localizedString_m_Key = CreateFieldSetter<LocalizedString>("m_Key");
        public static FastGetter CreateGetter<T>(string name) => CreateGetter(typeof(T), name);

        public static FastGetter CreateGetter(Type type, string name)
        {
            return new FastGetter(Harmony12.FastAccess.CreateGetterHandler(Harmony12.AccessTools.Property(type, name)));
        }

        public static FastGetter CreateFieldGetter<T>(string name) => CreateFieldGetter(typeof(T), name);

        public static FastGetter CreateFieldGetter(Type type, string name)
        {
            return new FastGetter(Harmony12.FastAccess.CreateGetterHandler(Harmony12.AccessTools.Field(type, name)));
        }

        public static FastSetter CreateSetter<T>(string name) => CreateSetter(typeof(T), name);

        public static FastSetter CreateSetter(Type type, string name)
        {
            return new FastSetter(Harmony12.FastAccess.CreateSetterHandler(Harmony12.AccessTools.Property(type, name)));
        }

        public static FastSetter CreateFieldSetter<T>(string name) => CreateFieldSetter(typeof(T), name);

        public static FastSetter CreateFieldSetter(Type type, string name)
        {
            return new FastSetter(Harmony12.FastAccess.CreateSetterHandler(Harmony12.AccessTools.Field(type, name)));
        }

        public static FastInvoke CreateInvoker<T>(String name) => CreateInvoker(typeof(T), name);

        public static FastInvoke CreateInvoker(Type type, String name)
        {
            return new FastInvoke(Harmony12.MethodInvoker.GetHandler(Harmony12.AccessTools.Method(type, name)));
        }

        public static FastInvoke CreateInvoker<T>(String name, Type[] args, Type[] typeArgs = null) => CreateInvoker(typeof(T), name, args, typeArgs);

        public static FastInvoke CreateInvoker(Type type, String name, Type[] args, Type[] typeArgs = null)
        {
            return new FastInvoke(Harmony12.MethodInvoker.GetHandler(Harmony12.AccessTools.Method(type, name, args, typeArgs)));
        }
    }
    public delegate void FastSetter(object source, object value);
    public delegate object FastGetter(object source);
    public delegate object FastInvoke(object target, params object[] paramters);
}
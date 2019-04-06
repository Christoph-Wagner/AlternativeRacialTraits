using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony12;
using Kingmaker.Blueprints;
using Kingmaker.Utility;
using UnityEngine;
using UnityModManagerNet;

namespace AlternativeRacialTraits
{
#if DEBUG
    [EnableReloading]
#endif
    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger Logger { get; set; }
        private static bool Enabled { get; set; }
        static HarmonyInstance harmonyInstance;
        public static GameObjects objects;
        static string testedGameVersion = "1.3.0";
        static readonly Dictionary<Type, bool> typesPatched = new Dictionary<Type, bool>();
        static readonly List<String> failedPatches = new List<String>();
        static readonly List<String> failedLoading = new List<String>();
        internal static LibraryScriptableObject library;
        
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
#if DEBUG
            modEntry.OnUnload = Unload;
#endif
            harmonyInstance = HarmonyInstance.Create(modEntry.Info.Id);
            if (!ApplyPatch(typeof(LibraryScriptableObject_LoadDictionary_Patch), "All mod features"))
            {
                throw new InvalidOperationException("Failed to patch LibraryScriptableObject.LoadDictionary(), cannot load mod");
            }
            return true;
        }

        internal static bool ApplyPatch(Type type, String featureName)
        {
            try
            {
                if (typesPatched.ContainsKey(type)) return typesPatched[type];

                var patchInfo = HarmonyMethodExtensions.GetHarmonyMethods(type);
                if (patchInfo == null || !patchInfo.Any())
                {
                    Log.Error($"Failed to apply patch {type}: could not find Harmony attributes");
                    failedPatches.Add(featureName);
                    typesPatched.Add(type, false);
                    return false;
                }
                var processor = new PatchProcessor(harmonyInstance, type, HarmonyMethod.Merge(patchInfo));
                var patch = Enumerable.FirstOrDefault(processor.Patch());
                if (patch == null)
                {
                    Log.Error($"Failed to apply patch {type}: no dynamic method generated");
                    failedPatches.Add(featureName);
                    typesPatched.Add(type, false);
                    return false;
                }
                typesPatched.Add(type, true);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to apply patch {type}: {e}");
                failedPatches.Add(featureName);
                typesPatched.Add(type, false);
                return false;
            }
        }
        
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        
#if DEBUG
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            harmonyInstance.UnpatchAll();
            return true;
        }
#endif
        internal static void SafeLoad(Action load, String name)
        {
            try
            {
                load();
            }
            catch (Exception e)
            {
                failedLoading.Add(name);
                Log.Error(e);
            }
        }

        internal static T SafeLoad<T>(Func<T> load, String name)
        {
            try
            {
                return load();
            }
            catch (Exception e)
            {
                failedLoading.Add(name);
                Log.Error(e);
                return default;
            }
        }
        static void CheckPatchingSuccess()
        {
            // Check to make sure we didn't forget to patch something.
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var infos = HarmonyMethodExtensions.GetHarmonyMethods(type);
                if (infos != null && infos.Any() && !typesPatched.ContainsKey(type))
                {
                    Log.Write($"Did not apply patch for {type}");
                }
            }
        }
        [System.Diagnostics.Conditional("DEBUG")]
        static void EnableGameLogging()
        {
            if (UberLogger.Logger.Enabled) return;
            // Code taken from GameStarter.Awake(). PF:K logging can be enabled with command line flags,
            // but when developing the mod it's easier to force it on.
            var dataPath = ApplicationPaths.persistentDataPath;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            UberLogger.Logger.Enabled = true;
            var text = Path.Combine(dataPath, "GameLog.txt");
            if (File.Exists(text))
            {
                File.Copy(text, Path.Combine(dataPath, "GameLogPrev.txt"), overwrite: true);
                File.Delete(text);
            }
            UberLogger.Logger.AddLogger(new UberLoggerFile("GameLogFull.txt", dataPath));
            UberLogger.Logger.AddLogger(new UberLoggerFilter(new UberLoggerFile("GameLog.txt", dataPath), UberLogger.LogSeverity.Warning, "MatchLight"));
            UberLogger.Logger.Enabled = true;
        }
        [HarmonyPatch(typeof(LibraryScriptableObject), "LoadDictionary", new Type[0])]
        static class LibraryScriptableObject_LoadDictionary_Patch
        {
            static void Postfix(LibraryScriptableObject __instance)
            {
                var self = __instance;
                if (library != null) return;
                library = self;
                objects = new GameObjects();
                EnableGameLogging();

                SafeLoad(objects.Load, "Initialization code");
                SafeLoad(RacialTraits.Load, "Racial Trait Replacement");
//                SafeLoad(Spells.Load, "New spells");
//                // Note: needs to be loaded after other spells, so it can offer them as a choice.
//                SafeLoad(WishSpells.Load, "Wish spells");
//                // Note: needs to run before almost everything else, so they can find the Oracle class.
//                // However needs to run after spells are added, because it uses some of them.
//                SafeLoad(OracleClass.Load, "Oracle class");
//                // Note: spells need to be added before this, because it adds metamagics.
//                // It needs to run after new classes too, because SpellSpecialization needs to find all class spell lists.
//                SafeLoad(MagicFeats.Load, "Magic feats");
//                // Note: needs to run after arcane spells (it uses some of them).
//                // Note: needs to run after things that add classes, and after bloodlines in case
//                // they allow qualifying for racial prerequisites.
//                SafeLoad(FavoredClassBonus.Load, "Favored class bonus, deity selection");
//                SafeLoad(Traits.Load, "Traits");
//                // Note: needs to run after we create Favored Prestige Class above.
//                SafeLoad(PrestigiousSpellcaster.Load, "Prestigious Spellcaster");
//                // Note: needs to run after things that add bloodlines.
//                SafeLoad(CrossbloodedSorcerer.Load, "Crossblooded Sorcerer");
//                // Note: needs to run after things that add martial classes or bloodlines.
//                SafeLoad(EldritchHeritage.Load, "Eldritch Heritage");
//                // Note: needs to run after crossblooded and spontaneous caster classes,
//                // so it can find their spellbooks.
//                SafeLoad(ReplaceSpells.Load, "Spell replacement for spontaneous casters");
//#if DEBUG
//                // Perform extra sanity checks in debug builds.
//                SafeLoad(CheckPatchingSuccess, "Check that all patches are used, and were loaded");
////                SafeLoad(SaveCompatibility.CheckCompat, "Check save game compatibility");
//                Log.Write("Loaded finished.");
//#endif
            }
        }

        
    }
}
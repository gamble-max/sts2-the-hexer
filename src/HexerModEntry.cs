using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Logging;

namespace HexerMod
{
    [ModInitializer("Initialize")]
    public static class HexerModEntry
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            var harmony = new Harmony("sts2mods.hexer");
            int totalPatches = 0;
            foreach (Type type in typeof(HexerModEntry).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length == 0)
                {
                    continue;
                }
                try
                {
                    var patched = harmony.CreateClassProcessor(type).Patch();
                    int count = patched?.Count ?? 0;
                    totalPatches += count;
                    Log.Info($"[Hexer] Patched {count} method(s) from {type.Name}.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[Hexer] Failed to patch {type.Name}: {ex}");
                }
            }
            Log.Info($"[Hexer] Mod initialized ({totalPatches} total patched method(s)).");
            ModHelper.SubscribeForCombatStateHooks("hexer_combat_subscriber", Core.HexerCombatHookSubscriber.GetSubscribers);
            Patches.RegistrationPatches.InvalidateModelDbCaches();
        }
    }
}

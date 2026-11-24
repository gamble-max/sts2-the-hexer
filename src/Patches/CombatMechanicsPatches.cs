using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Patches
{
    [HarmonyPatch]
    public static class CombatMechanicsPatches
    {
        /// <summary>
        /// Preview Psychokinesis heal bonus on cards in hand/targeting.
        /// </summary>
        [HarmonyPatch(typeof(DynamicVar), nameof(DynamicVar.UpdateCardPreview))]
        [HarmonyPostfix]
        public static void DynamicVarUpdateCardPreviewPostfix(DynamicVar __instance, CardModel card, bool runGlobalHooks)
        {
            if (__instance is not HealVar || !runGlobalHooks)
            {
                return;
            }
            int bonus = HexerCombatTracker.GetPreviewEmpowerBonus(card);
            if (bonus > 0)
            {
                __instance.PreviewValue = __instance.BaseValue + bonus;
            }
        }

        /// <summary>
        /// Unify Heal empowerment across all cards (Hexer & Colorless) when an empowered card is actively playing.
        /// </summary>
        [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal), typeof(bool))]
        [HarmonyPrefix]
        public static void CreatureCmdHealPrefix(Creature creature, ref decimal amount)
        {
            int bonus = HexerCombatTracker.GetCurrentActiveBonus();
            if (bonus > 0 && amount > 0m)
            {
                amount += bonus;
            }
        }
        private static bool AnyAllyHasAbyssalGaze(Creature? creature)
        {
            var combatState = creature?.CombatState;
            if (combatState == null)
            {
                return false;
            }
            return combatState.PlayerCreatures.Any(c => c.IsAlive && c.GetPower<AbyssalGazePower>() != null);
        }

        /// <summary>
        /// Abyssal Gaze: If an enemy has Weak + Vulnerable > 5, double their Weak multiplier (-25% -> -50%).
        /// </summary>
        [HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
        [HarmonyPostfix]
        public static void WeakPowerModifyDamagePostfix(
            WeakPower __instance,
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
            CardPlay? cardPlay,
            ref decimal __result)
        {
            if (__instance.Owner == null || !__instance.Owner.IsEnemy || dealer != __instance.Owner)
            {
                return;
            }
            if (__result >= 1m)
            {
                return;
            }
            if (GrudgeHelper.GetWeakPlusVulnerable(__instance.Owner) > 5 && AnyAllyHasAbyssalGaze(__instance.Owner))
            {
                __result -= (1m - __result);
            }
        }

        /// <summary>
        /// Abyssal Gaze: If an enemy has Weak + Vulnerable > 5, double their Vulnerable multiplier (+50% -> +100%).
        /// </summary>
        [HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
        [HarmonyPostfix]
        public static void VulnerablePowerModifyDamagePostfix(
            VulnerablePower __instance,
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
            CardPlay? cardPlay,
            ref decimal __result)
        {
            if (__instance.Owner == null || !__instance.Owner.IsEnemy || target != __instance.Owner)
            {
                return;
            }
            if (__result <= 1m)
            {
                return;
            }
            if (GrudgeHelper.GetWeakPlusVulnerable(__instance.Owner) > 5 && AnyAllyHasAbyssalGaze(__instance.Owner))
            {
                __result += (__result - 1m);
            }
        }
    }
}

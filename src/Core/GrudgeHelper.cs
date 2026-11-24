using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Sts2Mods.Shared;
using HexerMod.Powers;

namespace HexerMod.Core
{
    public static class GrudgeHelper
    {
        /// <summary>
        /// Specific debuffs recognized by Hexer mechanics and Grudge:
        /// Weak (虚弱), Frail (脆弱), Shrink (缩小), and Vulnerable (易伤).
        /// </summary>
        public static bool IsSpecificDebuff(PowerModel? power)
        {
            if (power == null || power.Amount <= 0)
            {
                return false;
            }
            return power is WeakPower
                || power is FrailPower
                || power is ShrinkPower
                || power is VulnerablePower;
        }

        public static bool IsGrudgeEligibleDebuff(PowerModel? power) => IsSpecificDebuff(power);

        public static IReadOnlyList<IHoverTip> GetSpecificDebuffHoverTips(
            bool includeExorcise = false,
            bool includeBlessing = false)
        {
            var tips = new List<IHoverTip>
            {
                new HoverTip(
                    new LocString("powers", "HEXER_GRUDGE_TIP.title"),
                    new LocString("powers", "HEXER_GRUDGE_TIP.description"),
                    ModAssetHelper.LoadTexture("res://Hexer/images/vfx/grudge_hexer_single.png"))
            };
            if (includeExorcise)
            {
                tips.Add(new HoverTip(
                    new LocString("powers", "HEXER_EXORCISE_TIP.title"),
                    new LocString("powers", "HEXER_EXORCISE_TIP.description"),
                    ModAssetHelper.LoadTexture("res://Hexer/images/powers/exorcised_strength_loss_power.png")));
            }
            if (includeBlessing)
            {
                tips.Add(new HoverTip(
                    new LocString("powers", "HEXER_BLESSING_TIP.title"),
                    new LocString("powers", "HEXER_BLESSING_TIP.description"),
                    ModAssetHelper.LoadTexture("res://Hexer/images/powers/ascendant_medium_power.png")));
            }
            tips.Add(HoverTipFactory.FromPower<WeakPower>());
            tips.Add(HoverTipFactory.FromPower<FrailPower>());
            tips.Add(HoverTipFactory.FromPower<ShrinkPower>());
            tips.Add(HoverTipFactory.FromPower<VulnerablePower>());
            return tips;
        }

        public static int GetGrudgeCount(Creature? creature)
        {
            if (creature == null || !creature.IsAlive)
            {
                return 0;
            }

            int total = 0;
            foreach (var p in creature.Powers)
            {
                if (IsGrudgeEligibleDebuff(p))
                {
                    total += p.Amount;
                }
            }
            return total;
        }

        public static int GetWeakPlusVulnerable(Creature? creature)
        {
            if (creature == null)
            {
                return 0;
            }

            int weak = creature.GetPower<WeakPower>()?.Amount ?? 0;
            int vuln = creature.GetPower<VulnerablePower>()?.Amount ?? 0;
            return weak + vuln;
        }

        /// <summary>
        /// Randomly remove up to n layers of Grudge-eligible debuffs from target.
        /// Returns the actual number of layers removed k = min(n, GrudgeCount(target)).
        /// </summary>
        public static async Task<int> RemoveRandomGrudgeDebuffsAsync(
            PlayerChoiceContext choiceContext,
            Creature target,
            Creature applier,
            int maxLayers,
            CardModel? cardSource)
        {
            if (target == null || !target.IsAlive || maxLayers <= 0)
            {
                return 0;
            }

            int totalGrudge = GetGrudgeCount(target);
            int toRemove = Math.Min(maxLayers, totalGrudge);
            if (toRemove <= 0)
            {
                return 0;
            }

            var rng = applier?.Player?.RunState?.Rng?.Niche
                ?? target.CombatState?.RunState?.Rng?.Niche;

            var decrements = new Dictionary<PowerModel, int>();
            var pool = new List<PowerModel>();
            foreach (var p in target.Powers)
            {
                if (IsGrudgeEligibleDebuff(p))
                {
                    for (int i = 0; i < p.Amount; i++)
                    {
                        pool.Add(p);
                    }
                }
            }

            int actualRemoved = 0;
            for (int i = 0; i < toRemove && pool.Count > 0; i++)
            {
                int idx = rng != null ? rng.NextInt(pool.Count) : 0;
                var chosen = pool[idx];
                pool.RemoveAt(idx);

                decrements.TryGetValue(chosen, out int count);
                decrements[chosen] = count + 1;
                actualRemoved++;
            }

            foreach (var kvp in decrements)
            {
                var power = kvp.Key;
                int removeAmount = kvp.Value;
                if (removeAmount >= power.Amount)
                {
                    await PowerCmd.Remove(power);
                }
                else
                {
                    await PowerCmd.ModifyAmount(choiceContext, power, -removeAmount, applier, cardSource, silent: false);
                }
            }

            return actualRemoved;
        }

        /// <summary>
        /// 祓除 n (on enemy): randomly remove k = min(n, Grudge) layers of Grudge-eligible debuffs,
        /// then target loses 2k Strength (not counted as a debuff).
        /// </summary>
        public static async Task<int> ExecuteExorciseAsync(
            PlayerChoiceContext choiceContext,
            Creature target,
            Creature applier,
            int n,
            CardModel? cardSource)
        {
            int k = await RemoveRandomGrudgeDebuffsAsync(choiceContext, target, applier, n, cardSource);
            if (k > 0)
            {
                if (target.IsAlive)
                {
                    await PowerCmd.Apply<ExorcisedStrengthLossPower>(
                        choiceContext,
                        target,
                        2m * k,
                        applier,
                        cardSource,
                        silent: false);
                }
                if (applier?.Player?.GetRelic<HexerMod.Relics.ObsidianTuningFork>() is { } fork)
                {
                    await fork.TryTriggerAsync(choiceContext);
                }
                if (target.IsAlive && applier?.Player?.GetRelic<HexerMod.Relics.SoulboundMirror>() is { } mirror)
                {
                    mirror.Flash();
                    await CreatureCmd.Damage(choiceContext, target, 4m * k, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, applier!);
                }
                if (applier != null && applier.IsAlive && applier.GetPower<UnendingGrudgePower>() is { } ug && ug.Amount > 0)
                {
                    await PowerCmd.Apply<PsychokinesisPower>(choiceContext, applier, 2m * k * ug.Amount, applier, cardSource, silent: false);
                    await CreatureCmd.GainBlock(applier, 2m * k * ug.Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null, fast: false);
                }
            }
            return k;
        }

        /// <summary>
        /// 祈福 n (on ally): randomly remove k = min(n, Grudge) layers of Grudge-eligible debuffs,
        /// then target gains 2k Strength.
        /// </summary>
        public static async Task<int> ExecuteBlessingAsync(
            PlayerChoiceContext choiceContext,
            Creature target,
            Creature applier,
            int n,
            CardModel? cardSource)
        {
            int k = await RemoveRandomGrudgeDebuffsAsync(choiceContext, target, applier, n, cardSource);
            if (k > 0)
            {
                if (target.IsAlive)
                {
                    await PowerCmd.Apply<StrengthPower>(
                        choiceContext,
                        target,
                        2m * k,
                        applier,
                        cardSource,
                        silent: false);
                }
                if (applier?.Player?.GetRelic<HexerMod.Relics.ObsidianTuningFork>() is { } fork)
                {
                    await fork.TryTriggerAsync(choiceContext);
                }
                if (applier != null && applier.IsAlive && applier.GetPower<UnendingGrudgePower>() is { } ug && ug.Amount > 0)
                {
                    await PowerCmd.Apply<PsychokinesisPower>(choiceContext, applier, 2m * k * ug.Amount, applier, cardSource, silent: false);
                    await CreatureCmd.GainBlock(applier, 2m * k * ug.Amount, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered, null, fast: false);
                }
            }
            return k;
        }
    }
}

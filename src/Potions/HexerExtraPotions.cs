using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using HexerMod.Powers;

namespace HexerMod.Potions
{
    public sealed class SpiritTears : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Common;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.Self;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(6m) };

        public override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        {
            if (Owner?.Creature != null && Owner.Creature.IsAlive)
            {
                await PowerCmd.Apply<PsychokinesisPower>(
                    choiceContext,
                    Owner.Creature,
                    DynamicVars["PsychokinesisPower"].BaseValue,
                    Owner.Creature,
                    null,
                    silent: false);
            }
        }
    }

    public sealed class AbyssalElixir : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Rare;
        public override PotionUsage Usage => PotionUsage.CombatOnly;
        public override TargetType TargetType => TargetType.AllEnemies;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<WeakPower>(3m),
                new PowerVar<VulnerablePower>(3m)
            };

        public override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<WeakPower>(),
                HoverTipFactory.FromPower<VulnerablePower>()
            };

        protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
        {
            if (Owner?.Creature?.CombatState == null)
            {
                return;
            }
            decimal weak = DynamicVars["WeakPower"].BaseValue;
            decimal vuln = DynamicVars["VulnerablePower"].BaseValue;
            foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weak, Owner.Creature, null, silent: false);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vuln, Owner.Creature, null, silent: false);
            }
        }
    }
}

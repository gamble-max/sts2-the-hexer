using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class ShadowWhisper : HexerCardBase
    {
        public ShadowWhisper() : base(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<WeakPower>(1m),
                new PowerVar<VulnerablePower>(1m),
                new PowerVar<PsychokinesisPower>(2m)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<WeakPower>(),
                HoverTipFactory.FromPower<VulnerablePower>(),
                HoverTipFactory.FromPower<PsychokinesisPower>()
            };

        protected override void OnUpgrade()
        {
            DynamicVars["WeakPower"].UpgradeValueBy(1m);
            DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
            DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null)
            {
                return;
            }
            decimal weakAmt = DynamicVars["WeakPower"].BaseValue;
            decimal vulnAmt = DynamicVars["VulnerablePower"].BaseValue;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weakAmt, Owner.Creature, this, silent: false);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vulnAmt, Owner.Creature, this, silent: false);
            }
            if (Owner.Creature.IsAlive)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }
}

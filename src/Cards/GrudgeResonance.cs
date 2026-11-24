using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;

namespace HexerMod.Cards
{
    public sealed class GrudgeResonance : HexerCardBase
    {
        public GrudgeResonance() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new CalculationBaseVar(6m),
                new ExtraDamageVar(2m),
                new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, target) =>
                    target == null ? 0m : GrudgeHelper.GetGrudgeCount(target))
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.CalculationBase.UpgradeValueBy(3m);
            DynamicVars.ExtraDamage.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null)
            {
                return;
            }
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitCount(1)
                .Execute(choiceContext);
        }
    }
}

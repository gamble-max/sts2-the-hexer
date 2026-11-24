using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;

namespace HexerMod.Cards
{
    public sealed class TenThousandGrudges : HexerCardBase
    {
        public TenThousandGrudges() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(4m, ValueProp.Move),
                new IntVar("Hits", 2m)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null)
            {
                return;
            }
            int extraHits = GrudgeHelper.GetGrudgeCount(Owner.Creature) + GrudgeHelper.GetGrudgeCount(cardPlay.Target);
            int totalHits = DynamicVars["Hits"].IntValue + extraHits;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitCount(totalHits)
                .Execute(choiceContext);
        }
    }
}

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
    public sealed class ExorciseStrike : HexerCardBase
    {
        public ExorciseStrike() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new DamageVar(7m, ValueProp.Move),
                new IntVar("Exorcise", 1m)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars["Exorcise"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null)
            {
                return;
            }
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitCount(1)
                .Execute(choiceContext);

            if (cardPlay.Target.IsAlive)
            {
                await GrudgeHelper.ExecuteExorciseAsync(
                    choiceContext,
                    cardPlay.Target,
                    Owner.Creature,
                    DynamicVars["Exorcise"].IntValue,
                    this);
            }
        }
    }
}

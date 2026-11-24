using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HexerMod.Core;

namespace HexerMod.Cards
{
    public sealed class PurifyingPrayer : HexerCardBase, IHexerEmpowerCard
    {
        public PurifyingPrayer() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer, true) { }

        public bool IsPsychokinesisEmpowerEligible => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new IntVar("Blessing", 2m),
                new HealVar(4m)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeBlessing: true);

        protected override void OnUpgrade()
        {
            DynamicVars["Blessing"].UpgradeValueBy(1m);
            DynamicVars.Heal.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target ?? Owner.Creature;
            await GrudgeHelper.ExecuteBlessingAsync(
                choiceContext,
                target,
                Owner.Creature,
                DynamicVars["Blessing"].IntValue,
                this);

            if (target.IsAlive)
            {
                await CreatureCmd.Heal(target, DynamicVars.Heal.BaseValue, playAnim: true);
            }
        }
    }
}

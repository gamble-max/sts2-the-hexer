using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class MindOverload : HexerCardBase
    {
        public MindOverload() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<MindOverloadPower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this,
                silent: false);
        }
    }
}

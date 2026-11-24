using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class AbyssalGaze : HexerCardBase
    {
        public AbyssalGaze() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<WeakPower>(),
                HoverTipFactory.FromPower<VulnerablePower>()
            };

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<AbyssalGazePower>(
                choiceContext,
                Owner.Creature,
                1m,
                Owner.Creature,
                this,
                silent: false);
        }
    }
}

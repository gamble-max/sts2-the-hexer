using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HexerMod.Cards
{
    public sealed class GrudgePulse : HexerCardBase
    {
        public GrudgePulse() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new CardsVar(2),
                new BlockVar(5m, ValueProp.Move)
            };

        protected override void OnUpgrade()
        {
            DynamicVars.Cards.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var drawn = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
            if (drawn.Any(c => c.Type == CardType.Curse))
            {
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
        }
    }
}

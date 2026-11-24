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
    public sealed class WraithTalisman : HexerCardBase
    {
        public WraithTalisman() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<WeakPower>(1m),
                new PowerVar<PsychokinesisPower>(5m),
                new CardsVar(1)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[]
            {
                HoverTipFactory.FromPower<WeakPower>(),
                HoverTipFactory.FromPower<PsychokinesisPower>()
            };

        protected override void OnUpgrade()
        {
            DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["WeakPower"].BaseValue,
                Owner.Creature,
                this,
                silent: false);

            await PowerCmd.Apply<PsychokinesisPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars["PsychokinesisPower"].BaseValue,
                Owner.Creature,
                this,
                silent: false);

            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }
}

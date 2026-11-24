using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class BlindStep : HexerCardBase
    {
        public BlindStep() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(7m, ValueProp.Move), new CardsVar(1) };

        protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }

    public sealed class Hexbrand : HexerCardBase
    {
        public Hexbrand() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<VulnerablePower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<VulnerablePower>() };

        protected override void OnUpgrade() => DynamicVars["VulnerablePower"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class MireShroud : HexerCardBase
    {
        public MireShroud() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(7m, ValueProp.Move), new PowerVar<ShrinkPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<ShrinkPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars["ShrinkPower"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            if (cardPlay.Target != null && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<ShrinkPower>(choiceContext, cardPlay.Target, DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class WhisperingPrayer : HexerCardBase
    {
        public WhisperingPrayer() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyPlayer, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Blessing", 1m), new BlockVar(5m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeBlessing: true);

        protected override void OnUpgrade()
        {
            DynamicVars["Blessing"].UpgradeValueBy(1m);
            DynamicVars.Block.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target ?? Owner.Creature;
            await GrudgeHelper.ExecuteBlessingAsync(choiceContext, target, Owner.Creature, DynamicVars["Blessing"].IntValue, this);
            if (target.IsAlive)
            {
                await CreatureCmd.GainBlock(target, DynamicVars.Block, cardPlay);
            }
        }
    }

    public sealed class DrawTheOmen : HexerCardBase
    {
        public DrawTheOmen() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Omen", 1m), new CardsVar(2) };

        protected override void OnUpgrade() => DynamicVars["Omen"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int count = DynamicVars["Omen"].IntValue;
            var candidates = PileType.Draw.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse)
                .Concat(PileType.Discard.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse))
                .Take(count)
                .ToList();
            foreach (var curse in candidates)
            {
                await CardPileCmd.Add(curse, PileType.Draw, CardPilePosition.Top, this, skipVisuals: false);
            }
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }

    public sealed class BurnTheTalisman : HexerCardBase
    {
        public BurnTheTalisman() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
            var selected = (await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this)).ToList();
            if (selected.Count > 0)
            {
                var chosen = selected[0];
                bool isCurse = chosen.Type == CardType.Curse;
                await CardCmd.Exhaust(choiceContext, chosen, causedByEthereal: false, skipVisuals: false);
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
                if (isCurse)
                {
                    await PlayerCmd.GainEnergy(1m, Owner);
                    await CardPileCmd.Draw(choiceContext, 1m, Owner, fromHandDraw: false);
                }
            }
        }
    }

    public sealed class CalmMind : HexerCardBase
    {
        public CalmMind() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new CardsVar(2) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
            await EphemeralHex.GenerateAsync(Owner, 1, PileType.Discard, CardPilePosition.Bottom);
        }
    }
}

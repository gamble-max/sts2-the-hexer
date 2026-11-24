using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public sealed class BloodOfferingTalisman : HexerCardBase
    {
        public BloodOfferingTalisman() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<ShrinkPower>(1m), new EnergyVar(1), new CardsVar(2) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<ShrinkPower>() };

        protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<ShrinkPower>(choiceContext, Owner.Creature, DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, this, silent: false);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }

    public sealed class MirrorOfMisfortune : HexerCardBase
    {
        public MirrorOfMisfortune() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<VulnerablePower>(1m), new IntVar("Blessing", 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeBlessing: true);

        protected override void OnUpgrade() => DynamicVars["Blessing"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, silent: false);

            int weak = Owner.Creature.GetPower<WeakPower>()?.Amount ?? 0;
            int frail = Owner.Creature.GetPower<FrailPower>()?.Amount ?? 0;
            int shrink = Owner.Creature.GetPower<ShrinkPower>()?.Amount ?? 0;
            int vuln = Owner.Creature.GetPower<VulnerablePower>()?.Amount ?? 0;
            if (weak > 0 && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, weak, Owner.Creature, this, silent: false);
            }
            if (frail > 0 && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<FrailPower>(choiceContext, cardPlay.Target, frail, Owner.Creature, this, silent: false);
            }
            if (shrink > 0 && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<ShrinkPower>(choiceContext, cardPlay.Target, shrink, Owner.Creature, this, silent: false);
            }
            if (vuln > 0 && cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, vuln, Owner.Creature, this, silent: false);
            }
            await GrudgeHelper.ExecuteBlessingAsync(choiceContext, Owner.Creature, Owner.Creature, DynamicVars["Blessing"].IntValue, this);
        }
    }

    public sealed class CurseWeaving : HexerCardBase
    {
        public CurseWeaving() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(9m, ValueProp.Move), new CardsVar(2) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(4m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await EphemeralHex.GenerateAsync(Owner, DynamicVars.Cards.IntValue, PileType.Hand, CardPilePosition.Bottom);
        }
    }

    public sealed class LayeredBarrier : HexerCardBase
    {
        public LayeredBarrier() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(6m, ValueProp.Move), new IntVar("BonusBlock", 5m), new PowerVar<PsychokinesisPower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars["BonusBlock"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            var handCurses = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse).ToList();
            foreach (var c in handCurses)
            {
                await CardCmd.Exhaust(choiceContext, c, causedByEthereal: false, skipVisuals: false);
            }
            if (handCurses.Count > 0 && Owner.Creature.IsAlive)
            {
                await CreatureCmd.GainBlock(Owner.Creature, handCurses.Count * DynamicVars["BonusBlock"].BaseValue, ValueProp.Unpowered, cardPlay);
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, handCurses.Count * DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class DeepMeditation : HexerCardBase
    {
        public DeepMeditation() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(5m), new PowerVar<EnergyNextTurnPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => DynamicVars["PsychokinesisPower"].UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
            await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, DynamicVars["EnergyNextTurnPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class CondensedWill : HexerCardBase
    {
        public CondensedWill() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<RetainPsychokinesisBonusPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, silent: false);
        }
    }

    public sealed class RitualOfBurdens : HexerCardBase
    {
        public RitualOfBurdens() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<WeakPower>(1m), new PowerVar<FrailPower>(1m), new BlockVar(4m, ValueProp.Move), new CardsVar(2) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<FrailPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars["WeakPower"].UpgradeValueBy(1m);
            DynamicVars["FrailPower"].UpgradeValueBy(1m);
            DynamicVars.Block.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            decimal weak = DynamicVars["WeakPower"].BaseValue;
            decimal frail = DynamicVars["FrailPower"].BaseValue;
            foreach (var c in CombatState.Creatures)
            {
                if (c.IsAlive)
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, c, weak, Owner.Creature, this, silent: false);
                    await PowerCmd.Apply<FrailPower>(choiceContext, c, frail, Owner.Creature, this, silent: false);
                }
            }
            if (Owner.Creature.IsAlive)
            {
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }

    public sealed class SoulCleansing : HexerCardBase, IHexerEmpowerCard
    {
        public SoulCleansing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public bool IsPsychokinesisEmpowerEligible => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new HealVar(5m) };

        protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int curses = PileType.Hand.GetPile(Owner).Cards.Count(c => c.Type == CardType.Curse);
            decimal heal = DynamicVars.Heal.BaseValue + 2m * curses;
            if (Owner.Creature.IsAlive)
            {
                await CreatureCmd.Heal(Owner.Creature, heal, playAnim: true);
            }
        }
    }

    public sealed class HexTransfer : HexerCardBase
    {
        public HexTransfer() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<WeakPower>(2m), new PowerVar<VulnerablePower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>(), new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade()
        {
            DynamicVars["WeakPower"].UpgradeValueBy(1m);
            DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, silent: false);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, silent: false);
            await EphemeralHex.GenerateAsync(Owner, 1, PileType.Hand, CardPilePosition.Bottom);
        }
    }

    public sealed class GraveyardEcho : HexerCardBase
    {
        public GraveyardEcho() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(8m, ValueProp.Move), new CardsVar(2) };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars.Cards.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            var exhaustedCurses = PileType.Exhaust.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse).ToList();
            int count = Math.Min(DynamicVars.Cards.IntValue, exhaustedCurses.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = Owner.RunState.Rng.Niche.NextInt(exhaustedCurses.Count);
                var chosen = exhaustedCurses[idx];
                exhaustedCurses.RemoveAt(idx);
                await CardPileCmd.Add(chosen, PileType.Hand, CardPilePosition.Bottom, this, skipVisuals: false);
            }
        }
    }

    public sealed class SharedBurden : HexerCardBase
    {
        public SharedBurden() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly, true) { }
        public override bool GainsBlock => true;
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Cleanse", 3m), new BlockVar(3m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips().Concat(new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() });

        protected override void OnUpgrade() => DynamicVars["Cleanse"].UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            int k = await GrudgeHelper.RemoveRandomGrudgeDebuffsAsync(
                choiceContext,
                cardPlay.Target,
                Owner.Creature,
                DynamicVars["Cleanse"].IntValue,
                this);
            if (k > 0 && Owner.Creature.IsAlive)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, 2m * k, Owner.Creature, this, silent: false);
                for (int i = 0; i < k && Owner.Creature.IsAlive; i++)
                {
                    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
                }
            }
        }
    }

    public sealed class ChorusOfBlessings : HexerCardBase
    {
        public ChorusOfBlessings() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies, true) { }
        public override bool GainsBlock => true;
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Blessing", 1m), new BlockVar(5m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeBlessing: true);

        protected override void OnUpgrade()
        {
            DynamicVars["Blessing"].UpgradeValueBy(1m);
            DynamicVars.Block.UpgradeValueBy(3m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            int n = DynamicVars["Blessing"].IntValue;
            foreach (var ally in CombatState.PlayerCreatures)
            {
                if (ally.IsAlive)
                {
                    await GrudgeHelper.ExecuteBlessingAsync(choiceContext, ally, Owner.Creature, n, this);
                    await CreatureCmd.GainBlock(ally, DynamicVars.Block, cardPlay);
                }
            }
        }
    }
}

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
    public sealed class MindNeedle : HexerCardBase
    {
        public MindNeedle() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(3m, ValueProp.Move), new IntVar("Hits", 2m) };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(DynamicVars["Hits"].IntValue).Execute(choiceContext);
        }
    }

    public sealed class SweepingHex : HexerCardBase
    {
        public SweepingHex() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(6m, ValueProp.Move), new PowerVar<WeakPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingAllOpponents(CombatState).WithHitCount(1).Execute(choiceContext);
            foreach (var e in CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, e, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class CursedScythe : HexerCardBase
    {
        public CursedScythe() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(10m, ValueProp.Move), new CardsVar(1) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.Cards.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            await EphemeralHex.GenerateAsync(Owner, DynamicVars.Cards.IntValue, PileType.Discard, CardPilePosition.Bottom);
        }
    }

    public sealed class BlindThrust : HexerCardBase
    {
        public BlindThrust() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new CardsVar(1) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.Cards.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            bool hasGrudge = GrudgeHelper.GetGrudgeCount(cardPlay.Target) > 0;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            if (hasGrudge)
            {
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
            }
        }
    }

    public sealed class SoulDrainStrike : HexerCardBase, IHexerEmpowerCard
    {
        public SoulDrainStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }
        public bool IsPsychokinesisEmpowerEligible => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(12m, ValueProp.Move), new HealVar(4m) };

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars.Heal.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            if (Owner.Creature.IsAlive)
            {
                await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, playAnim: true);
            }
        }
    }

    public sealed class TripleNeedle : HexerCardBase
    {
        public TripleNeedle() : base(1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(3m, ValueProp.Move), new IntVar("Hits", 3m), new PowerVar<PsychokinesisPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(1m);
            DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingRandomOpponents(CombatState, allowDuplicates: true).WithHitCount(DynamicVars["Hits"].IntValue).Execute(choiceContext);
            var handCurses = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse).ToList();
            if (handCurses.Count > 0)
            {
                var chosen = handCurses[Owner.RunState.Rng.Niche.NextInt(handCurses.Count)];
                await CardCmd.Exhaust(choiceContext, chosen, causedByEthereal: false, skipVisuals: false);
                if (Owner.Creature.IsAlive)
                {
                    await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
                }
            }
        }
    }

    public sealed class HexedIronWave : HexerCardBase
    {
        public HexedIronWave() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(6m, ValueProp.Move), new DamageVar(6m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(2m);
            DynamicVars.Damage.UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            if (cardPlay.Target != null && cardPlay.Target.IsAlive)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            }
            await EphemeralHex.GenerateAsync(Owner, 1, PileType.Discard, CardPilePosition.Bottom);
        }
    }

    public sealed class GrudgeLash : HexerCardBase
    {
        public GrudgeLash() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(13m, ValueProp.Move), new PowerVar<FrailPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<FrailPower>() };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, DynamicVars["FrailPower"].BaseValue, Owner.Creature, this, silent: false);
            if (cardPlay.Target != null && cardPlay.Target.IsAlive)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            }
        }
    }

    public sealed class ExorcisingArc : HexerCardBase
    {
        public ExorcisingArc() : base(2, CardType.Attack, CardRarity.Common, TargetType.AllEnemies, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new IntVar("Exorcise", 1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingAllOpponents(CombatState).WithHitCount(1).Execute(choiceContext);
            int n = DynamicVars["Exorcise"].IntValue;
            foreach (var e in CombatState.HittableEnemies)
            {
                if (e.IsAlive)
                {
                    await GrudgeHelper.ExecuteExorciseAsync(choiceContext, e, Owner.Creature, n, this);
                }
            }
        }
    }
}

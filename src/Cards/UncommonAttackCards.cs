using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class PsychicBarrage : HexerCardBase
    {
        public PsychicBarrage() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
        protected override bool HasEnergyCostX => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(5m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            int x = ResolveEnergyXValue();
            if (x <= 0) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(x).Execute(choiceContext);
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, x, Owner.Creature, this, silent: false);
        }
    }

    public sealed class ContagiousMalice : HexerCardBase
    {
        public ContagiousMalice() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(8m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>() };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null || CombatState == null) return;
            int weak = cardPlay.Target.GetPower<WeakPower>()?.Amount ?? 0;
            int vuln = cardPlay.Target.GetPower<VulnerablePower>()?.Amount ?? 0;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);

            foreach (var enemy in CombatState.HittableEnemies)
            {
                if (enemy == cardPlay.Target || !enemy.IsAlive) continue;
                if (weak > 0) await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weak, Owner.Creature, this, silent: false);
                if (vuln > 0) await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vuln, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class ThornyEmbrace : HexerCardBase
    {
        public ThornyEmbrace() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<FrailPower>(1m), new BlockVar(10m, ValueProp.Move), new PowerVar<ThornyEmbracePower>(4m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<FrailPower>(), HoverTipFactory.FromPower<ThornsPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
            DynamicVars["ThornyEmbracePower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, DynamicVars["FrailPower"].BaseValue, Owner.Creature, this, silent: false);
            if (Owner.Creature.IsAlive)
            {
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
                await PowerCmd.Apply<ThornyEmbracePower>(choiceContext, Owner.Creature, DynamicVars["ThornyEmbracePower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class PhantomFlurry : HexerCardBase
    {
        public PhantomFlurry() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(5m, ValueProp.Move), new IntVar("Hits", 2m) };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var handCurses = PileType.Hand.GetPile(Owner).Cards.Where(c => c.Type == CardType.Curse).ToList();
            foreach (var c in handCurses)
            {
                await CardCmd.Exhaust(choiceContext, c, causedByEthereal: false, skipVisuals: false);
            }
            if (cardPlay.Target != null && cardPlay.Target.IsAlive)
            {
                int totalHits = DynamicVars["Hits"].IntValue + handCurses.Count;
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(totalHits).Execute(choiceContext);
            }
        }
    }

    public sealed class SeveringExorcism : HexerCardBase
    {
        public SeveringExorcism() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(12m, ValueProp.Move), new BlockVar(4m, ValueProp.Move), new IntVar("Exorcise", 2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars["Exorcise"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            if (cardPlay.Target.IsAlive)
            {
                int k = await GrudgeHelper.ExecuteExorciseAsync(choiceContext, cardPlay.Target, Owner.Creature, DynamicVars["Exorcise"].IntValue, this);
                for (int i = 0; i < k && Owner.Creature.IsAlive; i++)
                {
                    await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
                }
            }
        }
    }

    public sealed class AbyssalLance : HexerCardBase
    {
        public AbyssalLance() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(13m, ValueProp.Move), new PowerVar<ShrinkPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars["ShrinkPower"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            int hits = GrudgeHelper.GetGrudgeCount(cardPlay.Target) > 5 ? 2 : 1;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(hits).Execute(choiceContext);
            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<ShrinkPower>(choiceContext, cardPlay.Target, DynamicVars["ShrinkPower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class LeechingHex : HexerCardBase, IHexerEmpowerCard
    {
        public LeechingHex() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
        public bool IsPsychokinesisEmpowerEligible => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(8m, ValueProp.Move), new HealVar(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.Heal.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            int g = Math.Min(5, GrudgeHelper.GetGrudgeCount(cardPlay.Target));
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            for (int i = 0; i < g && Owner.Creature.IsAlive; i++)
            {
                await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, playAnim: i == 0);
            }
        }
    }

    public sealed class ShatteredVisorStrike : HexerCardBase
    {
        public ShatteredVisorStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new PowerVar<WeakPower>(1m), new PowerVar<VulnerablePower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars["WeakPower"].UpgradeValueBy(1m);
            DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            if (cardPlay.Target.IsAlive)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this, silent: false);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class ResonantWave : HexerCardBase
    {
        public ResonantWave() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(7m, ValueProp.Move), new IntVar("Hits", 2m) };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingAllOpponents(CombatState).WithHitCount(DynamicVars["Hits"].IntValue).Execute(choiceContext);
        }
    }

    public sealed class VengefulCounter : HexerCardBase
    {
        public VengefulCounter() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy, true) { }
        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(6m, ValueProp.Move), new BlockVar(3m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.Block.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            int selfGrudge = GrudgeHelper.GetGrudgeCount(Owner.Creature);
            for (int i = 0; i < selfGrudge && Owner.Creature.IsAlive; i++)
            {
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            }
        }
    }
}

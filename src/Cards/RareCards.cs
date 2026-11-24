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
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class PsychokineticCataclysm : HexerCardBase
    {
        public PsychokineticCataclysm() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(12m, ValueProp.Move), new IntVar("Extra", 3m) };

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars["Extra"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingAllOpponents(CombatState).WithHitCount(1).Execute(choiceContext);

            var psycho = Owner.Creature.GetPower<PsychokinesisPower>();
            if (psycho != null && psycho.Amount > 0)
            {
                int rem = psycho.Amount;
                await PowerCmd.Remove(psycho);
                await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, rem * DynamicVars["Extra"].BaseValue, ValueProp.Unpowered, Owner.Creature);
            }
        }
    }

    public sealed class KarmicSeverance : HexerCardBase
    {
        public KarmicSeverance() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(10m, ValueProp.Move), new IntVar("Exorcise", 4m), new DamageVar("BonusDmg", 6m, ValueProp.Move) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m);
            DynamicVars["Exorcise"].UpgradeValueBy(2m);
            DynamicVars["BonusDmg"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(1).Execute(choiceContext);
            if (cardPlay.Target.IsAlive)
            {
                int k = await GrudgeHelper.ExecuteExorciseAsync(choiceContext, cardPlay.Target, Owner.Creature, DynamicVars["Exorcise"].IntValue, this);
                if (k > 0 && cardPlay.Target.IsAlive)
                {
                    await DamageCmd.Attack(DynamicVars["BonusDmg"].BaseValue)
                        .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(k).Execute(choiceContext);
                }
            }
        }
    }

    public sealed class SoulReaperScythe : HexerCardBase, IHexerEmpowerCard
    {
        public SoulReaperScythe() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies, true) { }
        public bool IsPsychokinesisEmpowerEligible => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(9m, ValueProp.Move), new HealVar(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);
            DynamicVars.Heal.UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            int cursedEnemies = CombatState.HittableEnemies.Count(e => e.IsAlive && GrudgeHelper.GetGrudgeCount(e) > 0);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).TargetingAllOpponents(CombatState).WithHitCount(1).Execute(choiceContext);
            for (int i = 0; i < cursedEnemies && Owner.Creature.IsAlive; i++)
            {
                await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue, playAnim: i == 0);
            }
        }
    }

    public sealed class CursedSingularity : HexerCardBase
    {
        public CursedSingularity() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(5m, ValueProp.Move) };

        protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            int curseCount = PileType.Hand.GetPile(Owner).Cards.Count(c => c.Type == CardType.Curse)
                + PileType.Draw.GetPile(Owner).Cards.Count(c => c.Type == CardType.Curse)
                + PileType.Discard.GetPile(Owner).Cards.Count(c => c.Type == CardType.Curse)
                + PileType.Exhaust.GetPile(Owner).Cards.Count(c => c.Type == CardType.Curse);
            int hits = 1 + curseCount;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay).Targeting(cardPlay.Target).WithHitCount(hits).Execute(choiceContext);
        }
    }

    public sealed class ForbiddenRite : HexerCardBase
    {
        public ForbiddenRite() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new EnergyVar(2), new CardsVar(3), new PowerVar<PsychokinesisPower>(5m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1m);
            DynamicVars.Cards.UpgradeValueBy(1m);
            DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, 2m, Owner.Creature, this, silent: false);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class AwakenTheThreeCurses : HexerCardBase
    {
        public AwakenTheThreeCurses() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new CardsVar(3) };

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var targets = new List<CardModel>();
            targets.AddRange(PileType.Draw.GetPile(Owner).Cards.Where(c => c is CurseZhou || c is CurseZu || c is CurseYuan));
            targets.AddRange(PileType.Discard.GetPile(Owner).Cards.Where(c => c is CurseZhou || c is CurseZu || c is CurseYuan));
            targets.AddRange(PileType.Exhaust.GetPile(Owner).Cards.Where(c => c is CurseZhou || c is CurseZu || c is CurseYuan));

            foreach (var c in targets)
            {
                if (!c.IsUpgraded && c.IsUpgradable)
                {
                    CardCmd.Upgrade(c, CardPreviewStyle.None);
                }
                await CardPileCmd.Add(c, PileType.Draw, CardPilePosition.Top, this, skipVisuals: false);
            }
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner, fromHandDraw: false);
        }
    }

    public sealed class EternalShroud : HexerCardBase
    {
        public EternalShroud() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<IntangiblePower>(1m), new PowerVar<PsychokinesisPower>(4m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<IntangiblePower>(), HoverTipFactory.FromPower<PsychokinesisPower>(), new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade() => DynamicVars["PsychokinesisPower"].UpgradeValueBy(3m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, silent: false);
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
            await EphemeralHex.GenerateAsync(Owner, 2, PileType.Draw, CardPilePosition.Random);
        }
    }

    public sealed class TransmuteMalice : HexerCardBase
    {
        public TransmuteMalice() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (IsUpgraded)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, 3m, Owner.Creature, this, silent: false);
            }
            var psycho = Owner.Creature.GetPower<PsychokinesisPower>();
            if (psycho != null && psycho.Amount > 0)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, psycho.Amount, Owner.Creature, this, silent: false);
            }
        }
    }

    public sealed class SanctuaryOfSouls : HexerCardBase, IHexerEmpowerCard
    {
        public SanctuaryOfSouls() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies, true) { }
        public bool IsPsychokinesisEmpowerEligible => true;
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Blessing", 3m), new HealVar(5m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeBlessing: true).Concat(new IHoverTip[] { HoverTipFactory.FromPower<BufferPower>() });

        protected override void OnUpgrade()
        {
            DynamicVars["Blessing"].UpgradeValueBy(2m);
            DynamicVars.Heal.UpgradeValueBy(3m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null) return;
            int n = DynamicVars["Blessing"].IntValue;
            decimal heal = DynamicVars.Heal.BaseValue;
            foreach (var ally in CombatState.PlayerCreatures)
            {
                if (ally.IsAlive)
                {
                    await GrudgeHelper.ExecuteBlessingAsync(choiceContext, ally, Owner.Creature, n, this);
                    await PowerCmd.Apply<BufferPower>(choiceContext, ally, 1m, Owner.Creature, this, silent: false);
                    await CreatureCmd.Heal(ally, heal, playAnim: true);
                }
            }
        }
    }

    public sealed class BlindSeerForm : HexerCardBase
    {
        public BlindSeerForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<BlindSeerFormPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, silent: false);
        }
    }

    public sealed class UnendingGrudge : HexerCardBase
    {
        public UnendingGrudge() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true).Concat(new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() });

        protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<UnendingGrudgePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, silent: false);
        }
    }

    public sealed class VesselOfCurses : HexerCardBase
    {
        public VesselOfCurses() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<VesselOfCursesPower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => DynamicVars["VesselOfCursesPower"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<VesselOfCursesPower>(choiceContext, Owner.Creature, DynamicVars["VesselOfCursesPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class AscendantMedium : HexerCardBase
    {
        public AscendantMedium() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<AscendantMediumPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<DexterityPower>() };

        protected override void OnUpgrade() => DynamicVars["AscendantMediumPower"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<AscendantMediumPower>(choiceContext, Owner.Creature, DynamicVars["AscendantMediumPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }
}

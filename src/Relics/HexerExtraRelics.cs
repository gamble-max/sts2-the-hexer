using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Relics
{
    public sealed class AllSeeingCrown : RelicModel
    {
        private int _lastDrawRound = -1;

        public override RelicRarity Rarity => RelicRarity.Ancient;
        public override string PackedIconPath => "res://Hexer/images/relics/all_seeing_crown.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/all_seeing_crown.png";
        protected override string BigIconPath => "res://Hexer/images/relics/all_seeing_crown.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<PsychokinesisPower>(6m),
                new CardsVar(1)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() }
                .Concat(GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true));

        protected override void AfterCloned()
        {
            base.AfterCloned();
            _lastDrawRound = -1;
        }

        public override async Task BeforeCombatStart()
        {
            AssertMutable();
            _lastDrawRound = -1;
            if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            {
                return;
            }
            Flash();
            await PowerCmd.Apply<PsychokinesisPower>(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                6m,
                Owner.Creature,
                null,
                silent: false);
        }

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            {
                return;
            }
            if (card.Owner == Owner && card.Type == CardType.Curse)
            {
                int round = Owner.Creature.CombatState?.RoundNumber ?? 0;
                if (_lastDrawRound != round)
                {
                    AssertMutable();
                    _lastDrawRound = round;
                    Flash();
                    await CardPileCmd.Draw(choiceContext, 1m, Owner, fromHandDraw: false);
                }
            }
        }
    }

    public sealed class SunkenPrayerBeads : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Common;
        public override string PackedIconPath => "res://Hexer/images/relics/sunken_prayer_beads.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/sunken_prayer_beads.png";
        protected override string BigIconPath => "res://Hexer/images/relics/sunken_prayer_beads.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<WeakPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

        public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
        {
            if (player == Owner && combatState.RoundNumber == 1 && Owner?.Creature != null && Owner.Creature.IsAlive)
            {
                Flash();
                foreach (var enemy in combatState.HittableEnemies)
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 1m, Owner.Creature, null, silent: false);
                }
            }
        }
    }

    public sealed class ObsidianTuningFork : RelicModel
    {
        private bool _triggeredThisCombat;

        public override RelicRarity Rarity => RelicRarity.Uncommon;
        public override string PackedIconPath => "res://Hexer/images/relics/obsidian_tuning_fork.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/obsidian_tuning_fork.png";
        protected override string BigIconPath => "res://Hexer/images/relics/obsidian_tuning_fork.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new PowerVar<PsychokinesisPower>(4m),
                new CardsVar(1)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() }
                .Concat(GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true));

        protected override void AfterCloned()
        {
            base.AfterCloned();
            _triggeredThisCombat = false;
        }

        public override Task BeforeCombatStart()
        {
            AssertMutable();
            _triggeredThisCombat = false;
            return Task.CompletedTask;
        }

        public async Task TryTriggerAsync(PlayerChoiceContext choiceContext)
        {
            if (!_triggeredThisCombat && Owner?.Creature != null && Owner.Creature.IsAlive)
            {
                AssertMutable();
                _triggeredThisCombat = true;
                Flash();
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, 4m, Owner.Creature, null, silent: false);
                await CardPileCmd.Draw(choiceContext, 1m, Owner, fromHandDraw: false);
            }
        }
    }

    public sealed class IncenseCenser : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Uncommon;
        public override string PackedIconPath => "res://Hexer/images/relics/incense_censer.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/incense_censer.png";
        protected override string BigIconPath => "res://Hexer/images/relics/incense_censer.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(4m, ValueProp.Unpowered) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side == CombatSide.Player && Owner?.Creature != null && Owner.Creature.IsAlive && GrudgeHelper.GetGrudgeCount(Owner.Creature) > 0)
            {
                Flash();
                await CreatureCmd.GainBlock(Owner.Creature, 4m, ValueProp.Unpowered, null, fast: false);
            }
        }
    }

    public sealed class SoulboundMirror : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Rare;
        public override string PackedIconPath => "res://Hexer/images/relics/soulbound_mirror.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/soulbound_mirror.png";
        protected override string BigIconPath => "res://Hexer/images/relics/soulbound_mirror.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new DamageVar(4m, ValueProp.Unpowered) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);
    }

    public sealed class GoldenHairpin : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Rare;
        public override string PackedIconPath => "res://Hexer/images/relics/golden_hairpin.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/golden_hairpin.png";
        protected override string BigIconPath => "res://Hexer/images/relics/golden_hairpin.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() }
                .Concat(GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true));
    }

    public sealed class DredgedShrine : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Shop;
        public override string PackedIconPath => "res://Hexer/images/relics/dredged_shrine.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/dredged_shrine.png";
        protected override string BigIconPath => "res://Hexer/images/relics/dredged_shrine.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() }
                .Concat(GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true));

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (player == Owner && Owner?.Creature != null && Owner.Creature.IsAlive)
            {
                Flash();
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null, silent: false);
            }
        }
    }
}

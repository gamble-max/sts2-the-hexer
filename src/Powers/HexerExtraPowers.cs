using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Cards;
using HexerMod.Core;

namespace HexerMod.Powers
{
    public sealed class CurseYuanPower : TemporaryThornsPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<CurseYuan>();
    }

    public sealed class ThornyEmbracePower : TemporaryThornsPower
    {
        public override AbstractModel OriginModel => ModelDb.Card<ThornyEmbrace>();
    }

    public sealed class EchoingBellsPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
        {
            if (Owner == null || !Owner.IsAlive || card.Owner != Owner.Player)
            {
                return;
            }
            if (causedByEthereal || card.Type == CardType.Curse || card.Keywords.Contains(CardKeyword.Ethereal))
            {
                Flash();
                await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: false);
                if (Owner.CombatState != null)
                {
                    await CreatureCmd.Damage(choiceContext, Owner.CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
                }
            }
        }
    }

    public sealed class GrudgeSiphonPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature? applier,
            CardModel? cardSource)
        {
            if (Owner == null || !Owner.IsAlive || applier != Owner || amount <= 0)
            {
                return;
            }
            if (power.Owner != null && power.Owner.IsEnemy && power.Owner.IsAlive && GrudgeHelper.IsSpecificDebuff(power))
            {
                Flash();
                await CreatureCmd.Damage(choiceContext, power.Owner, Amount, ValueProp.Unpowered, Owner);
            }
        }
    }

    public sealed class CursedSanctuaryPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (Owner == null || !Owner.IsAlive || player != Owner.Player)
            {
                return;
            }
            Flash();
            await EphemeralHex.GenerateAsync(player, 1, PileType.Hand, CardPilePosition.Bottom);
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }
    }

    public sealed class BlindSeerFormPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            if (Owner == null || !Owner.IsAlive || player != Owner.Player || Owner.CombatState == null)
            {
                return;
            }
            Flash();
            foreach (var creature in Owner.CombatState.Creatures)
            {
                if (creature.IsAlive)
                {
                    await PowerCmd.Apply<WeakPower>(choiceContext, creature, Amount, Owner, null, silent: false);
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, creature, Amount, Owner, null, silent: false);
                }
            }
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner, 3m * Amount, Owner, null, silent: false);
        }
    }

    public sealed class HexLinkPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterDamageGiven(
            PlayerChoiceContext choiceContext,
            Creature? dealer,
            DamageResult result,
            ValueProp props,
            Creature target,
            CardModel? cardSource)
        {
            if (Owner == null || !Owner.IsAlive || dealer == null || dealer == Owner)
            {
                return;
            }
            if (dealer.IsPlayer && target.IsEnemy)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner, Amount, Owner, null, silent: true);
            }
        }
    }

    public sealed class RetainPsychokinesisBonusPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterSideTurnEnd(
            PlayerChoiceContext choiceContext,
            CombatSide side,
            IEnumerable<Creature> participants)
        {
            if (side == CombatSide.Player && Owner != null && Owner.IsAlive)
            {
                await PowerCmd.TickDownDuration(this);
            }
        }
    }

    public sealed class UnendingGrudgePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true, includeBlessing: true);
    }

    public sealed class VesselOfCursesPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (Owner == null || !Owner.IsAlive || Owner.Player == null || card.Owner != Owner.Player)
            {
                return;
            }
            if (card.Type == CardType.Curse)
            {
                Flash();
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner, Amount, Owner, null, silent: false);
                await CardPileCmd.Draw(choiceContext, 1m, Owner.Player, fromHandDraw: false);
            }
        }
    }

    public sealed class AscendantMediumPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using HexerMod.Cards;
using HexerMod.Core;
using HexerMod.Powers;
using HexerMod.Relics;

namespace HexerMod.Models
{
    public sealed class Hexer : CharacterModel
    {
        public override bool IsPlayable => true;
        public override bool ShouldReceiveCombatHooks => true;
        public override Color NameColor => new Color(0.32f, 0.88f, 0.80f, 1f);
        public override CharacterGender Gender => CharacterGender.Masculine;
        protected override CharacterModel UnlocksAfterRunAs => null!;
        public override int StartingHp => 72;
        public override int StartingGold => 99;

        public override CardPoolModel CardPool => ModelDb.CardPool<HexerCardPool>();
        public override RelicPoolModel RelicPool => ModelDb.RelicPool<HexerRelicPool>();
        public override PotionPoolModel PotionPool => ModelDb.PotionPool<HexerPotionPool>();

        public override IEnumerable<CardModel> StartingDeck => new CardModel[]
        {
            ModelDb.Card<StrikeHexer>(),
            ModelDb.Card<StrikeHexer>(),
            ModelDb.Card<StrikeHexer>(),
            ModelDb.Card<StrikeHexer>(),
            ModelDb.Card<DefendHexer>(),
            ModelDb.Card<DefendHexer>(),
            ModelDb.Card<DefendHexer>(),
            ModelDb.Card<DefendHexer>(),
            ModelDb.Card<CurseZhou>(),
            ModelDb.Card<CurseZu>(),
            ModelDb.Card<CurseYuan>()
        };

        public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
        {
            ModelDb.Relic<EyelessCrown>()
        };

        public override float AttackAnimDelay => 0.15f;
        public override float CastAnimDelay => 0.2f;

        public override List<string> GetArchitectAttackVfx() => new()
        {
            "res://vfx/vfx_slash"
        };

        protected override string CharacterSelectIconPath => "res://Hexer/images/char_select/hexer_icon.png";
        protected override string CharacterSelectLockedIconPath => "res://Hexer/images/char_select/hexer_icon.png";
        protected override string MapMarkerPath => "res://Hexer/images/char_select/hexer_map_marker.png";

        public override Color EnergyLabelOutlineColor => new Color(0.10f, 0.35f, 0.35f, 1f);
        public override Color DialogueColor => new Color(0.28f, 0.82f, 0.76f, 1f);
        public override Color MapDrawingColor => new Color(0.28f, 0.82f, 0.76f, 1f);
        public override Color RemoteTargetingLineColor => new Color(0.32f, 0.88f, 0.80f, 1f);
        public override Color RemoteTargetingLineOutline => new Color(0.08f, 0.20f, 0.24f, 1f);
        public override Color DreadNameColor => new Color(0.45f, 0.92f, 0.85f, 1f);

        public override string CharacterSelectSfx => "event:/sfx/characters/necrobinder/necrobinder_select";
        public override string DeathSfx => "event:/sfx/characters/necrobinder/necrobinder_die";
        public override string CharacterTransitionSfx => "event:/sfx/characters/necrobinder/necrobinder_transition";

        private static bool IsPsychokinesisUser(Creature? c)
        {
            return c != null && c.IsAlive && c.Player != null &&
                   (c.Player.Character is Hexer || c.GetPower<PsychokinesisPower>() != null);
        }

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card.Type == CardType.Curse && card.Owner?.Creature is { } owner && IsPsychokinesisUser(owner))
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, owner, 1m, owner, null, silent: true);
            }
        }

        public override async Task AfterDamageGiven(
            PlayerChoiceContext choiceContext,
            Creature? dealer,
            DamageResult result,
            ValueProp props,
            Creature target,
            CardModel? cardSource)
        {
            if (IsPsychokinesisUser(dealer) && dealer != target && result != null && result.TotalDamage > 0)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, dealer!, 1m, dealer, null, silent: true);
            }
        }

        public override async Task AfterDamageReceived(
            PlayerChoiceContext choiceContext,
            Creature target,
            DamageResult result,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource)
        {
            var victim = result?.Receiver ?? target;
            if (IsPsychokinesisUser(victim) && result != null && result.TotalDamage > 0)
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, victim, 1m, victim, null, silent: true);
            }
        }

        public override async Task AfterPowerAmountChanged(
            PlayerChoiceContext choiceContext,
            PowerModel power,
            decimal amount,
            Creature? applier,
            CardModel? cardSource)
        {
            if (IsPsychokinesisUser(applier) && amount > 0 && GrudgeHelper.IsSpecificDebuff(power))
            {
                await PowerCmd.Apply<PsychokinesisPower>(choiceContext, applier!, 1m, applier, null, silent: true);
            }
        }

        public override async Task BeforeCardPlayed(CardPlay cardPlay)
        {
            var owner = cardPlay?.Card?.Owner?.Creature;
            if (owner == null || !owner.IsAlive || !HexerCombatTracker.IsEmpowerEligibleCard(cardPlay!.Card))
            {
                return;
            }

            var psycho = owner.GetPower<PsychokinesisPower>();
            if (psycho == null || psycho.Amount <= 0)
            {
                return;
            }

            int bonus = (psycho.Amount + 1) / 2;
            int spend = HexerCombatTracker.GetSpendAmount(owner);
            if (bonus <= 0)
            {
                return;
            }

            HexerCombatTracker.RegisterEmpowerment(cardPlay, bonus);
            if (spend > 0)
            {
                var ctx = new BlockingPlayerChoiceContext();
                if (spend >= psycho.Amount)
                {
                    await PowerCmd.Remove(psycho);
                }
                else
                {
                    await PowerCmd.ModifyAmount(ctx, psycho, -spend, owner, cardPlay.Card, silent: false);
                }

                if (owner.Player?.GetRelic<GoldenHairpin>() is { } hairpin)
                {
                    hairpin.Flash();
                    await PowerCmd.Apply<PsychokinesisPower>(ctx, owner, 1m, owner, cardPlay.Card, silent: true);
                }

                if (owner.GetPower<AscendantMediumPower>() is { } asc && asc.Amount > 0)
                {
                    if (cardPlay.Card!.Type == CardType.Attack)
                    {
                        await PowerCmd.Apply<StrengthPower>(ctx, owner, asc.Amount, owner, cardPlay.Card, silent: false);
                    }
                    else if (cardPlay.Card.Type == CardType.Skill)
                    {
                        await PowerCmd.Apply<DexterityPower>(ctx, owner, asc.Amount, owner, cardPlay.Card, silent: false);
                    }
                }
            }
        }

        public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            HexerCombatTracker.ClearEmpowerment(cardPlay);
            return Task.CompletedTask;
        }

        public override decimal ModifyDamageAdditive(
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
            CardPlay? cardPlay)
        {
            if (dealer == null || cardSource == null || cardSource.Owner?.Creature != dealer || !props.HasFlag(ValueProp.Move))
            {
                return 0m;
            }
            if (!IsPsychokinesisUser(dealer))
            {
                return 0m;
            }

            int active = HexerCombatTracker.GetActiveEmpowerBonus(cardPlay, cardSource);
            if (active > 0)
            {
                return active;
            }

            if (cardPlay == null)
            {
                return HexerCombatTracker.GetPreviewEmpowerBonus(cardSource);
            }

            return 0m;
        }

        public override decimal ModifyBlockAdditive(
            Creature target,
            decimal block,
            ValueProp props,
            CardModel? cardSource,
            CardPlay? cardPlay)
        {
            var owner = cardSource?.Owner?.Creature;
            if (owner == null || !props.HasFlag(ValueProp.Move) || !IsPsychokinesisUser(owner))
            {
                return 0m;
            }

            int active = HexerCombatTracker.GetActiveEmpowerBonus(cardPlay, cardSource);
            if (active > 0)
            {
                return active;
            }

            if (cardPlay == null)
            {
                return HexerCombatTracker.GetPreviewEmpowerBonus(cardSource!);
            }

            return 0m;
        }
    }
}

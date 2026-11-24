using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Powers;

namespace HexerMod.Core
{
    /// <summary>
    /// Tracks active empowering CardPlay (when a Hexer plays an Attack or Skill with Attack/Block/Heal)
    /// and deduplicates debuff Psychokinesis gains per target creature within a single PowerCmd.Apply batch.
    /// </summary>
    public static class HexerCombatTracker
    {
        private static readonly Dictionary<CardPlay, int> _activeEmpowerByCardPlay = new();
        private static readonly HashSet<CardModel> _activeEmpowerByCard = new();
        private static readonly Dictionary<CardModel, int> _activeEmpowerBonusByCard = new();
        private static CardPlay? _currentCardPlay;

        public static void RegisterEmpowerment(CardPlay cardPlay, int bonus)
        {
            if (cardPlay == null || bonus <= 0)
            {
                return;
            }
            _currentCardPlay = cardPlay;
            _activeEmpowerByCardPlay[cardPlay] = bonus;
            if (cardPlay.Card != null)
            {
                _activeEmpowerByCard.Add(cardPlay.Card);
                _activeEmpowerBonusByCard[cardPlay.Card] = bonus;
            }
        }

        public static void ClearEmpowerment(CardPlay cardPlay)
        {
            if (cardPlay == null)
            {
                return;
            }
            if (_currentCardPlay == cardPlay)
            {
                _currentCardPlay = null;
            }
            _activeEmpowerByCardPlay.Remove(cardPlay);
            if (cardPlay.Card != null)
            {
                _activeEmpowerByCard.Remove(cardPlay.Card);
                _activeEmpowerBonusByCard.Remove(cardPlay.Card);
            }
        }

        public static int GetCurrentActiveBonus()
        {
            return _currentCardPlay != null && _activeEmpowerByCardPlay.TryGetValue(_currentCardPlay, out int bonus)
                ? bonus
                : 0;
        }

        public static int GetActiveEmpowerBonus(CardPlay? cardPlay, CardModel? cardSource)
        {
            if (cardPlay != null && _activeEmpowerByCardPlay.TryGetValue(cardPlay, out int bonus))
            {
                return bonus;
            }
            if (cardSource != null && _activeEmpowerBonusByCard.TryGetValue(cardSource, out int cardBonus))
            {
                return cardBonus;
            }
            return 0;
        }

        /// <summary>
        /// Preview bonus shown on card in hand/targeting before it is played.
        /// </summary>
        public static int GetPreviewEmpowerBonus(CardModel card)
        {
            if (card == null || !IsEmpowerEligibleCard(card))
            {
                return 0;
            }
            var creature = card.Owner?.Creature;
            if (creature == null)
            {
                return 0;
            }
            var psycho = creature.GetPower<PsychokinesisPower>();
            if (psycho == null || psycho.Amount <= 0)
            {
                return 0;
            }
            return (psycho.Amount + 1) / 2;
        }

        public static int GetSpendAmount(Creature creature)
        {
            var psycho = creature?.GetPower<PsychokinesisPower>();
            if (psycho == null || psycho.Amount <= 0)
            {
                return 0;
            }
            if (creature!.GetPower<RetainPsychokinesisBonusPower>() != null)
            {
                return 0;
            }
            if (creature.GetPower<MindOverloadPower>() != null)
            {
                return 1;
            }
            return (psycho.Amount + 1) / 2;
        }

        /// <summary>
        /// Only Attack or Skill cards that have Attack Damage, Gain Block, or Heal effects
        /// consume Psychokinesis and receive empowerment.
        /// </summary>
        public static bool IsEmpowerEligibleCard(CardModel? card)
        {
            if (card == null)
            {
                return false;
            }
            if (card.Type != CardType.Attack && card.Type != CardType.Skill)
            {
                return false;
            }
            if (card is IHexerEmpowerCard custom)
            {
                return custom.IsPsychokinesisEmpowerEligible;
            }
            if (card.DynamicVars != null)
            {
                if (card.DynamicVars.ContainsKey("Damage") ||
                    card.DynamicVars.ContainsKey("CalculatedDamage") ||
                    card.DynamicVars.ContainsKey("BonusDmg") ||
                    card.DynamicVars.ContainsKey("Block") ||
                    card.DynamicVars.ContainsKey("CalculatedBlock") ||
                    card.DynamicVars.ContainsKey("Heal"))
                {
                    return true;
                }
            }
            return card.GainsBlock;
        }

        public static void ClearAll()
        {
            _currentCardPlay = null;
            _activeEmpowerByCardPlay.Clear();
            _activeEmpowerByCard.Clear();
            _activeEmpowerBonusByCard.Clear();
        }
    }

    public interface IHexerEmpowerCard
    {
        bool IsPsychokinesisEmpowerEligible { get; }
    }
}

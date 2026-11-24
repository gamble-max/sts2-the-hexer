using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HexerMod.Powers
{
    /// <summary>
    /// Strength loss applied by [祓除 n].
    /// Marked PowerType.None so it is NOT a debuff, does NOT count as Grudge,
    /// does NOT trigger Psychokinesis gain, and cannot be cleansed.
    /// </summary>
    public sealed class ExorcisedStrengthLossPower : PowerModel
    {
        public override PowerType Type => PowerType.None;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override decimal ModifyDamageAdditive(
            Creature? target,
            decimal amount,
            ValueProp props,
            Creature? dealer,
            CardModel? cardSource,
            CardPlay? cardPlay)
        {
            if (dealer == Owner && props.IsPoweredAttack())
            {
                return -Amount;
            }
            return 0m;
        }
    }
}

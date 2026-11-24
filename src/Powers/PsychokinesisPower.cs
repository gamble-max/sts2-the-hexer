using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Core;

namespace HexerMod.Powers
{
    /// <summary>
    /// Stores the current Psychokinesis stack count on the creature.
    /// All passive gain triggers and empowerment modifiers are dispatched via Hexer's
    /// permanent CombatState hook subscriber so reaching 0 stacks never disables triggers
    /// or drops active 1->0 card empowerment.
    /// </summary>
    public sealed class PsychokinesisPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();
    }
}

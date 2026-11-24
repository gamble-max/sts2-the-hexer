using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace HexerMod.Powers
{
    /// <summary>
    /// [极咒深渊]: While active, any enemy with Weak + Vulnerable > 5 has their
    /// Weak and Vulnerable multipliers doubled (-25% -> -50%, +50% -> +100%).
    /// Implemented via Harmony Postfix on WeakPower & VulnerablePower.
    /// </summary>
    public sealed class AbyssalGazePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;
    }
}

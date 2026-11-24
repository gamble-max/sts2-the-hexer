using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace HexerMod.Powers
{
    /// <summary>
    /// [灵媒过载]: Psychokinesis consumption per empowered card is fixed to 1,
    /// while empowerment bonus remains ceil(Amount / 2).
    /// </summary>
    public sealed class MindOverloadPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Single;
    }
}

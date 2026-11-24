using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Powers;

namespace HexerMod.Relics
{
    public sealed class EyelessCrown : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Starter;
        public override string PackedIconPath => "res://Hexer/images/relics/eyeless_crown.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/eyeless_crown.png";
        protected override string BigIconPath => "res://Hexer/images/relics/eyeless_crown.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PsychokinesisPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        public override async Task BeforeCombatStart()
        {
            if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            {
                return;
            }
            Flash();
            await PowerCmd.Apply<PsychokinesisPower>(
                new BlockingPlayerChoiceContext(),
                Owner.Creature,
                3m,
                Owner.Creature,
                null,
                silent: false);
        }
    }
}

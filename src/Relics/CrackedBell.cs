using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace HexerMod.Relics
{
    public sealed class CrackedBell : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Uncommon;
        public override string PackedIconPath => "res://Hexer/images/relics/cracked_bell.png";
        protected override string PackedIconOutlinePath => "res://Hexer/images/relics/cracked_bell.png";
        protected override string BigIconPath => "res://Hexer/images/relics/cracked_bell.png";

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(3m, ValueProp.Unpowered) };

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (Owner?.Creature == null || !Owner.Creature.IsAlive)
            {
                return;
            }
            if (card.Owner == Owner && card.Type == CardType.Curse)
            {
                Flash();
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null, fast: false);
            }
        }
    }
}

using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace HexerMod.Cards
{
    public abstract class HexerCardBase : CardModel
    {
        protected HexerCardBase(
            int canonicalEnergyCost,
            CardType type,
            CardRarity rarity,
            TargetType targetType,
            bool shouldShowInCardLibrary = true)
            : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
        {
        }

        protected virtual string CustomPortraitName => Id.Entry.ToLowerInvariant();

        public string CustomPortraitPath => $"res://Hexer/images/cards/{CustomPortraitName}.png";
    }
}

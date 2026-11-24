using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace HexerMod.Cards
{
    /// <summary>
    /// 【残咒】: Generated Token Curse. Unplayable, no cost, Ethereal.
    /// When drawn, lose 1 HP 2 times.
    /// </summary>
    public sealed class EphemeralHex : HexerCardBase
    {
        public EphemeralHex() : base(-1, CardType.Curse, CardRarity.Token, TargetType.None, shouldShowInCardLibrary: false) { }

        public override int MaxUpgradeLevel => 0;
        public override bool CanBeGeneratedInCombat => false;
        public override bool CanBeGeneratedByModifiers => false;

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
        {
            CardKeyword.Unplayable,
            CardKeyword.Ethereal
        };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[]
            {
                new HpLossVar(1m),
                new IntVar("Hits", 2m)
            };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            System.Array.Empty<IHoverTip>();

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card != this || Owner?.Creature == null || !Owner.Creature.IsAlive)
            {
                return;
            }
            await Cmd.Wait(0.2f);
            int hits = DynamicVars["Hits"].IntValue;
            decimal hpLoss = DynamicVars.HpLoss.BaseValue;
            for (int i = 0; i < hits; i++)
            {
                if (Owner?.Creature == null || !Owner.Creature.IsAlive)
                {
                    break;
                }
                await CreatureCmd.Damage(
                    choiceContext,
                    Owner.Creature,
                    hpLoss,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    this,
                    null);
            }
        }

        public static async Task<IReadOnlyList<CardPileAddResult>> GenerateAsync(
            Player owner,
            int count,
            PileType pileType,
            CardPilePosition position,
            bool upgraded = false)
        {
            if (owner?.Creature?.CombatState == null || count <= 0)
            {
                return System.Array.Empty<CardPileAddResult>();
            }
            var list = new List<CardModel>(count);
            for (int i = 0; i < count; i++)
            {
                var c = owner.Creature.CombatState.CreateCard<EphemeralHex>(owner);
                if (upgraded && c.IsUpgradable)
                {
                    CardCmd.Upgrade(c, CardPreviewStyle.None);
                }
                list.Add(c);
            }
            return await CardPileCmd.AddGeneratedCardsToCombat(list, pileType, owner, position);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    /// <summary>
    /// 【怨】: Unplayable, no cost, Eternal, Ethereal (removed on upgrade).
    /// When drawn, ALL units (allies + enemies) gain 3 Temporary Thorns (this turn).
    /// </summary>
    public sealed class CurseYuan : HexerCardBase
    {
        public CurseYuan() : base(-1, CardType.Curse, CardRarity.Curse, TargetType.None, true) { }

        protected override string CustomPortraitName => "curse_yuan";
        public override int MaxUpgradeLevel => 1;
        public override bool CanBeGeneratedInCombat => false;
        public override bool CanBeGeneratedByModifiers => false;

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[]
        {
            CardKeyword.Unplayable,
            CardKeyword.Eternal,
            CardKeyword.Ethereal
        };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<CurseYuanPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<ThornsPower>() };

        protected override void OnUpgrade()
        {
            RemoveKeyword(CardKeyword.Ethereal);
        }

        public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
        {
            if (card != this || Owner?.Creature?.CombatState == null)
            {
                return;
            }
            var combatState = Owner.Creature.CombatState;
            foreach (var creature in combatState.Creatures)
            {
                if (creature.IsAlive)
                {
                    await PowerCmd.Apply<CurseYuanPower>(
                        choiceContext,
                        creature,
                        3m,
                        Owner.Creature,
                        this,
                        silent: false);
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using HexerMod.Core;

namespace HexerMod.Cards
{
    public sealed class GreatExorcism : HexerCardBase
    {
        public GreatExorcism() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies, true) { }
        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new IntVar("Exorcise", 3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips(includeExorcise: true);

        protected override void OnUpgrade()
        {
            DynamicVars["Exorcise"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (CombatState == null)
            {
                return;
            }
            int n = DynamicVars["Exorcise"].IntValue;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await GrudgeHelper.ExecuteExorciseAsync(choiceContext, enemy, Owner.Creature, n, this);
            }
        }
    }
}

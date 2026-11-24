using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace HexerMod.Cards
{
    public sealed class CursedBarrier : HexerCardBase
    {
        public CursedBarrier() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self, true) { }

        public override bool GainsBlock => true;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new BlockVar(8m, ValueProp.Move) };

        protected override void OnUpgrade()
        {
            DynamicVars.Block.UpgradeValueBy(3m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

            if (CombatState == null || Owner.Creature.Block <= 0)
            {
                return;
            }
            var enemies = CombatState.HittableEnemies;
            if (enemies.Count == 0)
            {
                return;
            }
            int idx = Owner.RunState.Rng.Niche.NextInt(enemies.Count);
            var target = enemies[idx];
            await DamageCmd.Attack(Owner.Creature.Block)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitCount(1)
                .Execute(choiceContext);
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Cards
{
    public sealed class EchoingBells : HexerCardBase
    {
        public EchoingBells() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<EchoingBellsPower>(3m) };

        protected override void OnUpgrade() => DynamicVars["EchoingBellsPower"].UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<EchoingBellsPower>(choiceContext, Owner.Creature, DynamicVars["EchoingBellsPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class GrudgeSiphon : HexerCardBase
    {
        public GrudgeSiphon() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<GrudgeSiphonPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            GrudgeHelper.GetSpecificDebuffHoverTips();

        protected override void OnUpgrade() => DynamicVars["GrudgeSiphonPower"].UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<GrudgeSiphonPower>(choiceContext, Owner.Creature, DynamicVars["GrudgeSiphonPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class CursedSanctuary : HexerCardBase
    {
        public CursedSanctuary() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<CursedSanctuaryPower>(2m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>(), new CardHoverTip(ModelDb.Card<EphemeralHex>()) };

        protected override void OnUpgrade() => DynamicVars["CursedSanctuaryPower"].UpgradeValueBy(2m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<CursedSanctuaryPower>(choiceContext, Owner.Creature, DynamicVars["CursedSanctuaryPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class ObsidianAegis : HexerCardBase
    {
        public ObsidianAegis() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<PlatingPower>(4m), new PowerVar<PsychokinesisPower>(3m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars["PlatingPower"].UpgradeValueBy(2m);
            DynamicVars["PsychokinesisPower"].UpgradeValueBy(2m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<PlatingPower>(choiceContext, Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this, silent: false);
            await PowerCmd.Apply<PsychokinesisPower>(choiceContext, Owner.Creature, DynamicVars["PsychokinesisPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class PainfulAttunement : HexerCardBase
    {
        public PainfulAttunement() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<ThornsPower>(3m), new PowerVar<DexterityPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<ThornsPower>(), HoverTipFactory.FromPower<DexterityPower>() };

        protected override void OnUpgrade()
        {
            DynamicVars["ThornsPower"].UpgradeValueBy(1m);
            DynamicVars["DexterityPower"].UpgradeValueBy(1m);
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this, silent: false);
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }

    public sealed class HexLink : HexerCardBase
    {
        public HexLink() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self, true) { }
        public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

        protected override IEnumerable<DynamicVar> CanonicalVars =>
            new DynamicVar[] { new PowerVar<HexLinkPower>(1m) };

        protected override IEnumerable<IHoverTip> ExtraHoverTips =>
            new IHoverTip[] { HoverTipFactory.FromPower<PsychokinesisPower>() };

        protected override void OnUpgrade() => DynamicVars["HexLinkPower"].UpgradeValueBy(1m);

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await PowerCmd.Apply<HexLinkPower>(choiceContext, Owner.Creature, DynamicVars["HexLinkPower"].BaseValue, Owner.Creature, this, silent: false);
        }
    }
}

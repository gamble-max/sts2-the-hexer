using Godot;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Cards;

namespace HexerMod.Models
{
    public sealed class HexerCardPool : CardPoolModel
    {
        public override string Title => "hexer";
        public override string EnergyColorName => "defect";
        public override string CardFrameMaterialPath => "card_frame_blue";
        public override Color DeckEntryCardColor => new Color(0.16f, 0.56f, 0.52f, 1f);
        public override Color EnergyOutlineColor => new Color(0.25f, 0.85f, 0.78f, 1f);
        public override bool IsColorless => false;

        protected override CardModel[] GenerateAllCards() => new CardModel[]
        {
            // Starter (5)
            ModelDb.Card<StrikeHexer>(),
            ModelDb.Card<DefendHexer>(),
            ModelDb.Card<CurseZhou>(),
            ModelDb.Card<CurseZu>(),
            ModelDb.Card<CurseYuan>(),

            // Common (22 = 6 existing + 16 new)
            ModelDb.Card<SpiritSlash>(),
            ModelDb.Card<SoulWard>(),
            ModelDb.Card<GrudgePulse>(),
            ModelDb.Card<ShadowWhisper>(),
            ModelDb.Card<ExorciseStrike>(),
            ModelDb.Card<WraithTalisman>(),
            ModelDb.Card<MindNeedle>(),
            ModelDb.Card<SweepingHex>(),
            ModelDb.Card<CursedScythe>(),
            ModelDb.Card<BlindThrust>(),
            ModelDb.Card<SoulDrainStrike>(),
            ModelDb.Card<TripleNeedle>(),
            ModelDb.Card<HexedIronWave>(),
            ModelDb.Card<GrudgeLash>(),
            ModelDb.Card<ExorcisingArc>(),
            ModelDb.Card<BlindStep>(),
            ModelDb.Card<Hexbrand>(),
            ModelDb.Card<MireShroud>(),
            ModelDb.Card<WhisperingPrayer>(),
            ModelDb.Card<DrawTheOmen>(),
            ModelDb.Card<BurnTheTalisman>(),
            ModelDb.Card<CalmMind>(),

            // Uncommon (32 = 4 existing + 28 new)
            ModelDb.Card<GrudgeResonance>(),
            ModelDb.Card<PurifyingPrayer>(),
            ModelDb.Card<CursedBarrier>(),
            ModelDb.Card<MindOverload>(),
            ModelDb.Card<PsychicBarrage>(),
            ModelDb.Card<ContagiousMalice>(),
            ModelDb.Card<ThornyEmbrace>(),
            ModelDb.Card<PhantomFlurry>(),
            ModelDb.Card<SeveringExorcism>(),
            ModelDb.Card<AbyssalLance>(),
            ModelDb.Card<LeechingHex>(),
            ModelDb.Card<ShatteredVisorStrike>(),
            ModelDb.Card<ResonantWave>(),
            ModelDb.Card<VengefulCounter>(),
            ModelDb.Card<BloodOfferingTalisman>(),
            ModelDb.Card<MirrorOfMisfortune>(),
            ModelDb.Card<CurseWeaving>(),
            ModelDb.Card<LayeredBarrier>(),
            ModelDb.Card<DeepMeditation>(),
            ModelDb.Card<CondensedWill>(),
            ModelDb.Card<RitualOfBurdens>(),
            ModelDb.Card<SoulCleansing>(),
            ModelDb.Card<HexTransfer>(),
            ModelDb.Card<GraveyardEcho>(),
            ModelDb.Card<SharedBurden>(),
            ModelDb.Card<ChorusOfBlessings>(),
            ModelDb.Card<EchoingBells>(),
            ModelDb.Card<GrudgeSiphon>(),
            ModelDb.Card<CursedSanctuary>(),
            ModelDb.Card<ObsidianAegis>(),
            ModelDb.Card<PainfulAttunement>(),
            ModelDb.Card<HexLink>(),

            // Rare (16 = 3 existing + 13 new)
            ModelDb.Card<AbyssalGaze>(),
            ModelDb.Card<GreatExorcism>(),
            ModelDb.Card<TenThousandGrudges>(),
            ModelDb.Card<PsychokineticCataclysm>(),
            ModelDb.Card<KarmicSeverance>(),
            ModelDb.Card<SoulReaperScythe>(),
            ModelDb.Card<CursedSingularity>(),
            ModelDb.Card<ForbiddenRite>(),
            ModelDb.Card<AwakenTheThreeCurses>(),
            ModelDb.Card<EternalShroud>(),
            ModelDb.Card<TransmuteMalice>(),
            ModelDb.Card<SanctuaryOfSouls>(),
            ModelDb.Card<BlindSeerForm>(),
            ModelDb.Card<UnendingGrudge>(),
            ModelDb.Card<VesselOfCurses>(),
            ModelDb.Card<AscendantMedium>()
        };
    }
}

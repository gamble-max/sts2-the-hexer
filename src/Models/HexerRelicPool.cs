using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Relics;

namespace HexerMod.Models
{
    public sealed class HexerRelicPool : RelicPoolModel
    {
        public override string EnergyColorName => "defect";
        public override Color LabOutlineColor => new Color(0.25f, 0.85f, 0.78f, 1f);

        protected override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[]
        {
            ModelDb.Relic<EyelessCrown>(),
            ModelDb.Relic<AllSeeingCrown>(),
            ModelDb.Relic<SunkenPrayerBeads>(),
            ModelDb.Relic<CrackedBell>(),
            ModelDb.Relic<ObsidianTuningFork>(),
            ModelDb.Relic<IncenseCenser>(),
            ModelDb.Relic<SoulboundMirror>(),
            ModelDb.Relic<GoldenHairpin>(),
            ModelDb.Relic<DredgedShrine>()
        };
    }
}

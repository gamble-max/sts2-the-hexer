using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using HexerMod.Potions;

namespace HexerMod.Models
{
    public sealed class HexerPotionPool : PotionPoolModel
    {
        public override string EnergyColorName => "defect";
        public override Color LabOutlineColor => new Color(0.25f, 0.85f, 0.78f, 1f);

        protected override IEnumerable<PotionModel> GenerateAllPotions() => new PotionModel[]
        {
            ModelDb.Potion<SpiritTears>(),
            ModelDb.Potion<GrudgeDraught>(),
            ModelDb.Potion<AbyssalElixir>()
        };
    }
}

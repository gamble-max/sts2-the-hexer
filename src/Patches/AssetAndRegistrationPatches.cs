using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Unlocks;
using Sts2Mods.Shared;
using HexerMod.Cards;
using HexerMod.Models;
using HexerMod.Powers;
using HexerMod.Relics;
using HexerMod.Visuals;

namespace HexerMod.Patches
{
    [HarmonyPatch]
    public static class RegistrationPatches
    {
        private static readonly string[] ModelDbCacheFields =
        {
            "_allCards",
            "_allCardPools",
            "_allCharacterCardPools",
            "_allRelics",
            "_allRelicPools",
            "_allCharacterRelicPools",
            "_allPotions",
            "_allPotionPools",
            "_allCharacterPotionPools",
            "_allPowers"
        };

        public static void InvalidateModelDbCaches()
        {
            foreach (string fieldName in ModelDbCacheFields)
            {
                AccessTools.Field(typeof(ModelDb), fieldName)?.SetValue(null, null);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
        [HarmonyPostfix]
        public static void ModelDbInitPostfix()
        {
            InvalidateModelDbCaches();
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacters), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllCharactersPostfix(ref IEnumerable<CharacterModel> __result)
        {
            if (!ModelDb.Contains(typeof(Hexer)))
            {
                return;
            }
            var hexer = ModelDb.Character<Hexer>();
            if (!__result.Contains(hexer))
            {
                __result = __result.Append(hexer);
            }
        }

        [HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Characters), MethodType.Getter)]
        [HarmonyPostfix]
        public static void UnlockStateCharactersPostfix(ref IEnumerable<CharacterModel> __result)
        {
            if (!ModelDb.Contains(typeof(Hexer)))
            {
                return;
            }
            var hexer = ModelDb.Character<Hexer>();
            if (!__result.Contains(hexer))
            {
                __result = __result.Append(hexer);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCardPools), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllCardPoolsPostfix(ref IEnumerable<CardPoolModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerCardPool)))
            {
                return;
            }
            var pool = ModelDb.CardPool<HexerCardPool>();
            if (!__result.Contains(pool))
            {
                __result = __result.Append(pool);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterCardPools), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllCharacterCardPoolsPostfix(ref IEnumerable<CardPoolModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerCardPool)))
            {
                return;
            }
            var pool = ModelDb.CardPool<HexerCardPool>();
            if (!__result.Contains(pool))
            {
                __result = __result.Append(pool);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCards), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllCardsPostfix(ref IEnumerable<CardModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerCardPool)))
            {
                return;
            }
            var list = __result.ToList();
            bool changed = false;
            foreach (var c in ModelDb.CardPool<HexerCardPool>().AllCards)
            {
                if (!list.Contains(c))
                {
                    list.Add(c);
                    changed = true;
                }
            }
            if (ModelDb.Contains(typeof(EphemeralHex)))
            {
                var token = ModelDb.Card<EphemeralHex>();
                if (!list.Contains(token))
                {
                    list.Add(token);
                    changed = true;
                }
            }
            if (changed)
            {
                __result = list;
            }
        }

        [HarmonyPatch(typeof(TokenCardPool), "GenerateAllCards")]
        [HarmonyPostfix]
        public static void TokenCardPoolGenerateAllCardsPostfix(ref CardModel[] __result)
        {
            if (!ModelDb.Contains(typeof(EphemeralHex)))
            {
                return;
            }
            var token = ModelDb.Card<EphemeralHex>();
            if (!__result.Contains(token))
            {
                var list = __result.ToList();
                list.Add(token);
                __result = list.ToArray();
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelicPools), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllRelicPoolsPostfix(ref IEnumerable<RelicPoolModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerRelicPool)))
            {
                return;
            }
            var pool = ModelDb.RelicPool<HexerRelicPool>();
            if (!__result.Contains(pool))
            {
                __result = __result.Append(pool);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCharacterRelicPools), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllCharacterRelicPoolsPostfix(ref IEnumerable<RelicPoolModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerRelicPool)))
            {
                return;
            }
            var pool = ModelDb.RelicPool<HexerRelicPool>();
            if (!__result.Contains(pool))
            {
                __result = __result.Append(pool);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllRelics), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllRelicsPostfix(ref IEnumerable<RelicModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerRelicPool)))
            {
                return;
            }
            var list = __result.ToList();
            bool changed = false;
            foreach (var r in ModelDb.RelicPool<HexerRelicPool>().AllRelics)
            {
                if (!list.Contains(r))
                {
                    list.Add(r);
                    changed = true;
                }
            }
            if (changed)
            {
                __result = list;
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllPotionPools), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllPotionPoolsPostfix(ref IEnumerable<PotionPoolModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerPotionPool)))
            {
                return;
            }
            var pool = ModelDb.PotionPool<HexerPotionPool>();
            if (!__result.Contains(pool))
            {
                __result = __result.Append(pool);
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllPotions), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllPotionsPostfix(ref IEnumerable<PotionModel> __result)
        {
            if (!ModelDb.Contains(typeof(HexerPotionPool)))
            {
                return;
            }
            var list = __result.ToList();
            bool changed = false;
            foreach (var p in ModelDb.PotionPool<HexerPotionPool>().AllPotions)
            {
                if (!list.Contains(p))
                {
                    list.Add(p);
                    changed = true;
                }
            }
            if (changed)
            {
                __result = list;
            }
        }

        [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllPowers), MethodType.Getter)]
        [HarmonyPostfix]
        public static void AllPowersPostfix(ref IEnumerable<PowerModel> __result)
        {
            if (!ModelDb.Contains(typeof(PsychokinesisPower)))
            {
                return;
            }
            var list = __result.ToList();
            PowerModel[] custom =
            {
                ModelDb.Power<PsychokinesisPower>(),
                ModelDb.Power<CurseYuanPower>(),
                ModelDb.Power<ThornyEmbracePower>(),
                ModelDb.Power<ExorcisedStrengthLossPower>(),
                ModelDb.Power<AbyssalGazePower>(),
                ModelDb.Power<MindOverloadPower>(),
                ModelDb.Power<EchoingBellsPower>(),
                ModelDb.Power<GrudgeSiphonPower>(),
                ModelDb.Power<CursedSanctuaryPower>(),
                ModelDb.Power<BlindSeerFormPower>(),
                ModelDb.Power<HexLinkPower>(),
                ModelDb.Power<RetainPsychokinesisBonusPower>(),
                ModelDb.Power<UnendingGrudgePower>(),
                ModelDb.Power<VesselOfCursesPower>(),
                ModelDb.Power<AscendantMediumPower>()
            };
            bool changed = false;
            foreach (var p in custom)
            {
                if (!list.Contains(p))
                {
                    list.Add(p);
                    changed = true;
                }
            }
            if (changed)
            {
                __result = list;
            }
        }

        [HarmonyPatch(typeof(TouchOfOrobas), "RefinementUpgrades", MethodType.Getter)]
        [HarmonyPostfix]
        public static void RefinementUpgradesPostfix(ref Dictionary<ModelId, RelicModel> __result)
        {
            if (!ModelDb.Contains(typeof(EyelessCrown)) || !ModelDb.Contains(typeof(AllSeeingCrown)))
            {
                return;
            }
            var starter = ModelDb.Relic<EyelessCrown>();
            var upgraded = ModelDb.Relic<AllSeeingCrown>();
            if (__result != null && !__result.ContainsKey(starter.Id))
            {
                __result[starter.Id] = upgraded;
            }
        }
    }

    [HarmonyPatch]
    public static class AssetPatches
    {
        public static bool TryResolveHexerResource(string path, out Resource? resource)
        {
            resource = null;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (path.StartsWith("res://Hexer/", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var tex = ModAssetHelper.LoadTexture(path);
                if (tex != null)
                {
                    resource = tex;
                    return true;
                }
            }

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                string fileName = System.IO.Path.GetFileName(path);

                if (fileName.Equals("character_icon_hexer_outline.png", StringComparison.OrdinalIgnoreCase))
                {
                    var tex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_badge_outline.png");
                    if (tex != null)
                    {
                        resource = tex;
                        return true;
                    }
                }
                if (fileName.Equals("character_icon_hexer.png", StringComparison.OrdinalIgnoreCase))
                {
                    var tex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_badge.png");
                    if (tex != null)
                    {
                        resource = tex;
                        return true;
                    }
                }

                string? folder = null;
                if (path.Contains("/powers/", StringComparison.OrdinalIgnoreCase)) folder = "powers";
                else if (path.Contains("/potions/", StringComparison.OrdinalIgnoreCase)) folder = "potions";
                else if (path.Contains("/relics/", StringComparison.OrdinalIgnoreCase)) folder = "relics";

                if (folder != null && !string.IsNullOrEmpty(fileName))
                {
                    string modPath = $"res://Hexer/images/{folder}/{fileName}";
                    var tex = ModAssetHelper.LoadTexture(modPath);
                    if (tex != null)
                    {
                        resource = tex;
                        return true;
                    }
                }

                if (path.Contains("/card_portraits/hexer/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(fileName))
                {
                    var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/cards/{fileName}")
                              ?? ModAssetHelper.LoadTexture("res://Hexer/images/cards/hexer_scene.png");
                    if (tex != null)
                    {
                        resource = tex;
                        return true;
                    }
                }
            }

            if (path.Contains("hexer", StringComparison.OrdinalIgnoreCase))
            {
                if (path.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                {
                    string fallback = path.Replace("hexer", "necrobinder", StringComparison.OrdinalIgnoreCase);
                    if (ResourceLoader.Exists(fallback))
                    {
                        resource = ResourceLoader.Load(fallback);
                        return resource != null;
                    }
                }
                else if ((path.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
                          path.Contains("char_select", StringComparison.OrdinalIgnoreCase)) &&
                         path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    var tex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_icon.png");
                    if (tex != null)
                    {
                        resource = tex;
                        return true;
                    }
                }
                else if (path.Contains("arm_", StringComparison.OrdinalIgnoreCase))
                {
                    string fallback = path.Replace("hexer", "necrobinder", StringComparison.OrdinalIgnoreCase);
                    if (ResourceLoader.Exists(fallback))
                    {
                        resource = ResourceLoader.Load(fallback);
                        return resource != null;
                    }
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(AssetCache), "LoadAsset", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        public static bool LoadAssetPrefix(AssetCache __instance, string path, ref Resource? __result)
        {
            if (TryResolveHexerResource(path, out var res) && res != null)
            {
                __instance.SetAsset(path, res);
                __result = res;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(AssetCache), nameof(AssetCache.CreateSession))]
        [HarmonyPrefix]
        public static void CreateSessionPrefix(AssetCache __instance, ref IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return;
            }
            var remaining = new List<string>();
            foreach (string p in paths)
            {
                if (TryResolveHexerResource(p, out var res) && res != null)
                {
                    __instance.SetAsset(p, res);
                }
                else
                {
                    remaining.Add(p);
                }
            }
            paths = remaining;
        }
    }

    [HarmonyPatch]
    public static class ModelIconPatches
    {
        private static readonly ConcurrentDictionary<string, AtlasTexture> AtlasCache = new();
        private static readonly Assembly HexerAsm = typeof(Hexer).Assembly;

        private static bool IsHexerModel(AbstractModel model) => model.GetType().Assembly == HexerAsm;

        private static string? ResolveSpriteModPath(string atlasName, string spriteName)
        {
            string clean = spriteName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? spriteName.Substring(0, spriteName.Length - 4)
                : spriteName;

            if (atlasName.StartsWith("relic_outline_atlas", StringComparison.OrdinalIgnoreCase))
            {
                return $"res://Hexer/images/relics/{clean}_outline.png";
            }
            if (atlasName.StartsWith("relic_atlas", StringComparison.OrdinalIgnoreCase))
            {
                return $"res://Hexer/images/relics/{clean}.png";
            }
            if (atlasName.StartsWith("potion_outline_atlas", StringComparison.OrdinalIgnoreCase))
            {
                return $"res://Hexer/images/potions/{clean}_outline.png";
            }
            if (atlasName.StartsWith("potion_atlas", StringComparison.OrdinalIgnoreCase))
            {
                return $"res://Hexer/images/potions/{clean}.png";
            }
            if (atlasName.StartsWith("power_atlas", StringComparison.OrdinalIgnoreCase))
            {
                return $"res://Hexer/images/powers/{clean}.png";
            }
            if (atlasName.StartsWith("card_atlas", StringComparison.OrdinalIgnoreCase) &&
                clean.StartsWith("hexer/", StringComparison.OrdinalIgnoreCase))
            {
                string cardEntry = clean.Substring("hexer/".Length);
                if (ModelDb.Contains(typeof(HexerCardPool)))
                {
                    var match = ModelDb.CardPool<HexerCardPool>().AllCards
                        .OfType<HexerCardBase>()
                        .FirstOrDefault(c => c.Id.Entry.Equals(cardEntry, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        return match.CustomPortraitPath;
                    }
                }
                if (ModelDb.Contains(typeof(EphemeralHex)) &&
                    ModelDb.Card<EphemeralHex>() is HexerCardBase token &&
                    token.Id.Entry.Equals(cardEntry, StringComparison.OrdinalIgnoreCase))
                {
                    return token.CustomPortraitPath;
                }
                string candidate = $"res://Hexer/images/cards/{cardEntry}.png";
                return ModAssetHelper.LoadTexture(candidate) != null
                    ? candidate
                    : "res://Hexer/images/cards/hexer_scene.png";
            }
            return null;
        }

        private static AtlasTexture? GetOrCreateAtlasTexture(string modPath)
        {
            if (AtlasCache.TryGetValue(modPath, out var cached) && GodotObject.IsInstanceValid(cached))
            {
                return cached;
            }
            var tex = ModAssetHelper.LoadTexture(modPath);
            if (tex == null)
            {
                return null;
            }
            var atlasTex = new AtlasTexture
            {
                Atlas = tex,
                Region = new Rect2(0, 0, tex.GetWidth(), tex.GetHeight())
            };
            AtlasCache[modPath] = atlasTex;
            return atlasTex;
        }

        [HarmonyPatch(typeof(AtlasManager), nameof(AtlasManager.GetSprite))]
        [HarmonyPostfix]
        public static void AtlasManagerGetSpritePostfix(string atlasName, string spriteName, ref AtlasTexture? __result)
        {
            if (__result != null || string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
            {
                return;
            }
            string? modPath = ResolveSpriteModPath(atlasName, spriteName);
            if (modPath != null)
            {
                var atlas = GetOrCreateAtlasTexture(modPath);
                if (atlas != null)
                {
                    __result = atlas;
                }
            }
        }

        [HarmonyPatch(typeof(AtlasManager), nameof(AtlasManager.HasSprite))]
        [HarmonyPostfix]
        public static void AtlasManagerHasSpritePostfix(string atlasName, string spriteName, ref bool __result)
        {
            if (__result || string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
            {
                return;
            }
            string? modPath = ResolveSpriteModPath(atlasName, spriteName);
            if (modPath != null && ModAssetHelper.LoadTexture(modPath) != null)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(CardModel), nameof(CardModel.HasPortrait), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CardHasPortraitPostfix(CardModel __instance, ref bool __result)
        {
            if (__instance is HexerCardBase)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CardPortraitPrefix(CardModel __instance, ref Texture2D? __result)
        {
            if (__instance is not HexerCardBase hexerCard)
            {
                return true;
            }
            var tex = ModAssetHelper.LoadTexture(hexerCard.CustomPortraitPath)
                      ?? ModAssetHelper.LoadTexture("res://Hexer/images/cards/hexer_scene.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CardModel), nameof(CardModel.BigPortrait), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CardBigPortraitPrefix(CardModel __instance, ref Texture2D? __result)
        {
            if (__instance is not HexerCardBase hexerCard)
            {
                return true;
            }
            var tex = ModAssetHelper.LoadTexture(hexerCard.CustomPortraitPath)
                      ?? ModAssetHelper.LoadTexture("res://Hexer/images/cards/hexer_scene.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Icon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool RelicIconPrefix(RelicModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/relics/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.IconOutline), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool RelicIconOutlinePrefix(RelicModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/relics/{entry}_outline.png")
                      ?? ModAssetHelper.LoadTexture($"res://Hexer/images/relics/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.BigIcon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool RelicBigIconPrefix(RelicModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/relics/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Icon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PowerIconPrefix(PowerModel __instance, ref Texture2D? __result)
        {
            if (IsHexerModel(__instance))
            {
                string entry = __instance.Id.Entry.ToLowerInvariant();
                var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/powers/{entry}.png");
                if (tex != null)
                {
                    __result = tex;
                    return false;
                }
            }
            if (__instance is MegaCrit.Sts2.Core.Models.Powers.TemporaryThornsPower tempThorns)
            {
                __result = tempThorns.InternallyAppliedPower.Icon;
                return __result == null;
            }
            return true;
        }

        [HarmonyPatch(typeof(PowerModel), nameof(PowerModel.BigIcon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PowerBigIconPrefix(PowerModel __instance, ref Texture2D? __result)
        {
            if (IsHexerModel(__instance))
            {
                string entry = __instance.Id.Entry.ToLowerInvariant();
                var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/powers/{entry}.png");
                if (tex != null)
                {
                    __result = tex;
                    return false;
                }
            }
            if (__instance is MegaCrit.Sts2.Core.Models.Powers.TemporaryThornsPower tempThorns)
            {
                __result = tempThorns.InternallyAppliedPower.BigIcon;
                return __result == null;
            }
            return true;
        }

        [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.Image), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PotionImagePrefix(PotionModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/potions/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.Outline), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PotionOutlinePrefix(PotionModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/potions/{entry}_outline.png")
                      ?? ModAssetHelper.LoadTexture($"res://Hexer/images/potions/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.LargeImage), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PotionLargeImagePrefix(PotionModel __instance, ref Texture2D? __result)
        {
            if (!IsHexerModel(__instance))
            {
                return true;
            }
            string entry = __instance.Id.Entry.ToLowerInvariant();
            var tex = ModAssetHelper.LoadTexture($"res://Hexer/images/potions/{entry}.png");
            if (tex != null)
            {
                __result = tex;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    public static class AncientPatches
    {
        [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
        [HarmonyPostfix]
        public static void GetValidDialoguesPostfix(
            AncientDialogueSet __instance,
            ModelId characterId,
            int charVisits,
            int totalVisits,
            bool allowAnyCharacterDialogues,
            ref IEnumerable<AncientDialogue> __result)
        {
            if (!ModelDb.Contains(typeof(Hexer)) || characterId != ModelDb.Character<Hexer>().Id)
            {
                return;
            }

            var current = __result?.ToList();
            if (current != null && current.Count > 0)
            {
                __result = current;
                return;
            }

            if (__instance.AgnosticDialogues != null && __instance.AgnosticDialogues.Count > 0)
            {
                __result = __instance.AgnosticDialogues;
                return;
            }

            var ancientOnly = __instance.CharacterDialogues?.Values
                .SelectMany(d => d)
                .Where(d => d.Lines != null && d.Lines.Count > 0 && d.Lines.All(l => l.Speaker == AncientDialogueSpeaker.Ancient))
                .ToList();

            if (ancientOnly != null && ancientOnly.Count > 0)
            {
                __result = ancientOnly;
                return;
            }

            var fallback = __instance.GetValidDialogues(
                ModelDb.Character<Necrobinder>().Id,
                charVisits,
                totalVisits,
                allowAnyCharacterDialogues: true)?.ToList();

            if (fallback != null && fallback.Count > 0)
            {
                __result = fallback;
                return;
            }

            if (__instance.FirstVisitEverDialogue != null)
            {
                __result = new[] { __instance.FirstVisitEverDialogue };
            }
        }
    }

    [HarmonyPatch]
    public static class CompendiumPatches
    {
        private static readonly FieldInfo? FoolFilterField =
            AccessTools.Field(typeof(NCardLibrary), "_foolFilter");

        private static readonly FieldInfo? PoolFiltersField =
            AccessTools.Field(typeof(NCardLibrary), "_poolFilters");

        private static readonly FieldInfo? CardPoolFiltersField =
            AccessTools.Field(typeof(NCardLibrary), "_cardPoolFilters");

        private static readonly FieldInfo? LastHoveredField =
            AccessTools.Field(typeof(NCardLibrary), "_lastHoveredControl");

        private static readonly MethodInfo? UpdateCardPoolFilterMethod =
            AccessTools.Method(typeof(NCardLibrary), "UpdateCardPoolFilter");

        private static readonly FieldInfo? FilterImageField =
            AccessTools.Field(typeof(NCardPoolFilter), "_image");

        private static readonly FieldInfo? FilterHsvField =
            AccessTools.Field(typeof(NCardPoolFilter), "_hsv");

        [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
        [HarmonyPostfix]
        public static void CardLibraryReadyPostfix(NCardLibrary __instance)
        {
            if (!ModelDb.Contains(typeof(Hexer)) || !ModelDb.Contains(typeof(HexerCardPool)))
            {
                return;
            }

            if (FoolFilterField?.GetValue(__instance) is not NCardPoolFilter foolFilter ||
                PoolFiltersField?.GetValue(__instance) is not Dictionary<NCardPoolFilter, Func<CardModel, bool>> poolFilters ||
                CardPoolFiltersField?.GetValue(__instance) is not Dictionary<CharacterModel, NCardPoolFilter> cardPoolFilters)
            {
                return;
            }

            var hexer = ModelDb.Character<Hexer>();
            if (cardPoolFilters.ContainsKey(hexer))
            {
                return;
            }

            var parent = foolFilter.GetParent();
            if (parent == null)
            {
                return;
            }

            if (foolFilter.Duplicate() is not NCardPoolFilter hexerFilter)
            {
                return;
            }

            hexerFilter.Name = "HexerPool";
            parent.AddChild(hexerFilter);
            parent.MoveChild(hexerFilter, foolFilter.GetIndex() + 1);

            var badgeTex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_badge.png");
            if (badgeTex != null)
            {
                if (hexerFilter.GetNodeOrNull<TextureRect>("Image") is TextureRect imgRect)
                {
                    imgRect.Texture = badgeTex;
                    if (imgRect.Material is ShaderMaterial mat)
                    {
                        var dupMat = (ShaderMaterial)mat.Duplicate();
                        imgRect.Material = dupMat;
                        FilterHsvField?.SetValue(hexerFilter, dupMat);
                    }
                    FilterImageField?.SetValue(hexerFilter, imgRect);
                }
                if (hexerFilter.GetNodeOrNull<TextureRect>("Image/Shadow") is TextureRect shadowRect)
                {
                    shadowRect.Texture = badgeTex;
                }
            }

            hexerFilter.Loc = new LocString("characters", "HEXER.title");
            hexerFilter.Visible = true;
            hexerFilter.IsSelected = false;

            if (UpdateCardPoolFilterMethod != null)
            {
                hexerFilter.Toggled += filter => UpdateCardPoolFilterMethod.Invoke(__instance, new object[] { filter });
            }
            hexerFilter.Connect(Control.SignalName.FocusEntered, Callable.From(() =>
            {
                LastHoveredField?.SetValue(__instance, hexerFilter);
            }));

            poolFilters[hexerFilter] = c => c.Pool is HexerCardPool;
            cardPoolFilters[hexer] = hexerFilter;
        }

        private static readonly FieldInfo? GridSeenCardsField =
            AccessTools.Field(typeof(NCardLibraryGrid), "_seenCards");

        private static readonly FieldInfo? GridUnlockedCardsField =
            AccessTools.Field(typeof(NCardLibraryGrid), "_unlockedCards");

        [HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.RefreshVisibility))]
        [HarmonyPostfix]
        public static void RefreshVisibilityPostfix(NCardLibraryGrid __instance)
        {
            if (!ModelDb.Contains(typeof(HexerCardPool)))
            {
                return;
            }
            var seen = GridSeenCardsField?.GetValue(__instance) as HashSet<ModelId>;
            var unlocked = GridUnlockedCardsField?.GetValue(__instance) as HashSet<ModelId>;
            foreach (var card in ModelDb.CardPool<HexerCardPool>().AllCards)
            {
                seen?.Add(card.Id);
                unlocked?.Add(card.Id);
            }
        }

        [HarmonyPatch(typeof(NCardLibraryGrid), "GetCardVisibility")]
        [HarmonyPostfix]
        public static void GetCardVisibilityPostfix(CardModel card, ref MegaCrit.Sts2.Core.Entities.UI.ModelVisibility __result)
        {
            if (card is HexerCardBase)
            {
                __result = MegaCrit.Sts2.Core.Entities.UI.ModelVisibility.Visible;
            }
        }
    }

    [HarmonyPatch]
    public static class CharacterUiPatches
    {
        private static readonly FieldInfo? NCharBtnIconField =
            AccessTools.Field(typeof(NCharacterSelectButton), "_icon");

        private static readonly FieldInfo? NCharBtnIconAddField =
            AccessTools.Field(typeof(NCharacterSelectButton), "_iconAdd");

        private static readonly FieldInfo? NCharSelectBgContainerField =
            AccessTools.Field(typeof(NCharacterSelectScreen), "_bgContainer");

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.AttackSfx), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool AttackSfxPrefix(CharacterModel __instance, ref string __result)
        {
            if (__instance is Hexer)
            {
                __result = "event:/sfx/characters/necrobinder/necrobinder_attack";
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.RunWonAchievement), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool RunWonAchievementPrefix(CharacterModel __instance, ref MegaCrit.Sts2.Core.Achievements.Achievement __result)
        {
            if (__instance is Hexer)
            {
                __result = MegaCrit.Sts2.Core.Achievements.Achievement.NecrobinderWin;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CastSfx), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CastSfxPrefix(CharacterModel __instance, ref string __result)
        {
            if (__instance is Hexer)
            {
                __result = "event:/sfx/characters/necrobinder/necrobinder_cast";
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.Icon), MethodType.Getter)]
        [HarmonyPostfix]
        public static void CharacterIconPostfix(CharacterModel __instance, ref Control? __result)
        {
            if (__instance is not Hexer || __result == null)
            {
                return;
            }
            var badgeTex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_badge.png");
            var outlineTex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_badge_outline.png") ?? badgeTex;
            if (badgeTex == null)
            {
                return;
            }
            ReplaceIconTexturesRecursive(__result, badgeTex, outlineTex);
        }

        private static void ReplaceIconTexturesRecursive(Node node, Texture2D badgeTex, Texture2D? outlineTex)
        {
            if (node is TextureRect tr)
            {
                bool isOutline = tr.Name.ToString().Contains("Outline", StringComparison.OrdinalIgnoreCase);
                tr.Texture = isOutline ? (outlineTex ?? badgeTex) : badgeTex;
            }
            foreach (Node child in node.GetChildren())
            {
                ReplaceIconTexturesRecursive(child, badgeTex, outlineTex);
            }
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectIcon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CharacterSelectIconPrefix(CharacterModel __instance, ref CompressedTexture2D? __result)
        {
            if (__instance is Hexer)
            {
                __result = ModelDb.Character<Necrobinder>().CharacterSelectIcon;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CharacterSelectLockedIcon), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool CharacterSelectLockedIconPrefix(CharacterModel __instance, ref CompressedTexture2D? __result)
        {
            if (__instance is Hexer)
            {
                __result = ModelDb.Character<Necrobinder>().CharacterSelectLockedIcon;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.MapMarker), MethodType.Getter)]
        [HarmonyPrefix]
        public static bool MapMarkerPrefix(CharacterModel __instance, ref CompressedTexture2D? __result)
        {
            if (__instance is Hexer)
            {
                __result = ModelDb.Character<Necrobinder>().MapMarker;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
        [HarmonyPostfix]
        public static void CharSelectButtonInitPostfix(NCharacterSelectButton __instance, CharacterModel character)
        {
            if (character is not Hexer)
            {
                return;
            }
            var tex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_icon.png");
            if (tex == null)
            {
                return;
            }
            if (NCharBtnIconField?.GetValue(__instance) is TextureRect iconRect)
            {
                iconRect.Texture = tex;
            }
            if (NCharBtnIconAddField?.GetValue(__instance) is TextureRect iconAddRect)
            {
                iconAddRect.Texture = tex;
            }
        }

        [HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
        [HarmonyPostfix]
        public static void SelectCharacterPostfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
        {
            if (NCharSelectBgContainerField?.GetValue(__instance) is not Control bgContainer)
            {
                return;
            }

            if (bgContainer.GetNodeOrNull("HexerCustomBg") is Node oldBg)
            {
                bgContainer.RemoveChild(oldBg);
                oldBg.QueueFree();
            }

            if (charSelectButton?.Character is not Hexer)
            {
                return;
            }

            foreach (Node child in bgContainer.GetChildren())
            {
                if (child is CanvasItem ci)
                {
                    ci.Visible = false;
                }
            }

            HexerCharSelectAnimator.BuildAndAttach(bgContainer);
        }

        [HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
        [HarmonyPostfix]
        public static void CreateVisualsPostfix(CharacterModel __instance, ref NCreatureVisuals __result)
        {
            if (__instance is not Hexer || __result == null)
            {
                return;
            }
            HexerCombatVisualController.Attach(__result);
        }

        [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
        [HarmonyPostfix]
        public static void NCreatureReadyPostfix(NCreature __instance)
        {
            GrudgeOverlayController.Attach(__instance);
            if (__instance.Entity?.Player?.Character is Hexer && __instance.Visuals != null)
            {
                HexerCombatVisualController.Attach(__instance.Visuals);
            }
        }

        [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
        [HarmonyPostfix]
        public static void SetAnimationTriggerPostfix(NCreature __instance, string trigger)
        {
            if (__instance.Entity?.Player?.Character is Hexer && __instance.Visuals != null)
            {
                HexerCombatVisualController.TriggerPose(__instance.Visuals, trigger);
            }
        }

        [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
        [HarmonyPostfix]
        public static void StartDeathAnimPostfix(NCreature __instance, ref float __result)
        {
            if (__instance.Entity?.Player?.Character is Hexer && __instance.Visuals != null)
            {
                HexerCombatVisualController.TriggerPose(__instance.Visuals, "Dead");
                __result = Math.Max(__result, 0.95f);
            }
        }

        [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartReviveAnim))]
        [HarmonyPostfix]
        public static void StartReviveAnimPostfix(NCreature __instance)
        {
            if (__instance.Entity?.Player?.Character is Hexer && __instance.Visuals != null)
            {
                HexerCombatVisualController.TriggerPose(__instance.Visuals, "Revive");
            }
        }

        [HarmonyPatch(typeof(NCreature), "ImmediatelySetIdle")]
        [HarmonyPostfix]
        public static void ImmediatelySetIdlePostfix(NCreature __instance)
        {
            if (__instance.Entity?.Player?.Character is Hexer && __instance.Visuals != null)
            {
                HexerCombatVisualController.TriggerPose(__instance.Visuals, "Revive");
            }
        }

        [HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.CreatureCmd), nameof(MegaCrit.Sts2.Core.Commands.CreatureCmd.GainBlock),
            typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),
            typeof(decimal),
            typeof(MegaCrit.Sts2.Core.ValueProps.ValueProp),
            typeof(MegaCrit.Sts2.Core.Entities.Cards.CardPlay),
            typeof(bool))]
        [HarmonyPostfix]
        public static void GainBlockVisualPostfix(
            MegaCrit.Sts2.Core.Entities.Creatures.Creature creature,
            System.Threading.Tasks.Task<decimal> __result)
        {
            if (creature?.Player?.Character is Hexer &&
                MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance != null)
            {
                var node = MegaCrit.Sts2.Core.Nodes.Rooms.NCombatRoom.Instance.GetCreatureNode(creature);
                if (node?.Visuals != null)
                {
                    HexerCombatVisualController.TriggerPose(node.Visuals, "Defend");
                }
            }
        }
    }
}

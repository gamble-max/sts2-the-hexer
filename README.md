# 咒诅师 · 娑罗 (The Hexer) — 《杀戮尖塔 2》新角色 Mod (`v1.2.0`)

来自暗港沉没神龛的盲眼灵媒「娑罗」。以**念力**感知附着于万物之上的**怨**（虚弱、脆弱、缩小、易伤），在自伤诅咒与敌我减益的博弈中驾驭诅咒、祓除、祈福与烧牌循环。

A standalone, zero-dependency custom character mod for **Slay the Spire 2** with full **English & Simplified Chinese** localization (auto-follows game language).

## 核心机制亮点（`v1.2.0`）
- **2D 骨骼 + 幽灵披风建模 (`2D Skeletal & Spectral Cloak Rig`)**：选角与局内全面重制为原版平涂风格 2D 骨骼与网格形变建模，共生「幽灵披风」随【念力】与【怨】层数呈现 0～3 阶动态演化，双臂实时牵引发光念力游丝。
- **念力 (`Psychokinesis`)**：抽到诅咒、造成/受到伤害（每段）或施加【怨】时获得 +1 点；打出含攻击/格挡/回复效果的牌时立即消耗一半（向上取整）使对应数值获得等量加成。
- **怨 (`Grudge`)**：严格限定为 **虚弱、脆弱、缩小、易伤** 4 项特定负面状态的总层数（荆棘/中毒/灾厄等不计入），并带有头顶怨灵视觉与完整词条悬浮提示。
- **祓除 & 祈福 (`Exorcise` & `Blessing`)**：随机解除目标/友方指定层数的【怨】，每实际解除 1 层使敌人失去 2 点力量或使友方获得 2 点力量。
- **诅咒生成 & 烧牌流**：围绕 3 张永恒起始诅咒【咒】【诅】【怨】与衍生牌【残咒】，通过「止水冥心」「织咒成茧」「三叠灵障」「群鬼乱舞」「墓园回响」「焚符化灵」构建高机动抽滤、叠甲与无上限终结技（「万怨千咒」「诅咒奇点」）。
- **完整角色内容**：76 张独立卡图卡牌（75 张职业卡池 + 1 张衍生牌，百科全可见）、15 个自定义能力图标（含临时荆棘独立图标）、9 件遗物、3 瓶药水。

## 安装方法 / Installation

1. 前往本仓库 [Releases](../../releases) 下载最新的 `Hexer-v1.2.0.zip`。
2. 解压到《杀戮尖塔 2》安装目录下的 `mods/` 文件夹中，使目录结构为：
   `<Slay the Spire 2>/mods/Hexer/Hexer.dll`、`Hexer.pck` 与 `Hexer.json`。

## 目录结构

- [manifest.json](manifest.json) — Mod 清单（`id: "Hexer"`, `version: "1.2.0"`, `affects_gameplay: true`）
- `src/` — C# 源码（`HexerModEntry.cs`、`Core/`、`Models/`、`Cards/`、`Powers/`、`Relics/`、`Potions/`、`Patches/`、`Visuals/`）
- `pck/` — 图片资源（`images/`）与双语本地化（`localization/zhs/*.json`、`localization/eng/*.json`）
- `build/` — 独立零前置构建脚本（`build.mjs` + `pack-pck.mjs` + `Shared/`）

## 一键构建与部署 / Build from Source

需要 **Node.js (v18+)** 与 **Visual Studio 2022 / BuildTools**（含 Roslyn `csc.exe`），以及本地《杀戮尖塔 2》游戏目录（可通过环境变量 `STS2_DIR` 指定）：

```powershell
node build/build.mjs
# 构建并直接部署到游戏目录：
node build/build.mjs --deploy
```

- **更新日志**：[CHANGELOG.md](CHANGELOG.md)
- 产物输出：`dist/Hexer/`（`Hexer.dll`、`Hexer.pck`、`Hexer.json`）与 `releases/Hexer-v1.2.0.zip`

using System;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Sts2Mods.Shared;
using HexerMod.Core;
using HexerMod.Models;

namespace HexerMod.Visuals
{
    /// <summary>
    /// Attaches to every NCreature in combat and displays the hidden [怨] (Grudge) spirit overlays.
    /// Visible ONLY when the local player is playing as Hexer.
    /// Uses SceneTree.ProcessFrame instead of a Godot C# virtual _Process override so raw csc.exe builds work reliably.
    /// </summary>
    public sealed class GrudgeOverlayController
    {
        private static readonly ConditionalWeakTable<NCreature, GrudgeOverlayController> Controllers = new();

        private readonly NCreature _ownerCreature;
        private readonly Control _root;
        private readonly TextureRect _spriteRect;
        private readonly Callable _processCallable;
        private SceneTree? _tree;
        private int _lastGrudge = -1;
        private bool _lastIsHexer;
        private double _pulseTime;

        private const string HexerSinglePath = "res://Hexer/images/vfx/grudge_hexer_single.png";
        private const string HexerOver5Path = "res://Hexer/images/vfx/grudge_hexer_over5.png";
        private const string OtherSinglePath = "res://Hexer/images/vfx/grudge_other_single.png";
        private const string OtherOver5Path = "res://Hexer/images/vfx/grudge_other_over5.png";

        public static void Attach(NCreature creature)
        {
            if (creature == null || !GodotObject.IsInstanceValid(creature))
            {
                return;
            }
            if (creature.GetNodeOrNull("HexerGrudgeOverlay") != null)
            {
                return;
            }
            if (Controllers.TryGetValue(creature, out _))
            {
                return;
            }

            var controller = new GrudgeOverlayController(creature);
            Controllers.Add(creature, controller);
        }

        private GrudgeOverlayController(NCreature creature)
        {
            _ownerCreature = creature;
            _root = new Control
            {
                Name = "HexerGrudgeOverlay",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 5
            };
            _spriteRect = new TextureRect
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Visible = false
            };
            _root.AddChild(_spriteRect);
            creature.AddChild(_root);

            _processCallable = Callable.From(OnProcessFrame);
            _tree = creature.GetTree();
            _tree?.Connect(SceneTree.SignalName.ProcessFrame, _processCallable);
            creature.Connect(Node.SignalName.TreeExiting, Callable.From(Detach));

            RefreshVisual();
        }

        private void Detach()
        {
            if (_tree != null && GodotObject.IsInstanceValid(_tree) && _tree.IsConnected(SceneTree.SignalName.ProcessFrame, _processCallable))
            {
                _tree.Disconnect(SceneTree.SignalName.ProcessFrame, _processCallable);
            }
            _tree = null;
        }

        private void OnProcessFrame()
        {
            if (!GodotObject.IsInstanceValid(_ownerCreature) || !GodotObject.IsInstanceValid(_root) || !_ownerCreature.IsInsideTree())
            {
                Detach();
                return;
            }

            _pulseTime += 1.0 / 60.0;
            RefreshVisual();

            if (_spriteRect.Visible)
            {
                float baseY = _lastIsHexer ? -285f : -210f;
                float bob = (float)Math.Sin(_pulseTime * 2.2) * 5.0f;
                _spriteRect.Position = new Vector2(_spriteRect.Position.X, baseY + bob);
            }
        }

        private void RefreshVisual()
        {
            if (!GodotObject.IsInstanceValid(_ownerCreature) || !GodotObject.IsInstanceValid(_spriteRect))
            {
                return;
            }

            Creature? entity = _ownerCreature.Entity;
            if (entity == null || !entity.IsAlive || entity.CombatState == null)
            {
                _spriteRect.Visible = false;
                return;
            }

            // ONLY visible to the local Hexer player!
            var me = LocalContext.GetMe(entity.CombatState);
            if (me?.Character is not Hexer)
            {
                _spriteRect.Visible = false;
                return;
            }

            int grudge = GrudgeHelper.GetGrudgeCount(entity);
            if (grudge <= 0)
            {
                _spriteRect.Visible = false;
                _lastGrudge = 0;
                return;
            }

            bool isHexerUnit = entity.Player?.Character is Hexer;
            if (isHexerUnit)
            {
                // Hexer's own stacks are displayed via her evolving 2D Spectral Cloak Rig.
                _spriteRect.Visible = false;
                _lastGrudge = grudge;
                _lastIsHexer = true;
                return;
            }
            if (grudge == _lastGrudge && isHexerUnit == _lastIsHexer && _spriteRect.Visible)
            {
                return;
            }

            _lastGrudge = grudge;
            _lastIsHexer = isHexerUnit;

            bool over5 = grudge > 5;
            string texPath = isHexerUnit
                ? (over5 ? HexerOver5Path : HexerSinglePath)
                : (over5 ? OtherOver5Path : OtherSinglePath);

            var tex = ModAssetHelper.LoadTexture(texPath);
            if (tex == null)
            {
                _spriteRect.Visible = false;
                return;
            }

            _spriteRect.Texture = tex;
            float size = over5 ? 176f : 132f;
            float xOffset = isHexerUnit ? -size * 0.85f : -size * 0.5f;
            float baseY = isHexerUnit ? -285f : -210f;
            _spriteRect.Size = new Vector2(size, size);
            _spriteRect.Position = new Vector2(xOffset, baseY);
            _spriteRect.Modulate = over5
                ? new Color(1f, 1f, 1f, 0.92f)
                : new Color(1f, 1f, 1f, 0.82f);
            _spriteRect.Visible = true;
        }
    }
}

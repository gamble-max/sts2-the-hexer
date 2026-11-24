using System;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Sts2Mods.Shared;
using HexerMod.Core;
using HexerMod.Powers;

namespace HexerMod.Visuals
{
    public sealed class HexerCombatVisualController
    {
        private static readonly ConditionalWeakTable<NCreatureVisuals, HexerCombatVisualController> _controllers = new();

        private readonly NCreatureVisuals _visuals;
        private readonly Node2D _root;

        private readonly Polygon2D _groundShadow;
        private readonly Polygon2D _haloPolygon;
        private readonly Node2D _pelvisBone;
        private readonly Node2D _backThighBone;
        private readonly Node2D _backShinBone;
        private readonly Node2D _frontThighBone;
        private readonly Node2D _frontShinBone;
        private readonly Node2D _torsoBone;
        private readonly Node2D _backUpperArmBone;
        private readonly Node2D _backForearmBone;
        private readonly Marker2D _backHandTip;
        private readonly Node2D _frontUpperArmBone;
        private readonly Node2D _frontForearmBone;
        private readonly Sprite2D? _frontForearmSprite;
        private readonly Marker2D _frontHandTip;
        private readonly Node2D _headBone;
        private readonly Line2D _crownMistLine;
        private readonly Sprite2D? _spiritSpearNode;
        private readonly Line2D _spiritWardArc;
        private readonly Node2D _spiritHoodEyes;

        private readonly HexerDeformingMesh2D? _cloakBackMesh;
        private readonly HexerDeformingMesh2D? _cloakFrontMesh;
        private readonly HexerDeformingMesh2D? _spiritHoodMesh;
        private readonly HexerDeformingMesh2D? _tendrilLMesh;
        private readonly HexerDeformingMesh2D? _tendrilRMesh;

        private readonly Line2D[] _threads = new Line2D[5];
        private readonly CpuParticles2D? _spiritAuraParticles;
        private readonly CpuParticles2D? _deathBurstParticles;

        private readonly Texture2D? _forearmOpenTex;
        private readonly Texture2D? _forearmGripTex;

        private double _elapsed;
        private double _poseTimer;
        private double _poseDuration;
        private double _deathElapsed;
        private bool _startedWithDeadEntity;
        private string _activePose = "Idle";

        private float _curPelvisX;
        private float _curPelvisY = -130f;
        private float _curTorsoRot = 4f;
        private float _curHeadRot = -2f;
        private float _curBackThigh = -14f;
        private float _curBackShin = 16f;
        private float _curFrontThigh = 16f;
        private float _curFrontShin = -14f;
        private float _curBackUArm = -38f;
        private float _curBackFArm = -58f;
        private float _curFrontUArm = -10f;
        private float _curFrontFArm = -72f;
        private float _curCloakBackRot = 12f;
        private float _curCloakFrontRot = 8f;
        private float _curHoodY = -74f;
        private float _curFlare = 1.0f;

        private Color _flashColor = Colors.White;
        private double _flashTimer;
        private bool _connected;

        private HexerCombatVisualController(NCreatureVisuals visuals)
        {
            _visuals = visuals;

            var headTex = LoadRigTex("head");
            var spiritHoodTex = LoadRigTex("spirit_hood");
            var torsoTex = LoadRigTex("torso");
            var cloakBackTex = LoadRigTex("spirit_cloak_back");
            var cloakFrontTex = LoadRigTex("spirit_cloak_front");
            var tendrilTex = LoadRigTex("spirit_tendril");
            var upperArmTex = LoadRigTex("upper_arm");
            _forearmOpenTex = LoadRigTex("forearm_open");
            _forearmGripTex = LoadRigTex("forearm_grip") ?? _forearmOpenTex;
            var thighTex = LoadRigTex("thigh");
            var shinBootTex = LoadRigTex("shin_boot");
            var spearTex = LoadRigTex("spirit_spear");

            _root = new Node2D
            {
                Name = "HexerCombatRoot",
                Position = Vector2.Zero,
                ZIndex = 2
            };

            _groundShadow = BuildEllipsePolygon("GroundShadow", 76f, 12f, new Color(0f, 0f, 0f, 0.44f), -1);
            _root.AddChild(_groundShadow);

            _haloPolygon = BuildHaloPolygon("HaloPolygon", 88f, 102f, new Color(0.32f, 0.96f, 0.90f, 0.34f));
            _haloPolygon.Position = new Vector2(0f, -206f);
            _haloPolygon.Visible = false;
            _root.AddChild(_haloPolygon);

            _pelvisBone = new Node2D
            {
                Name = "PelvisBone",
                Position = new Vector2(0f, -130f)
            };
            _root.AddChild(_pelvisBone);

            var backShade = new Color(0.74f, 0.78f, 0.84f, 1f);

            // Back leg
            _backThighBone = new Node2D { Name = "BackThighBone", Position = new Vector2(12f, -4f), ZIndex = 1 };
            _backThighBone.AddChild(CreatePartSprite("BackThighSprite", thighTex, 38f, 86f, 0.5f, 0.14f, backShade));
            _backShinBone = new Node2D { Name = "BackShinBone", Position = new Vector2(0f, 60f) };
            _backShinBone.AddChild(CreatePartSprite("BackShinSprite", shinBootTex, 48f, 90f, 0.42f, 0.14f, backShade));
            _backThighBone.AddChild(_backShinBone);
            _pelvisBone.AddChild(_backThighBone);

            // Front leg
            _frontThighBone = new Node2D { Name = "FrontThighBone", Position = new Vector2(-10f, -4f), ZIndex = 2 };
            _frontThighBone.AddChild(CreatePartSprite("FrontThighSprite", thighTex, 38f, 86f, 0.5f, 0.14f, Colors.White));
            _frontShinBone = new Node2D { Name = "FrontShinBone", Position = new Vector2(0f, 60f) };
            _frontShinBone.AddChild(CreatePartSprite("FrontShinSprite", shinBootTex, 48f, 90f, 0.42f, 0.14f, Colors.White));
            _frontThighBone.AddChild(_frontShinBone);
            _pelvisBone.AddChild(_frontThighBone);

            // Torso bone
            _torsoBone = new Node2D { Name = "TorsoBone", Position = Vector2.Zero };
            _pelvisBone.AddChild(_torsoBone);

            if (cloakBackTex != null)
            {
                _cloakBackMesh = new HexerDeformingMesh2D("SpiritCloakBackMesh", cloakBackTex, 142f, 162f, 0.5f, 0.10f, 0);
                _cloakBackMesh.Node.Position = new Vector2(-10f, -82f);
                _cloakBackMesh.Node.Modulate = new Color(0.84f, 0.88f, 0.90f, 0.90f);
                _torsoBone.AddChild(_cloakBackMesh.Node);
            }

            if (tendrilTex != null)
            {
                _tendrilLMesh = new HexerDeformingMesh2D("SpiritTendrilLMesh", tendrilTex, 44f, 155f, 0.5f, 0.10f, 1);
                _tendrilLMesh.Node.Position = new Vector2(-36f, -72f);
                _tendrilLMesh.Node.Modulate = new Color(0.92f, 0.98f, 0.98f, 0.82f);
                _tendrilLMesh.Node.Visible = false;
                _torsoBone.AddChild(_tendrilLMesh.Node);

                _tendrilRMesh = new HexerDeformingMesh2D("SpiritTendrilRMesh", tendrilTex, 44f, 155f, 0.5f, 0.10f, 1);
                _tendrilRMesh.Node.Position = new Vector2(12f, -68f);
                _tendrilRMesh.Node.Modulate = new Color(0.92f, 0.98f, 0.98f, 0.82f);
                _tendrilRMesh.Node.Visible = false;
                _torsoBone.AddChild(_tendrilRMesh.Node);
            }

            // Back arm
            _backUpperArmBone = new Node2D { Name = "BackUpperArmBone", Position = new Vector2(18f, -76f), ZIndex = 2 };
            _backUpperArmBone.AddChild(CreatePartSprite("BackUpperArmSprite", upperArmTex, 28f, 76f, 0.5f, 0.14f, backShade));
            _backForearmBone = new Node2D { Name = "BackForearmBone", Position = new Vector2(0f, 54f) };
            _backForearmBone.AddChild(CreatePartSprite("BackForearmSprite", _forearmOpenTex, 32f, 82f, 0.5f, 0.14f, backShade));
            _backHandTip = new Marker2D { Name = "BackHandTip", Position = new Vector2(0f, 64f) };
            _backForearmBone.AddChild(_backHandTip);
            _backUpperArmBone.AddChild(_backForearmBone);
            _torsoBone.AddChild(_backUpperArmBone);

            // Inner torso
            var torsoSprite = CreatePartSprite("TorsoSprite", torsoTex, 68f, 104f, 0.5f, 0.88f, Colors.White);
            torsoSprite.ZIndex = 3;
            _torsoBone.AddChild(torsoSprite);

            // Spirit Hood + Eyes
            if (spiritHoodTex != null)
            {
                _spiritHoodMesh = new HexerDeformingMesh2D("SpiritHoodMesh", spiritHoodTex, 96f, 118f, 0.5f, 0.82f, 3);
                _spiritHoodMesh.Node.Position = new Vector2(-10f, -74f);
                _spiritHoodMesh.Node.Modulate = new Color(1f, 1f, 1f, 0.94f);
                _torsoBone.AddChild(_spiritHoodMesh.Node);
            }

            _spiritHoodEyes = new Node2D { Name = "SpiritHoodEyes", Position = new Vector2(-14f, -138f), ZIndex = 4, Visible = false };
            var eyeL = BuildDiamondPolygon("HoodEyeL", 4.5f, 6.5f, new Color(0.50f, 0.98f, 0.95f, 0.95f));
            eyeL.Position = new Vector2(-8f, 0f);
            var eyeR = BuildDiamondPolygon("HoodEyeR", 4.5f, 6.5f, new Color(0.50f, 0.98f, 0.95f, 0.95f));
            eyeR.Position = new Vector2(8f, 2f);
            _spiritHoodEyes.AddChild(eyeL);
            _spiritHoodEyes.AddChild(eyeR);
            _torsoBone.AddChild(_spiritHoodEyes);

            // Front spectral cloak
            if (cloakFrontTex != null)
            {
                _cloakFrontMesh = new HexerDeformingMesh2D("SpiritCloakFrontMesh", cloakFrontTex, 122f, 152f, 0.5f, 0.10f, 4);
                _cloakFrontMesh.Node.Position = new Vector2(2f, -84f);
                _cloakFrontMesh.Node.Modulate = new Color(1f, 1f, 1f, 0.94f);
                _torsoBone.AddChild(_cloakFrontMesh.Node);
            }

            // Head
            _headBone = new Node2D { Name = "HeadBone", Position = new Vector2(4f, -86f), ZIndex = 5 };
            _headBone.AddChild(CreatePartSprite("HeadSprite", headTex, 76f, 98f, 0.46f, 0.86f, Colors.White));
            _crownMistLine = new Line2D
            {
                Name = "CrownMistLine",
                Width = 5f,
                DefaultColor = new Color(0.45f, 0.96f, 0.92f, 0.55f),
                BeginCapMode = Line2D.LineCapMode.Round,
                EndCapMode = Line2D.LineCapMode.Round,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
                ZIndex = 6
            };
            _headBone.AddChild(_crownMistLine);
            _torsoBone.AddChild(_headBone);

            // Front arm + Spear
            _frontUpperArmBone = new Node2D { Name = "FrontUpperArmBone", Position = new Vector2(-16f, -74f), ZIndex = 6 };
            _frontUpperArmBone.AddChild(CreatePartSprite("FrontUpperArmSprite", upperArmTex, 28f, 76f, 0.5f, 0.14f, Colors.White));
            _frontForearmBone = new Node2D { Name = "FrontForearmBone", Position = new Vector2(0f, 54f) };
            _frontForearmSprite = CreatePartSprite("FrontForearmSprite", _forearmOpenTex, 32f, 82f, 0.5f, 0.14f, Colors.White);
            _frontForearmBone.AddChild(_frontForearmSprite);
            _frontHandTip = new Marker2D { Name = "FrontHandTip", Position = new Vector2(0f, 64f) };
            _frontForearmBone.AddChild(_frontHandTip);

            _spiritSpearNode = CreatePartSprite("SpiritSpearNode", spearTex, 185f, 40f, 0.22f, 0.5f, new Color(0.95f, 1f, 1f, 0.96f));
            _spiritSpearNode.Position = new Vector2(18f, 62f);
            _spiritSpearNode.RotationDegrees = 88f;
            _spiritSpearNode.ZIndex = 10;
            _spiritSpearNode.Visible = false;
            _frontForearmBone.AddChild(_spiritSpearNode);

            _frontUpperArmBone.AddChild(_frontForearmBone);
            _torsoBone.AddChild(_frontUpperArmBone);

            // Defend Ward Arc
            _spiritWardArc = new Line2D
            {
                Name = "SpiritWardArc",
                Width = 9f,
                DefaultColor = new Color(0.42f, 0.98f, 0.92f, 0.78f),
                BeginCapMode = Line2D.LineCapMode.Round,
                EndCapMode = Line2D.LineCapMode.Round,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
                ZIndex = 9,
                Visible = false
            };
            var wardPts = new Vector2[9];
            for (int i = 0; i < 9; i++)
            {
                float a = Mathf.DegToRad(-68f + i * 17f);
                wardPts[i] = new Vector2(44f + MathF.Cos(a) * 48f, -172f + MathF.Sin(a) * 86f);
            }
            _spiritWardArc.Points = wardPts;
            _root.AddChild(_spiritWardArc);

            // 5 live glowing psychokinesis threads
            var addMat = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            for (int i = 0; i < _threads.Length; i++)
            {
                var line = new Line2D
                {
                    Name = $"PsychokinesisThread{i}",
                    Width = 2.6f,
                    DefaultColor = new Color(0.38f, 0.98f, 0.92f, 0.82f),
                    BeginCapMode = Line2D.LineCapMode.Round,
                    EndCapMode = Line2D.LineCapMode.Round,
                    Material = addMat,
                    ZIndex = 8,
                    Visible = i < 2
                };
                _threads[i] = line;
                _root.AddChild(line);
            }

            var dotTex = ResourceLoader.Exists("res://images/vfx/dot.png")
                ? ResourceLoader.Load<Texture2D>("res://images/vfx/dot.png")
                : null;
            if (dotTex != null)
            {
                _spiritAuraParticles = new CpuParticles2D
                {
                    Name = "HexerSpiritAura",
                    Position = new Vector2(0f, -175f),
                    Amount = 14,
                    Lifetime = 2.6,
                    Preprocess = 1.5,
                    LocalCoords = true,
                    Texture = dotTex,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                    EmissionRectExtents = new Vector2(95f, 155f),
                    Gravity = new Vector2(0f, -26f),
                    ScaleAmountMin = 0.08f,
                    ScaleAmountMax = 0.22f,
                    Color = new Color(0.32f, 0.94f, 0.88f, 0.45f)
                };
                _spiritAuraParticles.Material = addMat;
                _root.AddChild(_spiritAuraParticles);

                _deathBurstParticles = new CpuParticles2D
                {
                    Name = "HexerDeathBurst",
                    Position = new Vector2(0f, -145f),
                    OneShot = true,
                    Emitting = false,
                    Amount = 28,
                    Lifetime = 0.95,
                    Explosiveness = 0.92f,
                    LocalCoords = true,
                    Texture = dotTex,
                    Direction = new Vector2(-0.3f, -1f),
                    Spread = 115f,
                    InitialVelocityMin = 90f,
                    InitialVelocityMax = 240f,
                    Gravity = new Vector2(0f, 180f),
                    ScaleAmountMin = 0.10f,
                    ScaleAmountMax = 0.28f,
                    Color = new Color(0.55f, 0.98f, 0.95f, 0.85f)
                };
                _deathBurstParticles.Material = addMat;
                _root.AddChild(_deathBurstParticles);
            }

            HideUnderlyingBody();
            AlignVisualBounds();
            _visuals.AddChild(_root);
            TryConnectProcessFrame();
        }

        private static Texture2D? LoadRigTex(string name)
            => ModAssetHelper.LoadTexture($"res://Hexer/images/combat/rig/{name}.png");

        private static Sprite2D CreatePartSprite(
            string name,
            Texture2D? tex,
            float targetW,
            float targetH,
            float pivotX,
            float pivotY,
            Color modulate)
        {
            float w = tex != null ? Math.Max(1f, tex.GetWidth()) : targetW;
            float h = tex != null ? Math.Max(1f, tex.GetHeight()) : targetH;
            float sx = targetW / w;
            float sy = targetH / h;
            return new Sprite2D
            {
                Name = name,
                Texture = tex,
                Centered = false,
                Offset = new Vector2(-w * pivotX, -h * pivotY),
                Scale = new Vector2(sx, sy),
                Modulate = modulate
            };
        }

        private static Polygon2D BuildEllipsePolygon(string name, float rx, float ry, Color color, int zIndex)
        {
            const int pts = 18;
            var poly = new Vector2[pts];
            for (int i = 0; i < pts; i++)
            {
                float a = i / (float)pts * MathF.Tau;
                poly[i] = new Vector2(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
            }
            return new Polygon2D
            {
                Name = name,
                Polygon = poly,
                Color = color,
                ZIndex = zIndex
            };
        }

        private static Polygon2D BuildHaloPolygon(string name, float innerR, float outerR, Color color)
        {
            const int segs = 24;
            var poly = new Vector2[segs * 2];
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * MathF.Tau;
                poly[i] = new Vector2(MathF.Cos(a) * outerR, MathF.Sin(a) * outerR);
                poly[segs * 2 - 1 - i] = new Vector2(MathF.Cos(a) * innerR, MathF.Sin(a) * innerR);
            }
            return new Polygon2D
            {
                Name = name,
                Polygon = poly,
                Color = color,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
                ZIndex = -1
            };
        }

        private static Polygon2D BuildDiamondPolygon(string name, float rx, float ry, Color color)
        {
            return new Polygon2D
            {
                Name = name,
                Polygon = new[]
                {
                    new Vector2(0f, -ry),
                    new Vector2(rx, 0f),
                    new Vector2(0f, ry),
                    new Vector2(-rx, 0f)
                },
                Color = color,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
            };
        }

        public static void Attach(NCreatureVisuals visuals)
        {
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
            {
                return;
            }
            if (_controllers.TryGetValue(visuals, out var existing))
            {
                existing.HideUnderlyingBody();
                existing.AlignVisualBounds();
                existing.TryConnectProcessFrame();
                return;
            }
            if (visuals.GetNodeOrNull<Node2D>("HexerCombatRoot") != null)
            {
                return;
            }
            _controllers.Add(visuals, new HexerCombatVisualController(visuals));
        }

        public static void TriggerPose(NCreatureVisuals? visuals, string triggerName)
        {
            if (visuals == null || !GodotObject.IsInstanceValid(visuals))
            {
                return;
            }
            if (_controllers.TryGetValue(visuals, out var ctrl))
            {
                ctrl.PlayPose(triggerName);
            }
        }

        public string ActivePose => _activePose;

        private void AlignVisualBounds()
        {
            if (_visuals.GetNodeOrNull<Control>("Bounds") is Control bounds)
            {
                bounds.OffsetLeft = -122f;
                bounds.OffsetTop = -332f;
                bounds.OffsetRight = 122f;
                bounds.OffsetBottom = 0f;
            }
            if (_visuals.GetNodeOrNull<Marker2D>("CenterPos") is Marker2D center)
            {
                center.Position = new Vector2(0f, -176f);
            }
            if (_visuals.GetNodeOrNull<Marker2D>("IntentPos") is Marker2D intent)
            {
                intent.Position = new Vector2(0f, -385f);
            }
        }

        private void HideUnderlyingBody()
        {
            try
            {
                if (_visuals.Body != null && GodotObject.IsInstanceValid(_visuals.Body))
                {
                    _visuals.Body.Visible = false;
                    _visuals.Body.Modulate = Colors.Transparent;
                }
            }
            catch { }

            foreach (Node child in _visuals.GetChildren())
            {
                if (child == _root)
                {
                    continue;
                }
                string name = child.Name.ToString();
                if (name.Contains("Visuals", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Spine", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Body", StringComparison.OrdinalIgnoreCase))
                {
                    if (child is CanvasItem ci)
                    {
                        ci.Visible = false;
                        ci.Modulate = Colors.Transparent;
                    }
                }
            }
        }

        private void PlayPose(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName))
            {
                return;
            }

            if (triggerName.Equals("Dead", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Dead";
                _poseTimer = 0;
                _poseDuration = 0;
                _deathElapsed = 0;
                _startedWithDeadEntity = _visuals.GetParent() is NCreature nc0 && nc0.Entity is { IsAlive: false };
                if (_spiritAuraParticles != null && GodotObject.IsInstanceValid(_spiritAuraParticles))
                {
                    _spiritAuraParticles.Emitting = false;
                }
                if (_deathBurstParticles != null && GodotObject.IsInstanceValid(_deathBurstParticles))
                {
                    _deathBurstParticles.Restart();
                    _deathBurstParticles.Emitting = true;
                }
                _flashColor = new Color(1.45f, 0.55f, 0.65f, 1f);
                _flashTimer = 0.24;
                return;
            }

            if (triggerName.Equals("Revive", StringComparison.OrdinalIgnoreCase) ||
                triggerName.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Idle";
                _poseTimer = 0;
                _poseDuration = 0;
                _deathElapsed = 0;
                _root.Modulate = Colors.White;
                _flashTimer = 0;
                if (_spiritAuraParticles != null && GodotObject.IsInstanceValid(_spiritAuraParticles))
                {
                    _spiritAuraParticles.Emitting = true;
                }
                return;
            }

            if (_activePose == "Dead")
            {
                return;
            }

            if (triggerName.Equals("Attack", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Attack";
                _poseDuration = 0.42;
                _poseTimer = _poseDuration;
                _flashColor = new Color(1.08f, 1.15f, 1.15f, 1f);
                _flashTimer = 0.14;
            }
            else if (triggerName.Equals("Defend", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Defend";
                _poseDuration = 0.40;
                _poseTimer = _poseDuration;
                _flashColor = new Color(0.75f, 1.25f, 1.25f, 1f);
                _flashTimer = 0.20;
            }
            else if (triggerName.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                     triggerName.Equals("PowerUp", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Cast";
                _poseDuration = 0.44;
                _poseTimer = _poseDuration;
                _flashColor = new Color(0.95f, 1.25f, 1.20f, 1f);
                _flashTimer = 0.22;
            }
            else if (triggerName.Equals("Hit", StringComparison.OrdinalIgnoreCase))
            {
                _activePose = "Hit";
                _poseDuration = 0.32;
                _poseTimer = _poseDuration;
                _flashColor = new Color(1.35f, 0.65f, 0.65f, 1f);
                _flashTimer = 0.16;
            }
        }

        private (int tier, int psy, int grudge) ReadStackTier()
        {
            Creature? entity = _visuals.GetParent() is NCreature nc ? nc.Entity : null;
            int psy = entity?.GetPower<PsychokinesisPower>()?.Amount ?? 0;
            int grudge = entity != null ? GrudgeHelper.GetGrudgeCount(entity) : 0;

            int tier = 0;
            if (psy >= 12 || grudge > 5) tier = 3;
            else if (psy >= 6 || grudge >= 3) tier = 2;
            else if (psy >= 1 || grudge >= 1) tier = 1;

            return (tier, psy, grudge);
        }

        private void TryConnectProcessFrame()
        {
            if (_connected || !GodotObject.IsInstanceValid(_visuals))
            {
                return;
            }
            if (!_visuals.IsInsideTree())
            {
                var cb = Callable.From(TryConnectProcessFrame);
                if (!_visuals.IsConnected(Node.SignalName.TreeEntered, cb))
                {
                    _visuals.Connect(Node.SignalName.TreeEntered, cb, (uint)GodotObject.ConnectFlags.OneShot);
                }
                return;
            }
            var tree = _visuals.GetTree();
            if (tree != null)
            {
                tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
                _connected = true;
            }
        }

        private void OnProcessFrame()
        {
            if (!GodotObject.IsInstanceValid(_visuals) || !GodotObject.IsInstanceValid(_root))
            {
                return;
            }

            HideUnderlyingBody();

            double dt = _visuals.GetProcessDeltaTime();
            if (dt <= 0) dt = 0.016;
            _elapsed += dt;
            float t = (float)_elapsed;

            var (tier, _, grudge) = ReadStackTier();

            if (_activePose == "Dead")
            {
                if (_startedWithDeadEntity && _visuals.GetParent() is NCreature nc && nc.Entity is { IsAlive: true } && _deathElapsed > 0.35)
                {
                    PlayPose("Revive");
                    return;
                }

                _deathElapsed += dt;
                float dFactor = (float)Math.Clamp(dt * 14.0, 0.05, 1.0);
                if (_deathElapsed < 0.20)
                {
                    float shake = MathF.Sin((float)_deathElapsed * 65f) * 5f * (1f - (float)(_deathElapsed / 0.20));
                    ApplyTargets(dFactor, -16f + shake, -124f, -18f, -12f, -8f, 18f, 12f, -8f, -25f, -35f, 24f, -60f, -20f, -16f, -72f, 1.08f);
                }
                else
                {
                    ApplyTargets(dFactor, -24f, -22f, 82f, 14f, 74f, 26f, 64f, 38f, 18f, 20f, 25f, 18f, 10f, 8f, -72f, 1.15f);
                }

                UpdateMeshesAndOverlay(tier, grudge, t, dead: true);
                ApplyFlash(dt, new Color(0.80f, 0.84f, 0.88f, 1f));
                return;
            }

            if (_poseTimer > 0)
            {
                _poseTimer -= dt;
                if (_poseTimer <= 0)
                {
                    _activePose = "Idle";
                }
            }

            float breath = MathF.Sin(t * 2.2f);
            float tierFlare = tier switch { 1 => 1.08f, 2 => 1.18f, 3 => 1.28f, _ => 1.00f };
            float tierHoodY = tier switch { 1 => -80f, 2 => -83f, 3 => -86f, _ => -74f };
            float lerpFactor = (float)Math.Clamp(dt * 16.0, 0.08, 1.0);

            if (_activePose == "Attack")
            {
                float progress = 1f - (float)Math.Clamp(_poseTimer / Math.Max(0.01, _poseDuration), 0.0, 1.0);
                if (progress < 0.28f)
                {
                    ApplyTargets(lerpFactor * 1.3f, -12f, -126f, -10f, -6f, -10f, 18f, 18f, -14f, 20f, -30f, 32f, -95f, -12f, -10f, tierHoodY, tierFlare * 1.05f);
                }
                else
                {
                    ApplyTargets(lerpFactor * 1.4f, 22f, -120f, 22f, -14f, 28f, 14f, -36f, 38f, 45f, -40f, -82f, -8f, 38f, 24f, tierHoodY, tierFlare);
                }
            }
            else if (_activePose == "Defend")
            {
                ApplyTargets(lerpFactor, -6f, -116f, -8f, 8f, 20f, -12f, -28f, 32f, -65f, -45f, -52f, -55f, 18f, -12f, tierHoodY, tierFlare * 1.10f);
            }
            else if (_activePose == "Cast")
            {
                ApplyTargets(lerpFactor, 0f, -138f + breath * 3f, -4f, -10f, -10f, 14f, 12f, -12f, -112f, -25f, -96f, -35f, 28f, 18f, tierHoodY - 4f, tierFlare * 1.12f);
            }
            else if (_activePose == "Hit")
            {
                ApplyTargets(lerpFactor * 1.3f, -16f, -126f, -18f, -12f, -8f, 18f, 12f, -8f, -25f, -35f, 24f, -60f, -22f, -18f, -72f, tierFlare);
            }
            else
            {
                float armWeave = MathF.Sin(t * 1.75f) * 5.5f;
                float cloakBase = tier >= 2 ? 20f : 12f;
                ApplyTargets(
                    lerpFactor,
                    0f,
                    -130f - (tier >= 2 ? 4f : 0f) + breath * 2.5f,
                    (tier >= 2 ? 2f : 4f) + breath * 1.2f,
                    -2f - breath * 1.8f,
                    -14f, 16f, 16f, -14f,
                    -38f - tier * 3f + armWeave, -58f - armWeave * 0.6f,
                    -10f - tier * 2.5f - armWeave, -72f + armWeave * 0.6f,
                    cloakBase,
                    8f + tier * 2f,
                    tierHoodY,
                    tierFlare);
            }

            UpdateMeshesAndOverlay(tier, grudge, t, dead: false);
            ApplyFlash(dt, Colors.White);
        }

        private void ApplyTargets(
            float k,
            float pelX, float pelY, float torRot, float headRot,
            float bThigh, float bShin, float fThigh, float fShin,
            float bUArm, float bFArm, float fUArm, float fFArm,
            float cloakBackRot, float cloakFrontRot, float hoodY, float flare)
        {
            _curPelvisX = Mathf.Lerp(_curPelvisX, pelX, k);
            _curPelvisY = Mathf.Lerp(_curPelvisY, pelY, k);
            _curTorsoRot = Mathf.Lerp(_curTorsoRot, torRot, k);
            _curHeadRot = Mathf.Lerp(_curHeadRot, headRot, k);
            _curBackThigh = Mathf.Lerp(_curBackThigh, bThigh, k);
            _curBackShin = Mathf.Lerp(_curBackShin, bShin, k);
            _curFrontThigh = Mathf.Lerp(_curFrontThigh, fThigh, k);
            _curFrontShin = Mathf.Lerp(_curFrontShin, fShin, k);
            _curBackUArm = Mathf.Lerp(_curBackUArm, bUArm, k);
            _curBackFArm = Mathf.Lerp(_curBackFArm, bFArm, k);
            _curFrontUArm = Mathf.Lerp(_curFrontUArm, fUArm, k);
            _curFrontFArm = Mathf.Lerp(_curFrontFArm, fFArm, k);
            _curCloakBackRot = Mathf.Lerp(_curCloakBackRot, cloakBackRot, k);
            _curCloakFrontRot = Mathf.Lerp(_curCloakFrontRot, cloakFrontRot, k);
            _curHoodY = Mathf.Lerp(_curHoodY, hoodY, k);
            _curFlare = Mathf.Lerp(_curFlare, flare, k);

            _pelvisBone.Position = new Vector2(_curPelvisX, _curPelvisY);
            _torsoBone.RotationDegrees = _curTorsoRot;
            _headBone.RotationDegrees = _curHeadRot;
            _backThighBone.RotationDegrees = _curBackThigh;
            _backShinBone.RotationDegrees = _curBackShin;
            _frontThighBone.RotationDegrees = _curFrontThigh;
            _frontShinBone.RotationDegrees = _curFrontShin;
            _backUpperArmBone.RotationDegrees = _curBackUArm;
            _backForearmBone.RotationDegrees = _curBackFArm;
            _frontUpperArmBone.RotationDegrees = _curFrontUArm;
            _frontForearmBone.RotationDegrees = _curFrontFArm;

            if (_spiritHoodMesh != null)
            {
                _spiritHoodMesh.Node.Position = new Vector2(-10f, _curHoodY);
            }
        }

        private void UpdateMeshesAndOverlay(int tier, int grudge, float t, bool dead)
        {
            float waveAmp = dead ? 1.5f : (3.5f + tier * 2.5f);
            float waveSpeed = 2.4f + tier * 0.45f;

            _cloakBackMesh?.UpdateDeformation(Mathf.DegToRad(_curCloakBackRot), t * waveSpeed, waveAmp, _curFlare);
            _cloakFrontMesh?.UpdateDeformation(Mathf.DegToRad(_curCloakFrontRot), t * waveSpeed + 1.1f, waveAmp * 0.85f, _curFlare);
            _spiritHoodMesh?.UpdateDeformation(Mathf.DegToRad(_curHeadRot * 0.4f), t * 1.9f, dead ? 0.5f : 2.2f + tier * 0.7f, 1f + tier * 0.03f);

            bool showTendrils = !dead && (tier >= 2 || _activePose == "Attack" || _activePose == "Cast");
            if (_tendrilLMesh != null && _tendrilRMesh != null)
            {
                _tendrilLMesh.Node.Visible = showTendrils;
                _tendrilRMesh.Node.Visible = showTendrils;
                if (showTendrils)
                {
                    _tendrilLMesh.UpdateDeformation(Mathf.DegToRad(26f + MathF.Sin(t * 2.5f) * 6f), t * 3.4f, 6.5f, 1.05f);
                    _tendrilRMesh.UpdateDeformation(Mathf.DegToRad(-30f + MathF.Cos(t * 2.3f) * 6f), t * 3.4f + 1.4f, 6.5f, 1.05f);
                }
            }

            bool showHoodEyes = !dead && tier >= 2;
            _spiritHoodEyes.Visible = showHoodEyes;
            if (showHoodEyes)
            {
                _spiritHoodEyes.Position = new Vector2(-14f, _curHoodY - 64f);
                _spiritHoodEyes.Modulate = grudge > 0
                    ? new Color(0.76f, 0.86f, 1.05f, 0.96f)
                    : new Color(0.48f, 0.98f, 0.94f, 0.96f);
            }

            bool showHalo = !dead && (tier >= 3 || _activePose == "Cast");
            _haloPolygon.Visible = showHalo;
            if (showHalo)
            {
                _haloPolygon.Rotation = t * 0.35f;
                float haloPulse = 0.96f + 0.05f * MathF.Sin(t * 3.2f);
                _haloPolygon.Scale = new Vector2(haloPulse, haloPulse);
            }

            bool isAttack = !dead && _activePose == "Attack";
            if (_frontForearmSprite != null)
            {
                _frontForearmSprite.Texture = isAttack ? _forearmGripTex : _forearmOpenTex;
            }
            if (_spiritSpearNode != null)
            {
                _spiritSpearNode.Visible = isAttack;
            }
            _spiritWardArc.Visible = !dead && _activePose == "Defend";

            // Ground shadow width expands when collapsed
            _groundShadow.Scale = dead ? new Vector2(1.55f, 1.15f) : new Vector2(1f, 1f);

            // Curling crown eye-mist
            var mistPts = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                float u = i / 4f;
                mistPts[i] = new Vector2(-18f - u * 22f, -48f - u * 10f + MathF.Sin(t * 3.5f - u * 3f) * 3.5f);
            }
            _crownMistLine.Points = mistPts;
            _crownMistLine.Visible = !dead;

            // Live hand-to-hand glowing psychokinesis threads
            int visibleThreads = dead || isAttack ? 0 : (_activePose == "Cast" ? 5 : (_activePose == "Defend" ? 4 : Math.Clamp(2 + tier, 2, 5)));
            Vector2 pBack = _root.ToLocal(_backHandTip.GlobalPosition);
            Vector2 pFront = _root.ToLocal(_frontHandTip.GlobalPosition);
            for (int i = 0; i < _threads.Length; i++)
            {
                var line = _threads[i];
                line.Visible = i < visibleThreads;
                if (!line.Visible)
                {
                    continue;
                }
                float spread = (i - (visibleThreads - 1) * 0.5f) * 4.5f;
                Vector2 a = pBack + new Vector2(0f, spread);
                Vector2 b = pFront + new Vector2(0f, -spread);
                Vector2 mid = (a + b) * 0.5f + new Vector2(0f, MathF.Sin(t * 4.2f + i * 1.1f) * 5.5f + spread * 0.4f);
                line.Points = new[] { a, mid, b };
            }

            if (_spiritAuraParticles != null && GodotObject.IsInstanceValid(_spiritAuraParticles))
            {
                _spiritAuraParticles.ScaleAmountMax = 0.20f + tier * 0.04f;
            }
        }

        private void ApplyFlash(double dt, Color baseModulate)
        {
            if (_flashTimer > 0)
            {
                _flashTimer -= dt;
                float k = (float)Math.Clamp(_flashTimer / 0.20, 0.0, 1.0);
                _root.Modulate = baseModulate.Lerp(_flashColor, k);
            }
            else
            {
                _root.Modulate = baseModulate;
            }
        }
    }
}

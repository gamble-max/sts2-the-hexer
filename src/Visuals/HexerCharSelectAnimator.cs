using System;
using System.Runtime.CompilerServices;
using Godot;
using Sts2Mods.Shared;

namespace HexerMod.Visuals
{
    public sealed class HexerCharSelectAnimator
    {
        private static readonly ConditionalWeakTable<Control, HexerCharSelectAnimator> _animators = new();

        private readonly Control _root;
        private readonly Control _envPivot;
        private readonly Node2D _rigRoot;
        private readonly Sprite2D _torsoBone;
        private readonly Node2D _headBone;
        private readonly Line2D _crownEyeMist;
        private readonly Sprite2D _leftUpperArm;
        private readonly Sprite2D _rightUpperArm;
        private readonly Sprite2D _leftForearm;
        private readonly Sprite2D _rightForearm;
        private readonly HexerDeformingMesh2D? _spiritHoodMesh;
        private readonly HexerDeformingMesh2D? _cloakLeftMesh;
        private readonly HexerDeformingMesh2D? _cloakRightMesh;
        private readonly Marker2D[] _leftTips = new Marker2D[4];
        private readonly Marker2D[] _rightTips = new Marker2D[4];
        private readonly Line2D[] _threads = new Line2D[4];

        private double _elapsed;
        private bool _connected;

        private HexerCharSelectAnimator(
            Control root,
            Control envPivot,
            Node2D rigRoot,
            Sprite2D torsoBone,
            Node2D headBone,
            Line2D crownEyeMist,
            Sprite2D leftUpperArm,
            Sprite2D rightUpperArm,
            Sprite2D leftForearm,
            Sprite2D rightForearm,
            HexerDeformingMesh2D? spiritHoodMesh,
            HexerDeformingMesh2D? cloakLeftMesh,
            HexerDeformingMesh2D? cloakRightMesh)
        {
            _root = root;
            _envPivot = envPivot;
            _rigRoot = rigRoot;
            _torsoBone = torsoBone;
            _headBone = headBone;
            _crownEyeMist = crownEyeMist;
            _leftUpperArm = leftUpperArm;
            _rightUpperArm = rightUpperArm;
            _leftForearm = leftForearm;
            _rightForearm = rightForearm;
            _spiritHoodMesh = spiritHoodMesh;
            _cloakLeftMesh = cloakLeftMesh;
            _cloakRightMesh = cloakRightMesh;
            TryConnectProcessFrame();
        }

        public static Control? BuildAndAttach(Control bgContainer)
        {
            var envTex = ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_bg_env.png")
                         ?? ModAssetHelper.LoadTexture("res://Hexer/images/char_select/hexer_bg.png");
            if (envTex == null)
            {
                return null;
            }

            var root = new Control
            {
                Name = "HexerCustomBg",
                Position = Vector2.Zero,
                Size = new Vector2(2560f, 1200f),
                ClipContents = true,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            var envPivot = new Control
            {
                Name = "BgEnvPivot",
                Position = Vector2.Zero,
                Size = new Vector2(2560f, 1200f),
                PivotOffset = new Vector2(1280f, 600f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            envPivot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            var bgRect = new TextureRect
            {
                Name = "BgEnv",
                Texture = envTex,
                Position = Vector2.Zero,
                Size = new Vector2(2560f, 1200f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            bgRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            envPivot.AddChild(bgRect);
            root.AddChild(envPivot);

            var addMat = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
            var dotTex = ResourceLoader.Exists("res://images/vfx/dot.png")
                ? ResourceLoader.Load<Texture2D>("res://images/vfx/dot.png")
                : null;

            if (dotTex != null)
            {
                var backMist = new CpuParticles2D
                {
                    Name = "BackMistParticles",
                    Position = new Vector2(1580f, 680f),
                    Amount = 22,
                    Lifetime = 4.2,
                    Preprocess = 3.0,
                    LocalCoords = true,
                    Texture = dotTex,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                    EmissionRectExtents = new Vector2(920f, 520f),
                    Gravity = new Vector2(6f, -18f),
                    ScaleAmountMin = 0.06f,
                    ScaleAmountMax = 0.18f,
                    Color = new Color(0.28f, 0.88f, 0.82f, 0.30f),
                    Material = addMat
                };
                root.AddChild(backMist);
            }

            var rigRoot = new Node2D
            {
                Name = "HexerCharSelectRig",
                Position = new Vector2(1680f, 1435f)
            };
            root.AddChild(rigRoot);

            var hoodTex = LoadCsRigTex("cs_spirit_hood");
            var torsoTex = LoadCsRigTex("cs_torso");
            var cloakLTex = LoadCsRigTex("cs_spirit_cloak_left");
            var cloakRTex = LoadCsRigTex("cs_spirit_cloak_right");
            var headTex = LoadCsRigTex("cs_head");
            var luaTex = LoadCsRigTex("cs_left_upper_arm");
            var ruaTex = LoadCsRigTex("cs_right_upper_arm");
            var lfaTex = LoadCsRigTex("cs_left_forearm_hand");
            var rfaTex = LoadCsRigTex("cs_right_forearm_hand");

            // 1. Spirit Hood behind neck
            HexerDeformingMesh2D? spiritHoodMesh = null;
            if (hoodTex != null)
            {
                spiritHoodMesh = new HexerDeformingMesh2D("SpiritHoodMesh", hoodTex, 640f, 570f, 0.5f, 0.72f, 1);
                spiritHoodMesh.Node.Position = new Vector2(-8f, -775f);
                spiritHoodMesh.Node.Modulate = new Color(1f, 1f, 1f, 0.95f);
                rigRoot.AddChild(spiritHoodMesh.Node);
            }

            // 2. Torso
            var torsoBone = CreateCsSprite("TorsoBone", torsoTex, 490f, 620f, 0.5f, 0.88f, Colors.White, 2);
            torsoBone.Position = new Vector2(0f, -280f);
            rigRoot.AddChild(torsoBone);

            // 3-4. BOTH Upper Arms strictly UNDER the Spectral Cloak (ZIndex = 2, ZAsRelative = false)
            var leftUpperBone = new Node2D { Name = "LeftUpperArmNode", Position = new Vector2(-185f, -705f), RotationDegrees = 34f, ZIndex = 2, ZAsRelative = false };
            var leftUpperSprite = CreateCsSprite("LeftUpperArmSprite", luaTex, 165f, 248f, 0.5f, 0.14f, new Color(0.86f, 0.88f, 0.90f, 1f), 2);
            leftUpperSprite.ZAsRelative = false;
            leftUpperBone.AddChild(leftUpperSprite);
            rigRoot.AddChild(leftUpperBone);

            var rightUpperBone = new Node2D { Name = "RightUpperArmNode", Position = new Vector2(185f, -690f), RotationDegrees = -30f, ZIndex = 2, ZAsRelative = false };
            var rightUpperSprite = CreateCsSprite("RightUpperArmSprite", ruaTex, 165f, 248f, 0.5f, 0.14f, new Color(0.92f, 0.94f, 0.96f, 1f), 2);
            rightUpperSprite.ZAsRelative = false;
            rightUpperBone.AddChild(rightUpperSprite);
            rigRoot.AddChild(rightUpperBone);

            // 5-6. Spectral Cloak wings OVER torso & upper arms (ZIndex = 4, ZAsRelative = false)
            HexerDeformingMesh2D? cloakLeftMesh = null;
            if (cloakLTex != null)
            {
                cloakLeftMesh = new HexerDeformingMesh2D("SpiritCloakLeftMesh", cloakLTex, 550f, 950f, 0.5f, 0.10f, 4);
                cloakLeftMesh.Node.Position = new Vector2(-165f, -760f);
                cloakLeftMesh.Node.Modulate = new Color(1f, 1f, 1f, 0.98f);
                cloakLeftMesh.Node.ZAsRelative = false;
                rigRoot.AddChild(cloakLeftMesh.Node);
            }

            HexerDeformingMesh2D? cloakRightMesh = null;
            if (cloakRTex != null)
            {
                cloakRightMesh = new HexerDeformingMesh2D("SpiritCloakRightMesh", cloakRTex, 565f, 970f, 0.5f, 0.10f, 4);
                cloakRightMesh.Node.Position = new Vector2(170f, -760f);
                cloakRightMesh.Node.Modulate = new Color(1f, 1f, 1f, 0.98f);
                cloakRightMesh.Node.ZAsRelative = false;
                rigRoot.AddChild(cloakRightMesh.Node);
            }

            // 7. Head (cs_head.png already includes painted cyan eye-mist)
            var headBone = new Node2D { Name = "HeadBone", Position = new Vector2(-12f, -820f), RotationDegrees = -2f, ZIndex = 5, ZAsRelative = false };
            var headSprite = CreateCsSprite("HeadSprite", headTex, 440f, 515f, 0.48f, 0.86f, Colors.White, 5);
            headSprite.ZAsRelative = false;
            headBone.AddChild(headSprite);
            var crownEyeMist = new Line2D
            {
                Name = "CrownEyeMist",
                Visible = false
            };
            headBone.AddChild(crownEyeMist);
            rigRoot.AddChild(headBone);

            // 8-9. Bandaged Forearms & Hands emerging IN FRONT of the Spectral Cloak (ZIndex = 6, ZAsRelative = false)
            var leftForearmBone = new Node2D
            {
                Name = "LeftForearmBone",
                Position = new Vector2(0f, 195f),
                RotationDegrees = -166f, // 34 - 166 = -132 global deg
                ZIndex = 6,
                ZAsRelative = false
            };
            var leftForearmSprite = CreateCsSprite("LeftForearmSprite", lfaTex, 150f, 390f, 0.5f, 0.14f, new Color(0.96f, 0.97f, 0.98f, 1f), 6);
            leftForearmSprite.ZAsRelative = false;
            leftForearmBone.AddChild(leftForearmSprite);
            leftUpperBone.AddChild(leftForearmBone);

            var rightForearmBone = new Node2D
            {
                Name = "RightForearmBone",
                Position = new Vector2(0f, 205f),
                RotationDegrees = 128f, // -30 + 128 = +98 global deg
                ZIndex = 6,
                ZAsRelative = false
            };
            var rightForearmSprite = CreateCsSprite("RightForearmSprite", rfaTex, 150f, 390f, 0.5f, 0.14f, Colors.White, 6);
            rightForearmSprite.ZAsRelative = false;
            rightForearmBone.AddChild(rightForearmSprite);
            rightUpperBone.AddChild(rightForearmBone);

            var animator = new HexerCharSelectAnimator(
                root, envPivot, rigRoot, torsoBone, headBone, crownEyeMist,
                leftForearmSprite, rightForearmSprite, leftForearmSprite, rightForearmSprite,
                spiritHoodMesh, cloakLeftMesh, cloakRightMesh);
            animator._leftForearmNode = leftForearmBone;
            animator._rightForearmNode = rightForearmBone;

            for (int i = 0; i < 4; i++)
            {
                float offL = (i - 1.5f) * 18f;
                var lt = new Marker2D { Name = $"LTip{i}", Position = new Vector2(offL, 285f + i * 6f) };
                leftForearmBone.AddChild(lt);
                animator._leftTips[i] = lt;

                float offR = (i - 1.5f) * 14f;
                var rt = new Marker2D { Name = $"RTip{i}", Position = new Vector2(offR, 275f - i * 5f) };
                rightForearmBone.AddChild(rt);
                animator._rightTips[i] = rt;

                var thread = new Line2D
                {
                    Name = $"SpiritThread{i}",
                    Width = 3.4f,
                    DefaultColor = new Color(0.35f, 0.98f, 0.94f, 0.86f),
                    BeginCapMode = Line2D.LineCapMode.Round,
                    EndCapMode = Line2D.LineCapMode.Round,
                    Material = addMat,
                    ZIndex = 8,
                    ZAsRelative = false
                };
                rigRoot.AddChild(thread);
                animator._threads[i] = thread;
            }

            if (dotTex != null)
            {
                var crownSmoke = new CpuParticles2D
                {
                    Name = "CrownSmokeParticles",
                    Position = new Vector2(1660f, 360f),
                    Amount = 16,
                    Lifetime = 2.6,
                    Preprocess = 1.5,
                    LocalCoords = true,
                    Texture = dotTex,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                    EmissionRectExtents = new Vector2(90f, 35f),
                    Gravity = new Vector2(16f, -24f),
                    ScaleAmountMin = 0.07f,
                    ScaleAmountMax = 0.18f,
                    Color = new Color(0.36f, 0.96f, 0.90f, 0.38f),
                    Material = addMat
                };
                root.AddChild(crownSmoke);

                var embers = new CpuParticles2D
                {
                    Name = "ForegroundEmbers",
                    Position = new Vector2(1580f, 680f),
                    Amount = 18,
                    Lifetime = 3.4,
                    Preprocess = 2.0,
                    LocalCoords = true,
                    Texture = dotTex,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                    EmissionRectExtents = new Vector2(500f, 420f),
                    Gravity = new Vector2(-8f, -22f),
                    ScaleAmountMin = 0.05f,
                    ScaleAmountMax = 0.14f,
                    Color = new Color(0.45f, 0.98f, 0.90f, 0.42f),
                    Material = addMat
                };
                root.AddChild(embers);
            }

            bgContainer.AddChild(root);
            _animators.Add(root, animator);
            return root;
        }

        private Node2D? _leftForearmNode;
        private Node2D? _rightForearmNode;

        private static Texture2D? LoadCsRigTex(string name)
            => ModAssetHelper.LoadTexture($"res://Hexer/images/char_select/rig/{name}.png");

        private static Sprite2D CreateCsSprite(
            string name,
            Texture2D? tex,
            float targetW,
            float targetH,
            float pivotX,
            float pivotY,
            Color modulate,
            int zIndex)
        {
            float w = tex != null ? Math.Max(1f, tex.GetWidth()) : targetW;
            float h = tex != null ? Math.Max(1f, tex.GetHeight()) : targetH;
            return new Sprite2D
            {
                Name = name,
                Texture = tex,
                Centered = false,
                Offset = new Vector2(-w * pivotX, -h * pivotY),
                Scale = new Vector2(targetW / w, targetH / h),
                Modulate = modulate,
                ZIndex = zIndex
            };
        }

        private void TryConnectProcessFrame()
        {
            if (_connected || !GodotObject.IsInstanceValid(_root))
            {
                return;
            }
            if (!_root.IsInsideTree())
            {
                var cb = Callable.From(TryConnectProcessFrame);
                if (!_root.IsConnected(Node.SignalName.TreeEntered, cb))
                {
                    _root.Connect(Node.SignalName.TreeEntered, cb, (uint)GodotObject.ConnectFlags.OneShot);
                }
                return;
            }
            var tree = _root.GetTree();
            if (tree != null)
            {
                tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
                _connected = true;
            }
        }

        private void OnProcessFrame()
        {
            if (!GodotObject.IsInstanceValid(_root) || !_root.Visible || !GodotObject.IsInstanceValid(_rigRoot))
            {
                return;
            }

            double dt = _root.GetProcessDeltaTime();
            if (dt <= 0) dt = 0.016;
            _elapsed += dt;
            float t = (float)_elapsed;

            if (GodotObject.IsInstanceValid(_envPivot))
            {
                float envZoom = 1.010f + 0.005f * MathF.Sin(t * 0.55f);
                _envPivot.Scale = new Vector2(envZoom, envZoom);
                _envPivot.Position = new Vector2(MathF.Sin(t * 0.7f) * 5f, MathF.Cos(t * 1.1f) * 3f);
            }

            float breath = MathF.Sin(t * 1.75f);
            _torsoBone.Position = new Vector2(0f, -280f + breath * 5f);
            _headBone.Position = new Vector2(-12f, -820f + breath * 4f);
            _headBone.RotationDegrees = -2f + MathF.Sin(t * 0.9f) * 1.8f;

            var mistPts = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                float u = i / 4f;
                mistPts[i] = new Vector2(85f + u * 95f, -265f - u * 35f + MathF.Sin(t * 2.8f - u * 2.6f) * 10f);
            }
            _crownEyeMist.Points = mistPts;

            _spiritHoodMesh?.UpdateDeformation(Mathf.DegToRad(MathF.Sin(t * 0.9f) * 1.5f), t * 1.6f, 6.5f, 1.02f);
            _cloakLeftMesh?.UpdateDeformation(Mathf.DegToRad(4f + MathF.Sin(t * 1.3f) * 2.5f), t * 1.9f, 12f, 1.04f);
            _cloakRightMesh?.UpdateDeformation(Mathf.DegToRad(-4f - MathF.Sin(t * 1.3f + 0.8f) * 2.5f), t * 1.9f + 1.0f, 12f, 1.04f);

            if (_leftForearmNode != null && GodotObject.IsInstanceValid(_leftForearmNode))
            {
                _leftForearmNode.RotationDegrees = -166f + MathF.Sin(t * 1.45f) * 4.5f;
            }
            if (_rightForearmNode != null && GodotObject.IsInstanceValid(_rightForearmNode))
            {
                _rightForearmNode.RotationDegrees = 128f - MathF.Sin(t * 1.45f + 0.6f) * 4.5f;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 aBase = _rigRoot.ToLocal(_leftTips[i].GlobalPosition);
                Vector2 bBase = _rigRoot.ToLocal(_rightTips[i].GlobalPosition);
                Vector2 dir = bBase - aBase;
                Vector2 perp = dir.LengthSquared() > 1f
                    ? new Vector2(-dir.Y, dir.X).Normalized()
                    : Vector2.Right;
                float fanL = (i - 1.5f) * 18f;
                float fanR = (i - 1.5f) * 12f;
                Vector2 a = aBase + perp * fanL;
                Vector2 b = bBase + perp * fanR;
                Vector2 mid = (a + b) * 0.5f + perp * (MathF.Sin(t * 3.1f + i * 0.9f) * 8f);
                _threads[i].Points = new[] { a, mid, b };
            }
        }
    }
}

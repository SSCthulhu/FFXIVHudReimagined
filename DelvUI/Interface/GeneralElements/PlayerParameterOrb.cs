using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using DelvUI.Config;
using DelvUI.Config.Attributes;
using DelvUI.Enums;
using DelvUI.Helpers;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using StructsBattleChara = FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara;

namespace DelvUI.Interface.GeneralElements
{
    public enum LimitBreakPreviewBars
    {
        One = 1,
        Two = 2,
        Three = 3
    }

    public enum OrbArcFillDirection
    {
        LeftToRight = 0,
        RightToLeft = 1
    }

    [DisableParentSettings("Size")]
    [Section("Player Parameter Orb")]
    [SubSection("Player Parameter Orb", 0)]
    public class PlayerParameterOrbConfig : AnchorablePluginConfigObject
    {
        [DragFloat("Health Radius", min = 20, max = 300)]
        [Order(20)]
        public float HealthRadius = 64f;

        [ColorEdit4("Health Background")]
        [Order(21)]
        public PluginConfigColor HealthBackgroundColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 70f / 100f));

        [Checkbox("Use Health Texture")]
        [Order(22)]
        public bool UseHealthTexture = false;

        [BarTexture("Health Texture")]
        [Order(23, collapseWith = nameof(UseHealthTexture))]
        public string HealthTextureName = BarTexturesManager.DefaultBarTextureName;

        [BarTextureDrawMode("Health Texture Draw Mode")]
        [Order(24, collapseWith = nameof(UseHealthTexture))]
        public BarTextureDrawMode HealthTextureDrawMode = BarTextureDrawMode.Stretch;

        [Checkbox("Show Health Border")]
        [Order(25)]
        public bool ShowHealthBorder = false;

        [DragInt("Health Border Thickness", min = 1, max = 10)]
        [Order(26, collapseWith = nameof(ShowHealthBorder))]
        public int HealthBorderThickness = 1;

        [ColorEdit4("Health Border Color")]
        [Order(27, collapseWith = nameof(ShowHealthBorder))]
        public PluginConfigColor HealthBorderColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [Checkbox("Show Shield Overlay", spacing = true)]
        [Order(75)]
        public bool ShowShieldOverlay = true;

        [ColorEdit4("Shield Color")]
        [Order(76, collapseWith = nameof(ShowShieldOverlay))]
        public PluginConfigColor ShieldColor = new(new Vector4(198f / 255f, 210f / 255f, 255f / 255f, 70f / 100f));

        [Checkbox("Use Job Color")]
        [Order(28)]
        public bool UseJobColor = true;

        [Checkbox("Use Role Color")]
        [Order(29)]
        public bool UseRoleColor = false;

        [NestedConfig("Color Based On Health Value", 30, collapsingHeader = false)]
        public ColorByHealthValueConfig ColorByHealth = new ColorByHealthValueConfig();

        [Checkbox("Use Smooth Transitions")]
        [Order(31)]
        public bool SmoothHealth = true;

        [DragFloat("Smooth Velocity", min = 1f, max = 100f)]
        [Order(32, collapseWith = nameof(SmoothHealth))]
        public float SmoothVelocity = 25f;

        [Checkbox("Show Mana Ring", spacing = true)]
        [Order(30)]
        public bool ShowManaRing = true;

        [DragFloat("Mana Ring Thickness", min = 1, max = 60)]
        [Order(31, collapseWith = nameof(ShowManaRing))]
        public float ManaRingThickness = 12f;

        [DragFloat("Mana Ring Gap", min = 0, max = 60)]
        [Order(32, collapseWith = nameof(ShowManaRing))]
        public float ManaRingGap = 8f;

        [ColorEdit4("Mana Color")]
        [Order(33, collapseWith = nameof(ShowManaRing))]
        public PluginConfigColor ManaColor = new(new Vector4(229f / 255f, 85f / 255f, 167f / 255f, 100f / 100f));

        [ColorEdit4("Mana Background")]
        [Order(34, collapseWith = nameof(ShowManaRing))]
        public PluginConfigColor ManaBackgroundColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 70f / 100f));

        [Checkbox("Show Mana Border")]
        [Order(35, collapseWith = nameof(ShowManaRing))]
        public bool ShowManaBorder = true;

        [DragInt("Mana Border Thickness", min = 1, max = 10)]
        [Order(36, collapseWith = nameof(ShowManaBorder))]
        public int ManaBorderThickness = 1;

        [ColorEdit4("Mana Border Color")]
        [Order(37, collapseWith = nameof(ShowManaBorder))]
        public PluginConfigColor ManaBorderColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [Checkbox("Show Cast Arc", spacing = true)]
        [Order(40)]
        public bool ShowCastArc = true;

        [DragFloat("Cast Arc Thickness", min = 1, max = 60)]
        [Order(41, collapseWith = nameof(ShowCastArc))]
        public float CastArcThickness = 12f;

        [DragFloat("Cast Arc Radius Offset", min = 0, max = 80)]
        [Order(42, collapseWith = nameof(ShowCastArc))]
        public float CastArcRadiusOffset = 12f;

        [ColorEdit4("Cast Arc Color")]
        [Order(43, collapseWith = nameof(ShowCastArc))]
        public PluginConfigColor CastArcColor = new(new Vector4(0f / 255f, 162f / 255f, 252f / 255f, 100f / 100f));

        [ColorEdit4("Cast Arc Background")]
        [Order(44, collapseWith = nameof(ShowCastArc))]
        public PluginConfigColor CastArcBackgroundColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 70f / 100f));

        [Checkbox("Preview Cast Arc")]
        [Order(45, collapseWith = nameof(ShowCastArc))]
        public bool PreviewCastArc = false;

        [Combo("Cast Fill Direction", "Left to Right", "Right to Left")]
        [Order(46, collapseWith = nameof(ShowCastArc))]
        public OrbArcFillDirection CastFillDirection = OrbArcFillDirection.LeftToRight;

        [Checkbox("Show Cast Border")]
        [Order(47, collapseWith = nameof(ShowCastArc))]
        public bool ShowCastBorder = true;

        [DragInt("Cast Border Thickness", min = 1, max = 10)]
        [Order(48, collapseWith = nameof(ShowCastBorder))]
        public int CastBorderThickness = 1;

        [ColorEdit4("Cast Border Color")]
        [Order(49, collapseWith = nameof(ShowCastBorder))]
        public PluginConfigColor CastBorderColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [Checkbox("Show Slidecast Marker")]
        [Order(50, collapseWith = nameof(ShowCastArc))]
        public bool ShowSlidecastMarker = true;

        [DragInt("Slidecast Time (milliseconds)", min = 0, max = 10000)]
        [Order(51, collapseWith = nameof(ShowSlidecastMarker))]
        public int SlidecastTime = 500;

        [ColorEdit4("Slidecast Color")]
        [Order(52, collapseWith = nameof(ShowSlidecastMarker))]
        public PluginConfigColor SlidecastColor = new(new Vector4(190f / 255f, 28f / 255f, 57f / 255f, 100f / 100f));

        [ColorEdit4("Slidecast Border Color")]
        [Order(53, collapseWith = nameof(ShowSlidecastMarker))]
        public PluginConfigColor SlidecastBorderColor = new(new Vector4(190f / 255f, 28f / 255f, 57f / 255f, 100f / 100f));

        [Checkbox("Show Limit Break Arc", spacing = true)]
        [Order(54)]
        public bool ShowLimitBreakArc = true;

        [Checkbox("Preview Limit Break Arc")]
        [Order(55, collapseWith = nameof(ShowLimitBreakArc))]
        public bool PreviewLimitBreakArc = false;

        [RadioSelector("1", "2", "3")]
        [Order(56, collapseWith = nameof(PreviewLimitBreakArc))]
        public int PreviewLimitBreakBars = 2;

        [Combo("Limit Break Fill Direction", "Left to Right", "Right to Left")]
        [Order(57, collapseWith = nameof(ShowLimitBreakArc))]
        public OrbArcFillDirection LimitBreakFillDirection = OrbArcFillDirection.RightToLeft;

        [DragFloat("Limit Break Arc Thickness", min = 1, max = 60)]
        [Order(58, collapseWith = nameof(ShowLimitBreakArc))]
        public float LimitBreakArcThickness = 12f;

        [DragFloat("Limit Break Radius Offset", min = 0, max = 120)]
        [Order(59, collapseWith = nameof(ShowLimitBreakArc))]
        public float LimitBreakRadiusOffset = 20f;

        [DragFloat("Segment Gap (degrees)", min = 0, max = 12)]
        [Order(60, collapseWith = nameof(ShowLimitBreakArc))]
        public float LimitBreakSegmentGapDegrees = 2f;

        [ColorEdit4("Limit Break Color")]
        [Order(61, collapseWith = nameof(ShowLimitBreakArc))]
        public PluginConfigColor LimitBreakColor = new(new Vector4(255f / 255f, 255f / 255f, 0f / 255f, 100f / 100f));

        [Checkbox("Use Partial Fill Color")]
        [Order(62, collapseWith = nameof(ShowLimitBreakArc))]
        public bool UsePartialLimitBreakColor = true;

        [ColorEdit4("Partial Fill Color")]
        [Order(63, collapseWith = nameof(UsePartialLimitBreakColor))]
        public PluginConfigColor PartialLimitBreakColor = new(new Vector4(0f / 255f, 181f / 255f, 255f / 255f, 100f / 100f));

        [ColorEdit4("Limit Break Background")]
        [Order(64, collapseWith = nameof(ShowLimitBreakArc))]
        public PluginConfigColor LimitBreakBackgroundColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 70f / 100f));

        [Checkbox("Show Limit Break Border")]
        [Order(65, collapseWith = nameof(ShowLimitBreakArc))]
        public bool ShowLimitBreakBorder = true;

        [DragInt("Limit Break Border Thickness", min = 1, max = 10)]
        [Order(66, collapseWith = nameof(ShowLimitBreakBorder))]
        public int LimitBreakBorderThickness = 1;

        [ColorEdit4("Limit Break Border Color")]
        [Order(67, collapseWith = nameof(ShowLimitBreakBorder))]
        public PluginConfigColor LimitBreakBorderColor = new(new Vector4(0f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [NestedConfig("Center Text Line 1", 70)]
        public EditableLabelConfig CenterTextLine1 = new EditableLabelConfig(new Vector2(0, -16), "[health:percent]%", DrawAnchor.Center, DrawAnchor.Center);

        [NestedConfig("Center Text Line 2", 71)]
        public EditableLabelConfig CenterTextLine2 = new EditableLabelConfig(new Vector2(0, 0), "[health:current-formatted]/[health:max-formatted]", DrawAnchor.Center, DrawAnchor.Center);

        [NestedConfig("Center Text Line 3", 72)]
        public EditableLabelConfig CenterTextLine3 = new EditableLabelConfig(new Vector2(0, 16), "[shield:text]", DrawAnchor.Center, DrawAnchor.Center);

        [Checkbox("Hide Default Player Unit Frame", spacing = true)]
        [Order(80)]
        public bool HideDefaultPlayerUnitFrame = false;

        [Checkbox("Show Tank Stance Indicator")]
        [Order(81)]
        public bool ShowTankStanceIndicator = true;

        [DragFloat("Tank Stance Indicator Size", min = 6, max = 64)]
        [Order(82, collapseWith = nameof(ShowTankStanceIndicator))]
        public float TankStanceIndicatorSize = 16f;

        [DragFloat2("Tank Stance Offset", min = -300, max = 300)]
        [Order(83, collapseWith = nameof(ShowTankStanceIndicator))]
        public Vector2 TankStanceIndicatorOffset = Vector2.Zero;

        [ColorEdit4("Tank Stance Active Color")]
        [Order(84, collapseWith = nameof(ShowTankStanceIndicator))]
        public PluginConfigColor TankStanceActiveColor = new(new Vector4(0f / 255f, 255f / 255f, 255f / 255f, 100f / 100f));

        [ColorEdit4("Tank Stance Inactive Color")]
        [Order(85, collapseWith = nameof(ShowTankStanceIndicator))]
        public PluginConfigColor TankStanceInactiveColor = new(new Vector4(255f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));

        [NestedConfig("Visibility", 200)]
        public VisibilityConfig VisibilityConfig = new VisibilityConfig();

        public new static PlayerParameterOrbConfig DefaultConfig()
        {
            var config = new PlayerParameterOrbConfig
            {
                Position = Vector2.Zero,
                Anchor = DrawAnchor.Center,
                Size = new Vector2(220, 220),
            };

            config.ColorByHealth.Enabled = false;
            return config;
        }
    }

    public unsafe class PlayerParameterOrbHud : ParentAnchoredDraggableHudElement, IHudElementWithActor, IHudElementWithAnchorableParent, IHudElementWithVisibilityConfig
    {
        private const int ArcSegments = 120;
        public PlayerParameterOrbConfig Config => (PlayerParameterOrbConfig)_config;
        public VisibilityConfig VisibilityConfig => Config.VisibilityConfig;
        public IGameObject? Actor { get; set; }

        private readonly SmoothHPHelper _smoothHPHelper = new();
        private readonly LabelHud _centerLine1Hud;
        private readonly LabelHud _centerLine2Hud;
        private readonly LabelHud _centerLine3Hud;

        protected override bool AnchorToParent => false;
        protected override DrawAnchor ParentAnchor => DrawAnchor.Center;

        public PlayerParameterOrbHud(PlayerParameterOrbConfig config, string displayName) : base(config, displayName)
        {
            _centerLine1Hud = new LabelHud(config.CenterTextLine1);
            _centerLine2Hud = new LabelHud(config.CenterTextLine2);
            _centerLine3Hud = new LabelHud(config.CenterTextLine3);
        }

        protected override (List<Vector2>, List<Vector2>) ChildrenPositionsAndSizes()
        {
            var size = CalculateBoundsSize();
            return (new List<Vector2> { Config.Position }, new List<Vector2> { size });
        }

        public override void DrawChildren(Vector2 origin)
        {
            if (!Config.Enabled || Actor is not ICharacter character)
            {
                return;
            }

            uint currentHp = character.CurrentHp;
            uint maxHp = Math.Max(1, character.MaxHp);
            if (Config.SmoothHealth)
            {
                currentHp = _smoothHPHelper.GetNextHp((int)currentHp, (int)maxHp, Config.SmoothVelocity);
            }

            float hpRatio = Math.Clamp(currentHp / (float)maxHp, 0f, 1f);

            uint currentMp = character.CurrentMp;
            uint maxMp = Math.Max(1, character.MaxMp);
            float mpRatio = Math.Clamp(currentMp / (float)maxMp, 0f, 1f);
            float shieldRatio = Config.ShowShieldOverlay ? Math.Clamp(Utils.ActorShieldValue(character), 0f, 1f) : 0f;

            Vector2 boundsSize = CalculateBoundsSize();
            Vector2 topLeft = Utils.GetAnchoredPosition(origin + ParentPos() + Config.Position, boundsSize, Config.Anchor);
            Vector2 center = topLeft + boundsSize / 2f;

            PluginConfigColor healthFillColor = ColorUtils.ColorForCharacter(
                character,
                currentHp,
                maxHp,
                Config.UseJobColor,
                Config.UseRoleColor,
                Config.ColorByHealth
            ) ?? GlobalColors.Instance.SafeColorForJobId(character.ClassJob.RowId);

            float healthRadius = Config.HealthRadius;
            float activeOuterRadius = GetActiveOuterRadius();
            float castRadius = activeOuterRadius + Config.CastArcThickness * 0.5f;
            float lbRadius = activeOuterRadius + Config.LimitBreakArcThickness * 0.5f;

            AddDrawAction(Config.StrataLevel, () =>
            {
                DrawHelper.DrawInWindow(ID + "_Orb", topLeft, boundsSize, false, (drawList) =>
                {
                    DrawHealthOrb(drawList, center, healthRadius, hpRatio, shieldRatio, healthFillColor, Config.HealthBackgroundColor);
                    DrawManaRing(drawList, center, healthRadius, mpRatio);
                    DrawCastArc(drawList, center, castRadius);
                    DrawLimitBreakArc(drawList, center, lbRadius);
                    DrawTankStanceIndicator(drawList, center, character, healthRadius);
                });

                DrawCenterLabels(center, character, currentHp, maxHp);
            });
        }

        private Vector2 CalculateBoundsSize()
        {
            float radius = Config.HealthRadius;

            if (Config.ShowManaRing)
            {
                float manaOuter = Config.HealthRadius + Config.ManaRingGap + Config.ManaRingThickness;
                radius = Math.Max(radius, manaOuter);
            }

            if (Config.ShowCastArc)
            {
            float activeOuter = GetActiveOuterRadius();
            float castOuter = activeOuter + Config.CastArcThickness;
                radius = Math.Max(radius, castOuter);
            }

            if (Config.ShowLimitBreakArc)
            {
            float activeOuter = GetActiveOuterRadius();
            float lbOuter = activeOuter + Config.LimitBreakArcThickness;
                radius = Math.Max(radius, lbOuter);
            }

            float extent = radius + 4f;
            return new Vector2(extent * 2f, extent * 2f);
        }

        private void DrawHealthOrb(ImDrawListPtr drawList, Vector2 center, float radius, float hpRatio, float shieldRatio, PluginConfigColor fillColor, PluginConfigColor backgroundColor)
        {
            drawList.AddCircleFilled(center, radius, backgroundColor.Base, ArcSegments);

            if (hpRatio > 0f)
            {
                float clipTop = center.Y + radius - (radius * 2f * hpRatio);
                Vector2 clipMin = new(center.X - radius - 2f, clipTop);
                Vector2 clipMax = new(center.X + radius + 2f, center.Y + radius + 2f);

                drawList.PushClipRect(clipMin, clipMax, true);
                drawList.AddCircleFilled(center, radius, fillColor.Base, ArcSegments);
                drawList.PopClipRect();
            }

            if (true)
            {
                DrawHealthOrbReimaginedOverlays(drawList, center, radius, hpRatio, fillColor.Base, backgroundColor.Base);
            }
            if (Config.UseHealthTexture && hpRatio > 0f)
            {
                DrawHealthOrbTexture(drawList, center, radius, hpRatio, fillColor, Config.HealthTextureName, Config.HealthTextureDrawMode);
            }
            if (shieldRatio > 0f)
            {
                DrawHealthShieldOverlay(
                    drawList,
                    center,
                    radius,
                    shieldRatio,
                    Config.ShieldColor.Base
                );
            }

            if (Config.ShowHealthBorder)
            {
                DrawHealthBorder(
                    drawList,
                    center,
                    radius,
                    Config.HealthBorderColor.Base,
                    Config.HealthBorderThickness,
                    true
                );
            }
        }

        private void DrawManaRing(ImDrawListPtr drawList, Vector2 center, float healthRadius, float mpRatio)
        {
            if (!Config.ShowManaRing || Config.ManaRingThickness <= 0f)
            {
                return;
            }

            float ringRadius = healthRadius + Config.ManaRingGap + Config.ManaRingThickness * 0.5f;
            float start = -(float)Math.PI / 2f;
            float end = start + (float)Math.PI * 2f;

            DrawArc(drawList, center, ringRadius, start, end, Config.ManaBackgroundColor.Base, Config.ManaRingThickness);

            if (mpRatio > 0f)
            {
                float filledEnd = start + ((float)Math.PI * 2f * mpRatio);
                DrawArc(drawList, center, ringRadius, start, filledEnd, Config.ManaColor.Base, Config.ManaRingThickness);
            }

            if (true)
            {
                if (mpRatio > 0f)
                {
                    float filledEnd = start + ((float)Math.PI * 2f * mpRatio);
                    DrawArc(
                        drawList,
                        center,
                        ringRadius - (Config.ManaRingThickness * 0.20f),
                        start,
                        filledEnd,
                        ModulateBorderColor(Config.ManaColor.Base, 1.35f, 0.45f),
                        Math.Max(1f, Config.ManaRingThickness * 0.24f)
                    );
                    DrawArc(
                        drawList,
                        center,
                        ringRadius + (Config.ManaRingThickness * 0.08f),
                        start,
                        filledEnd,
                        ModulateBorderColor(Config.ManaColor.Base, 0.85f, 0.28f),
                        Math.Max(1f, Config.ManaRingThickness * 0.16f)
                    );
                }
            }

            if (Config.ShowManaBorder)
            {
                DrawArcBorder(
                    drawList,
                    center,
                    ringRadius,
                    start,
                    end,
                    Config.ManaRingThickness,
                    Config.ManaBorderColor.Base,
                    Config.ManaBorderThickness,
                    true
                );
            }
        }

        private void DrawCastArc(ImDrawListPtr drawList, Vector2 center, float radius)
        {
            if (!Config.ShowCastArc || Config.CastArcThickness <= 0f)
            {
                return;
            }

            bool previewMode = Config.PreviewCastArc;
            float currentCast;
            float totalCast;
            if (previewMode)
            {
                currentCast = 0.55f;
                totalCast = 1f;
            }
            else if (!TryGetCastInfo(out currentCast, out totalCast))
            {
                return;
            }

            // Top semicircle (left -> top -> right)
            float start = (float)Math.PI;
            float end = 2f * (float)Math.PI;
            float arcSpan = end - start;
            float ratio = Math.Clamp(currentCast / totalCast, 0f, 1f);
            bool reachedSlidecastWindow = false;
            bool hasCastFill = false;
            float fillStart = start;
            float fillEnd = start;

            DrawArc(drawList, center, radius, start, end, Config.CastArcBackgroundColor.Base, Config.CastArcThickness);

            if (ratio > 0f)
            {
                (fillStart, fillEnd) = ResolveDirectionalArc(start, end, ratio, Config.CastFillDirection);
                hasCastFill = fillEnd > fillStart;
                DrawArc(drawList, center, radius, fillStart, fillEnd, Config.CastArcColor.Base, Config.CastArcThickness);

                if (Config.ShowSlidecastMarker && Config.SlidecastTime > 0)
                {
                    float slidecastRatio = Math.Clamp((totalCast - (Config.SlidecastTime / 1000f)) / totalCast, 0f, 1f);
                    reachedSlidecastWindow = ratio >= slidecastRatio;
                    float markerAngle = ResolveDirectionalAngle(start, end, slidecastRatio, Config.CastFillDirection);
                    Vector2 markerPos = new(
                        center.X + MathF.Cos(markerAngle) * radius,
                        center.Y + MathF.Sin(markerAngle) * radius
                    );

                    drawList.AddCircleFilled(markerPos, Math.Max(2f, Config.CastArcThickness * 0.2f), Config.SlidecastColor.Base, 16);
                }
            }

            if (true)
            {
                if (hasCastFill)
                {
                    DrawArc(
                        drawList,
                        center,
                        radius + (Config.CastArcThickness * 0.08f),
                        fillStart,
                        fillEnd,
                        ModulateBorderColor(Config.CastArcBackgroundColor.Base, 0.55f, 0.58f),
                        Math.Max(1f, Config.CastArcThickness * 0.18f)
                    );
                    DrawArc(
                        drawList,
                        center,
                        radius - (Config.CastArcThickness * 0.18f),
                        fillStart,
                        fillEnd,
                        ModulateBorderColor(Config.CastArcColor.Base, 1.20f, 0.34f),
                        Math.Max(1f, Config.CastArcThickness * 0.22f)
                    );
                }

                if (Config.ShowCastBorder)
                {
                    DrawArcEndCap(
                        drawList,
                        center,
                        radius,
                        start,
                        Config.CastArcThickness,
                        ModulateBorderColor(Config.CastBorderColor.Base, 1.08f, 0.85f),
                        Math.Max(1f, Config.CastBorderThickness * 0.9f)
                    );
                    DrawArcEndCap(
                        drawList,
                        center,
                        radius,
                        end,
                        Config.CastArcThickness,
                        ModulateBorderColor(Config.CastBorderColor.Base, 1.08f, 0.85f),
                        Math.Max(1f, Config.CastBorderThickness * 0.9f)
                    );
                }
            }

            if (Config.ShowCastBorder)
            {
                uint borderColor = reachedSlidecastWindow
                    ? Config.SlidecastBorderColor.Base
                    : Config.CastBorderColor.Base;
                DrawArcBorder(drawList, center, radius, start, end, Config.CastArcThickness, borderColor, Config.CastBorderThickness, true);
            }
        }

        private void DrawLimitBreakArc(ImDrawListPtr drawList, Vector2 center, float radius)
        {
            if (!Config.ShowLimitBreakArc || Config.LimitBreakArcThickness <= 0f)
            {
                return;
            }

            bool previewMode = Config.PreviewLimitBreakArc;
            int bars;
            int max;
            int current;

            if (previewMode)
            {
                // RadioSelector stores zero-based option index; convert to 1/2/3.
                bars = Math.Clamp(Config.PreviewLimitBreakBars + 1, 1, 3);
                max = bars * 1000;
                // Preview pattern: last segment partially filled for clear styling feedback.
                current = bars switch
                {
                    1 => 650,
                    2 => 1450,
                    _ => 2450
                };
            }
            else
            {
                if (!TryGetLimitBreakInfo(out current, out max, out bars) || bars <= 0 || max <= 0)
                {
                    return;
                }
            }

            // Bottom semicircle (right -> bottom -> left)
            float start = 0f;
            float end = (float)Math.PI;
            float totalSpan = end - start;
            float segmentGap = Math.Clamp(Config.LimitBreakSegmentGapDegrees, 0f, 12f) * ((float)Math.PI / 180f);
            float segmentSpan = totalSpan / bars;
            float segmentValue = max / (float)bars;

            for (int i = 0; i < bars; i++)
            {
                // Keep outer edges flush so cast+LB meet as a full ring.
                float segStart = start + segmentSpan * i + (i > 0 ? segmentGap * 0.5f : 0f);
                float segEnd = start + segmentSpan * (i + 1) - (i < bars - 1 ? segmentGap * 0.5f : 0f);
                if (segEnd <= segStart)
                {
                    continue;
                }

                DrawArc(drawList, center, radius, segStart, segEnd, Config.LimitBreakBackgroundColor.Base, Config.LimitBreakArcThickness);

                if (Config.ShowLimitBreakBorder)
                {
                    DrawSegmentArcBorder(
                        drawList,
                        center,
                        radius,
                        segStart,
                        segEnd,
                        Config.LimitBreakArcThickness,
                        Config.LimitBreakBorderColor.Base,
                        Config.LimitBreakBorderThickness,
                        true
                    );
                }

                float segMinValue = i * segmentValue;
                float segMaxValue = (i + 1) * segmentValue;
                if (Config.LimitBreakFillDirection == OrbArcFillDirection.LeftToRight)
                {
                    // Reverse logical fill order while keeping geometry the same.
                    float reverseMin = (bars - i - 1) * segmentValue;
                    float reverseMax = (bars - i) * segmentValue;
                    segMinValue = reverseMin;
                    segMaxValue = reverseMax;
                }

                float fill = Math.Clamp((current - segMinValue) / Math.Max(1f, segMaxValue - segMinValue), 0f, 1f);

                if (fill <= 0f)
                {
                    continue;
                }

                float segmentSpanForFill = segEnd - segStart;
                float fillStart = segStart;
                float fillEnd = segStart + segmentSpanForFill * fill;

                if (Config.LimitBreakFillDirection == OrbArcFillDirection.LeftToRight)
                {
                    // Reverse fill direction inside each segment.
                    fillStart = segEnd - segmentSpanForFill * fill;
                    fillEnd = segEnd;
                }

                uint fillColor = fill >= 1f || !Config.UsePartialLimitBreakColor
                    ? Config.LimitBreakColor.Base
                    : Config.PartialLimitBreakColor.Base;

                DrawArc(drawList, center, radius, fillStart, fillEnd, fillColor, Config.LimitBreakArcThickness);

                if (true)
                {
                    DrawArc(
                        drawList,
                        center,
                        radius + (Config.LimitBreakArcThickness * 0.18f),
                        segStart,
                        segEnd,
                        ModulateBorderColor(Config.LimitBreakBackgroundColor.Base, 0.55f, 0.58f),
                        Math.Max(1f, Config.LimitBreakArcThickness * 0.20f)
                    );

                    DrawArc(
                        drawList,
                        center,
                        radius - (Config.LimitBreakArcThickness * 0.20f),
                        fillStart,
                        fillEnd,
                        ModulateBorderColor(fillColor, 1.28f, 0.42f),
                        Math.Max(1f, Config.LimitBreakArcThickness * 0.24f)
                    );

                    if (Config.ShowLimitBreakBorder)
                    {
                        float capThickness = Math.Max(1f, Config.LimitBreakBorderThickness * 0.95f);
                        uint capColor = ModulateBorderColor(Config.LimitBreakBorderColor.Base, 1.08f, 0.82f);
                        DrawArcEndCap(drawList, center, radius, segStart, Config.LimitBreakArcThickness, capColor, capThickness);
                        DrawArcEndCap(drawList, center, radius, segEnd, Config.LimitBreakArcThickness, capColor, capThickness);
                    }
                }
            }
        }

        private void DrawCenterLabels(Vector2 center, ICharacter character, uint currentHp, uint maxHp)
        {
            _centerLine1Hud.Draw(center, Vector2.Zero, character, null, currentHp, maxHp);
            _centerLine2Hud.Draw(center, Vector2.Zero, character, null, currentHp, maxHp);
            _centerLine3Hud.Draw(center, Vector2.Zero, character, null, currentHp, maxHp);
        }

        private static void DrawArc(ImDrawListPtr drawList, Vector2 center, float radius, float start, float end, uint color, float thickness)
        {
            if (end <= start || thickness <= 0f)
            {
                return;
            }

            drawList.PathArcTo(center, radius, start, end, ArcSegments);
            drawList.PathStroke(color, ImDrawFlags.None, thickness);
        }

        private static (float, float) ResolveDirectionalArc(float start, float end, float ratio, OrbArcFillDirection direction)
        {
            ratio = Math.Clamp(ratio, 0f, 1f);
            float span = end - start;
            if (direction == OrbArcFillDirection.LeftToRight)
            {
                return (start, start + span * ratio);
            }

            return (end - span * ratio, end);
        }

        private static float ResolveDirectionalAngle(float start, float end, float ratio, OrbArcFillDirection direction)
        {
            ratio = Math.Clamp(ratio, 0f, 1f);
            float span = end - start;
            return direction == OrbArcFillDirection.LeftToRight
                ? start + span * ratio
                : end - span * ratio;
        }

        private static void DrawArcBorder(ImDrawListPtr drawList, Vector2 center, float radius, float start, float end, float arcThickness, uint color, int borderThickness, bool useReimaginedStyle)
        {
            if (borderThickness <= 0 || arcThickness <= 0f || end <= start)
            {
                return;
            }

            float half = arcThickness * 0.5f;
            float outerRadius = radius + half;
            float borderPx = Math.Max(1f, borderThickness);
            drawList.PathArcTo(center, outerRadius, start, end, ArcSegments);
            drawList.PathStroke(color, ImDrawFlags.None, borderPx);
        }

        private static void DrawSegmentArcBorder(ImDrawListPtr drawList, Vector2 center, float radius, float start, float end, float arcThickness, uint color, int borderThickness, bool useReimaginedStyle)
        {
            if (borderThickness <= 0 || arcThickness <= 0f || end <= start)
            {
                return;
            }

            float half = arcThickness * 0.5f;
            float innerRadius = Math.Max(1f, radius - half);
            float outerRadius = radius + half;
            float borderPx = Math.Max(1f, borderThickness);
            // Outside outline plus side caps only.
            drawList.PathArcTo(center, outerRadius, start, end, ArcSegments);
            drawList.PathStroke(color, ImDrawFlags.None, borderPx);

            DrawRadialBorderLine(drawList, center, start, innerRadius, outerRadius, color, borderPx, 0f, false);
            DrawRadialBorderLine(drawList, center, end, innerRadius, outerRadius, color, borderPx, 0f, false);
        }

        private static void DrawRadialBorderLine(ImDrawListPtr drawList, Vector2 center, float angle, float innerRadius, float outerRadius, uint color, float thickness, float accentOffset, bool useReimaginedStyle)
        {
            Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 from = center + dir * innerRadius;
            Vector2 to = center + dir * outerRadius;
            drawList.AddLine(from, to, color, thickness);
        }

        private static uint ModulateBorderColor(uint color, float rgbMultiplier, float alphaMultiplier)
        {
            Vector4 rgba = ImGui.ColorConvertU32ToFloat4(color);
            rgba.X = Math.Clamp(rgba.X * rgbMultiplier, 0f, 1f);
            rgba.Y = Math.Clamp(rgba.Y * rgbMultiplier, 0f, 1f);
            rgba.Z = Math.Clamp(rgba.Z * rgbMultiplier, 0f, 1f);
            rgba.W = Math.Clamp(rgba.W * alphaMultiplier, 0f, 1f);
            return ImGui.ColorConvertFloat4ToU32(rgba);
        }

        private static void DrawHealthBorder(ImDrawListPtr drawList, Vector2 center, float radius, uint color, int borderThickness, bool useReimaginedStyle)
        {
            float borderPx = Math.Max(1f, borderThickness);
            drawList.AddCircle(center, radius, color, ArcSegments, borderPx);
        }

        private static void DrawArcEndCap(ImDrawListPtr drawList, Vector2 center, float radius, float angle, float arcThickness, uint color, float lineThickness)
        {
            Vector2 dir = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 inner = center + dir * (radius - arcThickness * 0.5f);
            Vector2 outer = center + dir * (radius + arcThickness * 0.5f);
            drawList.AddLine(inner, outer, color, lineThickness);
        }

        private static void DrawHealthOrbReimaginedOverlays(ImDrawListPtr drawList, Vector2 center, float radius, float hpRatio, uint fillColor, uint backgroundColor)
        {
            if (hpRatio <= 0f || radius <= 1f)
            {
                return;
            }

            float clipTop = center.Y + radius - (radius * 2f * hpRatio);
            Vector2 clipMin = new(center.X - radius - 2f, clipTop);
            Vector2 clipMax = new(center.X + radius + 2f, center.Y + radius + 2f);
            drawList.PushClipRect(clipMin, clipMax, true);

            int lines = Math.Max(20, (int)(radius * 1.1f));
            for (int i = 0; i <= lines; i++)
            {
                float y = center.Y - radius + (2f * radius * i / lines);
                float dy = y - center.Y;
                float xSq = radius * radius - dy * dy;
                if (xSq <= 0f)
                {
                    continue;
                }

                float x = MathF.Sqrt(xSq);
                float t = i / (float)lines;
                uint shade = ModulateBorderColor(fillColor, 0.72f + 0.46f * (1f - t), 0.15f + 0.24f * (1f - t));
                drawList.AddLine(new Vector2(center.X - x, y), new Vector2(center.X + x, y), shade, 1f);
            }

            drawList.PopClipRect();

            DrawArc(
                drawList,
                center,
                radius - Math.Max(1f, radius * 0.06f),
                -2.45f,
                -0.75f,
                ModulateBorderColor(fillColor, 1.28f, 0.34f),
                Math.Max(1f, radius * 0.045f)
            );
            DrawArc(
                drawList,
                center,
                radius + Math.Max(1f, radius * 0.03f),
                0.65f,
                2.60f,
                ModulateBorderColor(backgroundColor, 0.55f, 0.34f),
                Math.Max(1f, radius * 0.05f)
            );
        }

        private static void DrawHealthOrbTexture(
            ImDrawListPtr drawList,
            Vector2 center,
            float radius,
            float hpRatio,
            PluginConfigColor fillColor,
            string textureName,
            BarTextureDrawMode drawMode)
        {
            IDalamudTextureWrap? texture = BarTexturesManager.Instance?.GetBarTexture(textureName);
            if (texture == null)
            {
                return;
            }

            float clipTop = center.Y + radius - (radius * 2f * hpRatio);
            Vector2 clipMin = new(center.X - radius - 2f, clipTop);
            Vector2 clipMax = new(center.X + radius + 2f, center.Y + radius + 2f);

            Vector2 min = new(center.X - radius, center.Y - radius);
            Vector2 max = new(center.X + radius, center.Y + radius);
            Vector2 size = max - min;
            Vector2 uv1 = GetBarTextureUv1(size, texture.Width, texture.Height, drawMode);

            drawList.PushClipRect(clipMin, clipMax, true);
            drawList.AddImageRounded(texture.Handle, min, max, Vector2.Zero, uv1, fillColor.Base, radius);
            drawList.PopClipRect();
        }

        private static void DrawHealthShieldOverlay(ImDrawListPtr drawList, Vector2 center, float radius, float shieldRatio, uint shieldColor)
        {
            if (shieldRatio <= 0f || radius <= 1f)
            {
                return;
            }

            float ratio = Math.Clamp(shieldRatio, 0f, 1f);
            float clipTop = center.Y + radius - (radius * 2f * ratio);
            Vector2 clipMin = new(center.X - radius - 2f, clipTop);
            Vector2 clipMax = new(center.X + radius + 2f, center.Y + radius + 2f);

            drawList.PushClipRect(clipMin, clipMax, true);
            drawList.AddCircleFilled(center, radius - 0.75f, shieldColor, ArcSegments);
            drawList.PopClipRect();

            DrawArc(
                drawList,
                center,
                radius - 1.0f,
                -2.45f,
                -0.80f,
                ModulateBorderColor(shieldColor, 1.22f, 0.55f),
                Math.Max(1f, radius * 0.04f)
            );
        }

        private static Vector2 GetBarTextureUv1(Vector2 size, int textureWidth, int textureHeight, BarTextureDrawMode drawMode)
        {
            if (drawMode == BarTextureDrawMode.Stretch)
            {
                return new Vector2(1f, 1f);
            }

            float x = drawMode == BarTextureDrawMode.RepeatVertical ? 1f : size.X / Math.Max(1, textureWidth);
            float y = drawMode == BarTextureDrawMode.RepeatHorizontal ? 1f : size.Y / Math.Max(1, textureHeight);
            return new Vector2(x, y);
        }

        private void DrawTankStanceIndicator(ImDrawListPtr drawList, Vector2 center, ICharacter character, float healthRadius)
        {
            if (!Config.ShowTankStanceIndicator || character is not IPlayerCharacter || character is not IBattleChara battleChara)
            {
                return;
            }

            if (JobsHelper.RoleForJob(character.ClassJob.RowId) != JobRoles.Tank)
            {
                return;
            }

            bool isActive = HasTankStance(battleChara);
            uint color = isActive ? Config.TankStanceActiveColor.Base : Config.TankStanceInactiveColor.Base;

            float size = Math.Max(6f, Config.TankStanceIndicatorSize);
            // Top-of-orb by default, with user-adjustable offset.
            Vector2 pos = center + new Vector2(0f, -(healthRadius + size * 0.85f)) + Config.TankStanceIndicatorOffset;
            DrawShieldIndicator(drawList, pos, size, color, character.ClassJob.RowId);
        }

        private static void DrawShieldIndicator(ImDrawListPtr drawList, Vector2 center, float size, uint color, uint jobId)
        {
            // Use the current job icon with Style 1 (index 0), matching Nameplates Role/Job icon style behavior.
            uint iconId = JobsHelper.IconIDForJob(jobId, 0);
            if (iconId == 0)
            {
                iconId = JobsHelper.IconIDForJob(JobIDs.PLD, 0);
            }

            Vector2 iconSize = new(size, size);
            Vector2 iconPos = center - iconSize / 2f;
            DrawHelper.DrawIcon(iconId, iconPos, iconSize, false, color, drawList);
        }

        private static bool HasTankStance(IBattleChara chara)
        {
            return Utils.StatusListForBattleChara(chara).Any(status =>
                status.StatusId == 79 ||   // Iron Will
                status.StatusId == 91 ||   // Defiance
                status.StatusId == 392 ||  // Royal Guard
                status.StatusId == 393 ||  // Iron Will (variant)
                status.StatusId == 743 ||  // Grit
                status.StatusId == 1396 || // Defiance (variant)
                status.StatusId == 1397 || // Grit (variant)
                status.StatusId == 1833    // Royal Guard (variant)
            );
        }

        private float GetActiveOuterRadius()
        {
            if (Config.ShowManaRing && Config.ManaRingThickness > 0f)
            {
                return Config.HealthRadius + Config.ManaRingGap + Config.ManaRingThickness;
            }

            return Config.HealthRadius;
        }

        private bool TryGetCastInfo(out float current, out float total)
        {
            current = 0f;
            total = 0f;

            if (Actor is not IBattleChara battleChara)
            {
                return false;
            }

            try
            {
                current = battleChara.CurrentCastTime;
                total = battleChara.TotalCastTime;

                StructsBattleChara* structChara = (StructsBattleChara*)battleChara.Address;
                CastInfo* castInfo = structChara->GetCastInfo();
                if (castInfo != null && castInfo->TotalCastTime > 0f)
                {
                    total = castInfo->TotalCastTime;
                }

                if (total <= 0f || (!Utils.IsActorCasting(battleChara) && current <= 0f))
                {
                    return false;
                }

                return current < total;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetLimitBreakInfo(out int current, out int max, out int bars)
        {
            current = 0;
            max = 0;
            bars = 0;

            LimitBreakController* lbController = LimitBreakController.Instance();
            if (lbController == null)
            {
                return false;
            }

            current = lbController->CurrentUnits;
            max = lbController->BarUnits * lbController->BarCount;
            bars = lbController->BarCount;

            AddonHWDAetherGauge* caGauge = (AddonHWDAetherGauge*)Plugin.GameGui.GetAddonByName("HWDAetherGauge", 1).Address;
            if (caGauge != null)
            {
                current = caGauge->MaxGaugeValue;
                max = 1000;
                bars = 5;
            }

            return bars > 0 && max > 0;
        }
    }
}

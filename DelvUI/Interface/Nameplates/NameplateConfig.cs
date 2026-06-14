using DelvUI.Config;
using DelvUI.Config.Attributes;
using DelvUI.Enums;
using DelvUI.Helpers;
using DelvUI.Interface.Bars;
using DelvUI.Interface.Nameplates;
using DelvUI.Interface.StatusEffects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using ImDrawListPtr = Dalamud.Bindings.ImGui.ImDrawListPtr;
using ImGuiWindowFlags = Dalamud.Bindings.ImGui.ImGuiWindowFlags;
using ImDrawFlags = Dalamud.Bindings.ImGui.ImDrawFlags;

namespace DelvUI.Interface.GeneralElements
{
    public enum NameplatesOcclusionMode
    {
        None = 0,
        Simple = 1,
        Full
    };

    public enum NameplatesOcclusionType
    {
        Walls = 0,
        WallsAndObjects = 1
    };

    [DisableParentSettings("Strata", "Position")]
    [Section("Nameplates")]
    [SubSection("General", 0)]
    public class NameplatesGeneralConfig : MovablePluginConfigObject
    {
        public new static NameplatesGeneralConfig DefaultConfig() => new NameplatesGeneralConfig();

        [Combo("Occlusion Mode", new string[] { "Disabled", "Simple", "Full" }, help = "This controls wheter you'll see nameplates through walls and objects.\n\nDisabled: Nameplates will always be seen for units in range.\nSimple: Uses simple calculations to check if a nameplate is being covered by walls or objects. Use this for better performance.\nFull: Uses more complex calculations to check if a nameplate is being covered by walls or objects. Use this for better results.")]
        [Order(10)]
        public NameplatesOcclusionMode OcclusionMode = NameplatesOcclusionMode.Full;

        [Combo("Occlusion Type", new string[] { "Walls", "Walls and Objects" }, help = "This controls which kind of objects will cover nameplates.\n\n\nWalls: Default setting. Only walls will cover nameplates.\n\nWalls and Objects: Some objects like columns and trees will also cover nameplates.\nThis Occlusion Type can yield some unexpected results like nameplates for NPCs behind counters not being visible.")]
        [Order(11)]
        public NameplatesOcclusionType OcclusionType = NameplatesOcclusionType.Walls;

        [Checkbox("Try to keep nameplates on screen", spacing = true, help = "Disclaimer: Aether UI relies heavily on the game's default nameplates, so this setting won't be a huge improvement.\nThis setting tries to prevent nameplates from being cutoff at the border of the screen, but it won't keep showing nameplates that the game wouldn't.")]
        [Order(20)]
        public bool ClampToScreen = true;

        [Checkbox("Always show nameplate for target")]
        [Order(21)]
        public bool AlwaysShowTargetNameplate = true;

        public int RaycastFlag() => OcclusionType == NameplatesOcclusionType.WallsAndObjects ? 0x2000 : 0x4000;
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Player", 0)]
    public class PlayerNameplateConfig : NameplateWithPlayerBarConfig
    {
        public PlayerNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static PlayerNameplateConfig DefaultConfig()
        {
            return NameplatesHelper.GetNameplateWithBarConfig<PlayerNameplateConfig, NameplatePlayerBarConfig>(
                0xFFD0E5E0,
                0xFF30444A,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
        }
    }

    [DisableParentSettings("HideWhenInactive", "TitleLabelConfig", "SwapLabelsWhenNeeded")]
    [Section("Nameplates")]
    [SubSection("Enemies", 0)]
    public class EnemyNameplateConfig : NameplateWithEnemyBarConfig
    {
        public EnemyNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateEnemyBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static EnemyNameplateConfig DefaultConfig()
        {
            EnemyNameplateConfig config = NameplatesHelper.GetNameplateWithBarConfig<EnemyNameplateConfig, NameplateEnemyBarConfig>(
                0xFF993535,
                0xFF000000,
                HUDConstants.DefaultEnemyNameplateBarSize
            );

            config.SwapLabelsWhenNeeded = false;

            config.NameLabelConfig.Position = new Vector2(-8, 0);
            config.NameLabelConfig.Text = "Lv[level] [name]";
            config.NameLabelConfig.FrameAnchor = DrawAnchor.TopRight;
            config.NameLabelConfig.TextAnchor = DrawAnchor.Right;
            config.NameLabelConfig.Color = PluginConfigColor.FromHex(0xFFFFFFFF);

            config.BarConfig.LeftLabelConfig.Enabled = true;
            config.BarConfig.OnlyShowWhenNotFull = false;

            // debuffs
            LabelConfig durationConfig = new LabelConfig(new Vector2(0, -4), "", DrawAnchor.Bottom, DrawAnchor.Center);
            durationConfig.FontID = FontsConfig.DefaultMediumFontKey;

            LabelConfig stacksConfig = new LabelConfig(new Vector2(-3, 4), "", DrawAnchor.TopRight, DrawAnchor.Center);
            durationConfig.FontID = FontsConfig.DefaultMediumFontKey;
            stacksConfig.Color = new(Vector4.UnitW);
            stacksConfig.OutlineColor = new(Vector4.One);

            StatusEffectIconConfig iconConfig = new StatusEffectIconConfig(durationConfig, stacksConfig);
            iconConfig.Size = new Vector2(30, 30);
            iconConfig.DispellableBorderConfig.Enabled = false;

            Vector2 pos = new Vector2(2, -20);
            Vector2 size = new Vector2(230, 70);

            EnemyNameplateStatusEffectsListConfig debuffs = new EnemyNameplateStatusEffectsListConfig(
                DrawAnchor.TopLeft,
                pos,
                size,
                false,
                true,
                false,
                GrowthDirections.Right | GrowthDirections.Up,
                iconConfig
            );
            debuffs.Limit = 7;
            debuffs.ShowPermanentEffects = true;
            debuffs.IconConfig.DispellableBorderConfig.Enabled = false;
            debuffs.IconPadding = new Vector2(1, 6);
            debuffs.ShowOnlyMine = true;
            debuffs.ShowTooltips = false;
            debuffs.DisableInteraction = true;
            config.DebuffsConfig = debuffs;

            // castbar
            Vector2 castbarSize = new Vector2(config.BarConfig.Size.X, 10);

            LabelConfig castNameConfig = new LabelConfig(new Vector2(0, -1), "", DrawAnchor.Center, DrawAnchor.Center);
            castNameConfig.FontID = FontsConfig.DefaultSmallFontKey;

            NumericLabelConfig castTimeConfig = new NumericLabelConfig(new Vector2(-5, 0), "", DrawAnchor.Right, DrawAnchor.Right);
            castTimeConfig.Enabled = false;
            castTimeConfig.FontID = FontsConfig.DefaultSmallFontKey;
            castTimeConfig.NumberFormat = 1;

            NameplateCastbarConfig castbarConfig = new NameplateCastbarConfig(Vector2.Zero, castbarSize, castNameConfig, castTimeConfig);
            castbarConfig.HealthBarAnchor = DrawAnchor.BottomLeft;
            castbarConfig.Anchor = DrawAnchor.TopLeft;
            castbarConfig.ShowIcon = false;
            config.CastbarConfig = castbarConfig;

            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive", "TitleLabelConfig", "SwapLabelsWhenNeeded")]
    public class BossesNameplateConfig : NameplateWithEnemyBarConfig
    {
        [Checkbox("Use Native Boss/Elite Indicators", help = "When enabled, Aether UI uses available game-native nameplate indicators first to detect bosses/elites.")]
        [Order(5)]
        public bool UseNativeBossIndicators = true;

        [Checkbox("Keep Boss Plate During Combat", help = "Keeps the Bosses plate visible while you remain in combat, even if the boss is temporarily off-screen.")]
        [Order(6)]
        public bool KeepVisibleDuringCombat = true;

        [DragInt("Fallback Min Max HP", min = 1, max = 100000000, velocity = 5000f, help = "Fallback heuristic when native indicators are missing. Units at or above this max HP can be treated as boss/elite.")]
        [Order(7)]
        public int FallbackMinMaxHp = 300000;

        [DragFloat("Fallback Min Hitbox Radius", min = 0, max = 50, velocity = 0.25f, help = "Additional fallback heuristic. Large hitbox enemies can be treated as boss/elite.")]
        [Order(8)]
        public float FallbackMinHitboxRadius = 7.5f;

        public BossesNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateEnemyBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static BossesNameplateConfig DefaultConfig()
        {
            BossesNameplateConfig config = NameplatesHelper.GetNameplateWithBarConfig<BossesNameplateConfig, NameplateEnemyBarConfig>(
                0xFFE4CC76,
                0xFF000000,
                new Vector2(340, 28)
            );

            // Ensure enemy-specific nested configs exist before mutating them.
            // Some helper paths do not initialize these for derived enemy configs.
            EnemyNameplateConfig enemyDefaults = EnemyNameplateConfig.DefaultConfig();
            config.DebuffsConfig ??= enemyDefaults.DebuffsConfig;
            config.CastbarConfig ??= enemyDefaults.CastbarConfig;

            config.Position = new Vector2(0, -250);
            config.SwapLabelsWhenNeeded = false;

            config.NameLabelConfig.Position = new Vector2(0, -24);
            config.NameLabelConfig.Text = "Lv[level] [name]";
            config.NameLabelConfig.FrameAnchor = DrawAnchor.Top;
            config.NameLabelConfig.TextAnchor = DrawAnchor.Bottom;
            config.NameLabelConfig.FontID = FontsConfig.DefaultMediumFontKey;
            config.NameLabelConfig.Color = PluginConfigColor.FromHex(0xFFFFFFFF);

            config.BarConfig.LeftLabelConfig.Enabled = true;
            config.BarConfig.LeftLabelConfig.Text = "[health:current-short]";
            config.BarConfig.RightLabelConfig.Enabled = true;
            config.BarConfig.RightLabelConfig.Text = "[health:percent-short]";
            config.BarConfig.OptionalLabelConfig.Enabled = false;
            config.BarConfig.OnlyShowWhenNotFull = false;
            config.BarConfig.HideHealthAtZero = false;
            config.BarConfig.DrawBorder = true;
            config.BarConfig.TargetedBorderThickness = 2;

            config.IconConfig.Enabled = true;
            config.IconConfig.Size = new Vector2(42, 42);
            config.IconConfig.Position = new Vector2(-10, 0);

            config.DebuffsConfig.Enabled = false;
            config.CastbarConfig.Enabled = false;
            config.BarConfig.OrderLabelConfig.Enabled = false;

            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Party Members", 0)]
    public class PartyMembersNameplateConfig : NameplateWithPlayerBarConfig
    {
        public PartyMembersNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static PartyMembersNameplateConfig DefaultConfig()
        {
            PartyMembersNameplateConfig config = NameplatesHelper.GetNameplateWithBarConfig<PartyMembersNameplateConfig, NameplatePlayerBarConfig>(
                0xFFD0E5E0,
                0xFF000000,
                HUDConstants.DefaultPlayerNameplateBarSize
            );

            config.BarConfig.UseRoleColor = true;
            config.NameLabelConfig.UseRoleColor = true;
            config.TitleLabelConfig.UseRoleColor = true;
            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Alliance Members", 0)]
    public class AllianceMembersNameplateConfig : NameplateWithPlayerBarConfig
    {
        public AllianceMembersNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static AllianceMembersNameplateConfig DefaultConfig()
        {
            return NameplatesHelper.GetNameplateWithBarConfig<AllianceMembersNameplateConfig, NameplatePlayerBarConfig>(
                0xFF99BE46,
                0xFF3D4C1C,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Friends", 0)]
    public class FriendPlayerNameplateConfig : NameplateWithPlayerBarConfig
    {
        public FriendPlayerNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static FriendPlayerNameplateConfig DefaultConfig()
        {
            return NameplatesHelper.GetNameplateWithBarConfig<FriendPlayerNameplateConfig, NameplatePlayerBarConfig>(
                0xFFEB6211,
                0xFF4A2008,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Other Players", 0)]
    public class OtherPlayerNameplateConfig : NameplateWithPlayerBarConfig
    {
        public OtherPlayerNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static OtherPlayerNameplateConfig DefaultConfig()
        {
            return NameplatesHelper.GetNameplateWithBarConfig<OtherPlayerNameplateConfig, NameplatePlayerBarConfig>(
                0xFF91BBD8,
                0xFF33434E,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("Pets", 0)]
    public class PetNameplateConfig : NameplateWithNPCBarConfig
    {
        public PetNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static PetNameplateConfig DefaultConfig()
        {
            PetNameplateConfig config = NameplatesHelper.GetNameplateWithBarConfig<PetNameplateConfig, NameplateBarConfig>(
                0xFFD1E5C8,
                0xFF2A2F28,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
            config.OnlyShowWhenTargeted = true;
            config.SwapLabelsWhenNeeded = false;
            config.NameLabelConfig.Text = "Lv[level] [name]";
            config.NameLabelConfig.FontID = FontsConfig.DefaultSmallFontKey;
            config.TitleLabelConfig.FontID = FontsConfig.DefaultSmallFontKey;

            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive")]
    [Section("Nameplates")]
    [SubSection("NPCs", 0)]
    public class NPCNameplateConfig : NameplateWithNPCBarConfig
    {
        public NPCNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig, barConfig)
        {
        }

        public new static NPCNameplateConfig DefaultConfig()
        {
            NPCNameplateConfig config = NameplatesHelper.GetNameplateWithBarConfig<NPCNameplateConfig, NameplateBarConfig>(
                0xFFD1E5C8,
                0xFF3A4b1E,
                HUDConstants.DefaultPlayerNameplateBarSize
            );
            config.NameLabelConfig.Position = new Vector2(0, -20);
            config.TitleLabelConfig.Position = Vector2.Zero;

            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive", "SwapLabelsWhenNeeded")]
    [Section("Nameplates")]
    [SubSection("Minions", 0)]
    public class MinionNPCNameplateConfig : NameplateConfig
    {
        public MinionNPCNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig)
            : base(position, nameLabel, titleLabelConfig)
        {
        }

        public new static MinionNPCNameplateConfig DefaultConfig()
        {
            MinionNPCNameplateConfig config = NameplatesHelper.GetNameplateConfig<MinionNPCNameplateConfig>(0xFFFFFFFF, 0xFF000000);
            config.OnlyShowWhenTargeted = true;
            config.SwapLabelsWhenNeeded = false;
            config.NameLabelConfig.Position = new Vector2(0, -17);
            config.NameLabelConfig.FontID = FontsConfig.DefaultSmallFontKey;
            config.TitleLabelConfig.Position = new Vector2(0, 0);
            config.TitleLabelConfig.FontID = FontsConfig.DefaultSmallFontKey;

            return config;
        }
    }

    [DisableParentSettings("HideWhenInactive", "SwapLabelsWhenNeeded")]
    [Section("Nameplates")]
    [SubSection("Objects", 0)]
    public class ObjectsNameplateConfig : NameplateConfig
    {
        public ObjectsNameplateConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig)
            : base(position, nameLabel, titleLabelConfig)
        {
        }

        public new static ObjectsNameplateConfig DefaultConfig()
        {
            ObjectsNameplateConfig config = NameplatesHelper.GetNameplateConfig<ObjectsNameplateConfig>(0xFFFFFFFF, 0xFF000000);
            config.SwapLabelsWhenNeeded = false;

            return config;
        }
    }

    public class NameplateConfig : MovablePluginConfigObject
    {
        [Checkbox("Only show when targeted")]
        [Order(1)]
        public bool OnlyShowWhenTargeted = false;

        [SliderFloat("Plate Scale", min = 0.5f, max = 2.5f, format = "%.2fx")]
        [Order(2)]
        public float PlateScale = 1f;

        [Checkbox("Swap Name and Title labels when needed", spacing = true, help = "This will swap the contents of these labels depending on if the title goes before or after the name of a player.")]
        [Order(20)]
        public bool SwapLabelsWhenNeeded = true;

        [NestedConfig("Name Label", 21)]
        public EditableLabelConfig NameLabelConfig = null!;

        [NestedConfig("Title Label", 22)]
        public EditableNonFormattableLabelConfig TitleLabelConfig = null!;

        [NestedConfig("Change Alpha Based on Range", 145)]
        public NameplateRangeConfig RangeConfig = new();

        [NestedConfig("Visibility", 200)]
        public VisibilityConfig VisibilityConfig = new VisibilityConfig();

        [ManualDraw]
        [ManualDrawPriority(-10)]
        public bool DrawTabPreview(ref bool changed)
        {
            NameplatesTabPreviewRenderer.DrawForConfig(this);
            return false;
        }

        public NameplateConfig(Vector2 position, EditableLabelConfig nameLabelConfig, EditableNonFormattableLabelConfig titleLabelConfig)
            : base()
        {
            Position = position;
            NameLabelConfig = nameLabelConfig;
            TitleLabelConfig = titleLabelConfig;
        }

        public NameplateConfig() : base() { } // don't remove
    }

    public interface NameplateWithBarConfig
    {
        public NameplateBarConfig GetBarConfig();
    }

    public class NameplateWithNPCBarConfig : NameplateConfig, NameplateWithBarConfig
    {
        [NestedConfig("Health Bar", 40)]
        public NameplateBarConfig BarConfig = null!;

        public NameplateBarConfig GetBarConfig() => BarConfig;

        public NameplateWithNPCBarConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig)
        {
            BarConfig = barConfig;
        }

        public NameplateWithNPCBarConfig() : base() { } // don't remove
    }

    public class NameplateWithPlayerBarConfig : NameplateConfig, NameplateWithBarConfig
    {
        [NestedConfig("Health Bar", 40)]
        public NameplatePlayerBarConfig BarConfig = null!;

        [NestedConfig("Role/Job Icon", 50)]
        public NameplateRoleJobIconConfig RoleIconConfig = new NameplateRoleJobIconConfig(
            new Vector2(-5, 0),
            new Vector2(30, 30),
            DrawAnchor.Right,
            DrawAnchor.Left
        )
        { Strata = StrataLevel.LOWEST };

        [NestedConfig("Player State Icon", 55)]
        public NameplatePlayerIconConfig StateIconConfig = new NameplatePlayerIconConfig(
            new Vector2(5, 0),
            new Vector2(30, 30),
            DrawAnchor.Left,
            DrawAnchor.Right
        )
        { Strata = StrataLevel.LOWEST };

        public NameplateBarConfig GetBarConfig() => BarConfig;

        public NameplateWithPlayerBarConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplatePlayerBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig)
        {
            BarConfig = barConfig;
        }

        public NameplateWithPlayerBarConfig() : base() { } // don't remove
    }

    public class NameplateWithEnemyBarConfig : NameplateConfig, NameplateWithBarConfig
    {
        [NestedConfig("Health Bar", 40)]
        public NameplateEnemyBarConfig BarConfig = null!;

        [NestedConfig("Icon", 45)]
        public NameplateIconConfig IconConfig = new NameplateIconConfig(
            new Vector2(0, 0),
            new Vector2(40, 40),
            DrawAnchor.Right,
            DrawAnchor.Left
        )
        { PrioritizeHealthBarAnchor = true, Strata = StrataLevel.LOWEST };

        [NestedConfig("Debuffs", 50)]
        public EnemyNameplateStatusEffectsListConfig DebuffsConfig = null!;

        [NestedConfig("Castbar", 55)]
        public NameplateCastbarConfig CastbarConfig = null!;

        public NameplateBarConfig GetBarConfig() => BarConfig;

        public NameplateWithEnemyBarConfig(
            Vector2 position,
            EditableLabelConfig nameLabel,
            EditableNonFormattableLabelConfig titleLabelConfig,
            NameplateEnemyBarConfig barConfig)
            : base(position, nameLabel, titleLabelConfig)
        {
            BarConfig = barConfig;
        }

        public NameplateWithEnemyBarConfig() : base() { } // don't remove
    }

    [DisableParentSettings("HideWhenInactive")]
    public class NameplateBarConfig : BarConfig
    {
        [Checkbox("Only Show when not at full Health")]
        [Order(1)]
        public bool OnlyShowWhenNotFull = true;

        [Checkbox("Hide Health when fully depleted", help = "This will hide the healthbar when the characters HP has been brought to zero")]
        [Order(2)]
        public bool HideHealthAtZero = true;

        [Checkbox("Disable Interaction")]
        [Order(3)]
        public bool DisableInteraction = false;

        [Checkbox("Use Different Size when targeted", spacing = true)]
        [Order(31)]
        public bool UseDifferentSizeWhenTargeted = false;

        [DragInt2("Size When Targeted", min = 1, max = 4000)]
        [Order(32, collapseWith = nameof(UseDifferentSizeWhenTargeted))]
        public Vector2 SizeWhenTargeted;

        [ColorEdit4("Targeted Border Color")]
        [Order(38, collapseWith = nameof(DrawBorder))]
        public PluginConfigColor TargetedBorderColor = PluginConfigColor.FromHex(0xFFFFFFFF);

        [DragInt("Targeted Border Thickness", min = 1, max = 10)]
        [Order(39, collapseWith = nameof(DrawBorder))]
        public int TargetedBorderThickness = 2;

        [NestedConfig("Color Based On Health Value", 50, collapsingHeader = false)]
        public ColorByHealthValueConfig ColorByHealth = new ColorByHealthValueConfig();

        [Checkbox("Hide Health if Possible", spacing = true, help = "This will hide any label that has a health tag if the character doesn't have health (ie minions, friendly npcs, etc)")]
        [Order(121)]
        public bool HideHealthIfPossible = true;

        [NestedConfig("Left Text", 125)]
        public EditableLabelConfig LeftLabelConfig = null!;

        [NestedConfig("Right Text", 130)]
        public EditableLabelConfig RightLabelConfig = null!;

        [NestedConfig("Optional Text", 131)]
        public EditableLabelConfig OptionalLabelConfig = null!;

        [NestedConfig("Shields", 140)]
        public ShieldConfig ShieldConfig = new ShieldConfig();

        [NestedConfig("Custom Mouseover Area", 150)]
        public MouseoverAreaConfig MouseoverAreaConfig = new MouseoverAreaConfig();

        public NameplateBarConfig(Vector2 position, Vector2 size, EditableLabelConfig leftLabelConfig, EditableLabelConfig rightLabelConfig, EditableLabelConfig optionalLabelConfig)
            : base(position, size, new PluginConfigColor(new(40f / 255f, 40f / 255f, 40f / 255f, 100f / 100f)))
        {
            Position = position;
            Size = size;
            LeftLabelConfig = leftLabelConfig;
            RightLabelConfig = rightLabelConfig;
            OptionalLabelConfig = optionalLabelConfig;
            BackgroundColor = new PluginConfigColor(new(0f / 255f, 0f / 255f, 0f / 255f, 100f / 100f));
            ColorByHealth.Enabled = false;
            MouseoverAreaConfig.Enabled = false;
        }

        public bool IsVisible(uint hp, uint maxHp)
        {
            return Enabled && (!OnlyShowWhenNotFull || hp < maxHp) && !(HideHealthAtZero && hp <= 0);
        }

        public Vector2 GetSize(bool targeted)
        {
            return targeted && UseDifferentSizeWhenTargeted ? SizeWhenTargeted : Size;
        }

        public NameplateBarConfig() : base(Vector2.Zero, Vector2.Zero, PluginConfigColor.Empty) { } // don't remove
    }

    public class NameplatePlayerBarConfig : NameplateBarConfig
    {
        [Checkbox("Use Job Color", spacing = true)]
        [Order(45)]
        public bool UseJobColor = false;

        [Checkbox("Use Role Color")]
        [Order(46)]
        public bool UseRoleColor = false;

        [Checkbox("Job Color As Background Color", spacing = true)]
        [Order(50)]
        public bool UseJobColorAsBackgroundColor = false;

        [Checkbox("Role Color As Background Color")]
        [Order(51)]
        public bool UseRoleColorAsBackgroundColor = false;

        public NameplatePlayerBarConfig(Vector2 position, Vector2 size, EditableLabelConfig leftLabelConfig, EditableLabelConfig rightLabelConfig, EditableLabelConfig optionalLabelConfig)
            : base(position, size, leftLabelConfig, rightLabelConfig, optionalLabelConfig)
        {
        }
    }

    public class NameplateEnemyBarConfig : NameplateBarConfig
    {
        [Checkbox("Use State Colors", spacing = true)]
        [Order(45)]
        public bool UseStateColor = true;

        [ColorEdit4("Unengaged")]
        [Order(46, collapseWith = nameof(UseStateColor))]
        public PluginConfigColor UnengagedColor = PluginConfigColor.FromHex(0xFFDA9D2E);

        [ColorEdit4("Unengaged (Hostile)")]
        [Order(47, collapseWith = nameof(UseStateColor))]
        public PluginConfigColor UnengagedHostileColor = PluginConfigColor.FromHex(0xFF994B35);

        [ColorEdit4("Engaged")]
        [Order(48, collapseWith = nameof(UseStateColor))]
        public PluginConfigColor EngagedColor = PluginConfigColor.FromHex(0xFF993535);

        [ColorEdit4("Claimed")]
        [Order(49, collapseWith = nameof(UseStateColor))]
        public PluginConfigColor ClaimedColor = PluginConfigColor.FromHex(0xFFEA93EA);

        [ColorEdit4("Unclaimed")]
        [Order(50, collapseWith = nameof(UseStateColor))]
        public PluginConfigColor UnclaimedColor = PluginConfigColor.FromHex(0xFFE5BB9E);

        [Checkbox("Use Custom Color when being targeted", spacing = true, help = "This will change the color of the bar when the enemy is targeting the player.")]
        [Order(51)]
        public bool UseCustomColorWhenBeingTargeted = false;

        [ColorEdit4("Targeted")]
        [Order(52, collapseWith = nameof(UseCustomColorWhenBeingTargeted))]
        public PluginConfigColor CustomColorWhenBeingTargeted = PluginConfigColor.FromHex(0xFFC4216D);

        [NestedConfig("Order Label", 132)]
        public DefaultFontLabelConfig OrderLabelConfig = new DefaultFontLabelConfig(new Vector2(5, 0), "", DrawAnchor.Right, DrawAnchor.Left)
        {
            Strata = StrataLevel.LOWEST
        };

        public NameplateEnemyBarConfig(Vector2 position, Vector2 size, EditableLabelConfig leftLabelConfig, EditableLabelConfig rightLabelConfig, EditableLabelConfig optionalLabelConfig)
            : base(position, size, leftLabelConfig, rightLabelConfig, optionalLabelConfig)
        {

        }
    }

    [Exportable(false)]
    public class NameplateRangeConfig : PluginConfigObject
    {
        [DragInt("Fade start range (yalms)", min = 1, max = 500)]
        [Order(5)]
        public int StartRange = 50;

        [DragInt("Fade end range (yalms)", min = 1, max = 500)]
        [Order(10)]
        public int EndRange = 64;

        public float AlphaForDistance(float distance, float maxAlpha = 1f)
        {
            float diff = distance - StartRange;
            if (!Enabled || diff <= 0)
            {
                return maxAlpha;
            }

            float a = diff / (EndRange - StartRange);
            return Math.Max(0, Math.Min(maxAlpha, 1 - a));
        }
    }

    public class EnemyNameplateStatusEffectsListConfig : StatusEffectsListConfig
    {
        [Anchor("Health Bar Anchor")]
        [Order(4)]
        public DrawAnchor HealthBarAnchor = DrawAnchor.BottomLeft;

        public EnemyNameplateStatusEffectsListConfig(DrawAnchor anchor, Vector2 position, Vector2 size, bool showBuffs, bool showDebuffs, bool showPermanentEffects,
            GrowthDirections growthDirections, StatusEffectIconConfig iconConfig)
            : base(position, size, showBuffs, showDebuffs, showPermanentEffects, growthDirections, iconConfig)
        {
            HealthBarAnchor = anchor;
        }
    }

    [DisableParentSettings("AnchorToUnitFrame", "UnitFrameAnchor", "HideWhenInactive", "FillDirection")]
    public class NameplateCastbarConfig : TargetCastbarConfig
    {
        [Checkbox("Match Width with Health Bar")]
        [Order(11)]
        public bool MatchWidth = false;

        [Checkbox("Match Height with Health Bar")]
        [Order(12)]
        public bool MatchHeight = false;

        [Anchor("Health Bar Anchor")]
        [Order(16)]
        public DrawAnchor HealthBarAnchor = DrawAnchor.BottomLeft;

        public NameplateCastbarConfig(Vector2 position, Vector2 size, LabelConfig castNameConfig, NumericLabelConfig castTimeConfig)
            : base(position, size, castNameConfig, castTimeConfig)
        {

        }
    }

    internal static class NameplatesHelper
    {
        internal static T GetNameplateConfig<T>(uint bgColor, uint borderColor) where T : NameplateConfig
        {
            EditableLabelConfig nameLabelConfig = new EditableLabelConfig(new Vector2(0, 0), "[name]", DrawAnchor.Top, DrawAnchor.Bottom)
            {
                Color = PluginConfigColor.FromHex(bgColor),
                OutlineColor = PluginConfigColor.FromHex(borderColor),
                FontID = FontsConfig.DefaultMediumFontKey
            };

            EditableNonFormattableLabelConfig titleLabelConfig = new EditableNonFormattableLabelConfig(new Vector2(0, -25), "<[title]>", DrawAnchor.Top, DrawAnchor.Bottom)
            {
                Color = PluginConfigColor.FromHex(bgColor),
                OutlineColor = PluginConfigColor.FromHex(borderColor),
                FontID = FontsConfig.DefaultMediumFontKey
            };

            return (T)Activator.CreateInstance(typeof(T), Vector2.Zero, nameLabelConfig, titleLabelConfig)!;
        }

        internal static T GetNameplateWithBarConfig<T, B>(uint bgColor, uint borderColor, Vector2 barSize)
            where T : NameplateConfig
            where B : NameplateBarConfig
        {
            EditableLabelConfig leftLabelConfig = new EditableLabelConfig(new Vector2(5, 0), "[health:current-short]", DrawAnchor.Left, DrawAnchor.Left)
            {
                Enabled = false,
                FontID = FontsConfig.DefaultMediumFontKey,
                Strata = StrataLevel.LOWEST
            };
            EditableLabelConfig rightLabelConfig = new EditableLabelConfig(new Vector2(-5, 0), "", DrawAnchor.Right, DrawAnchor.Right)
            {
                Enabled = false,
                FontID = FontsConfig.DefaultMediumFontKey,
                Strata = StrataLevel.LOWEST
            };
            EditableLabelConfig optionalLabelConfig = new EditableLabelConfig(new Vector2(0, 0), "", DrawAnchor.Center, DrawAnchor.Center)
            {
                Enabled = false,
                FontID = FontsConfig.DefaultSmallFontKey,
                Strata = StrataLevel.LOWEST
            };

            var barConfig = Activator.CreateInstance(typeof(B), new Vector2(0, -5), barSize, leftLabelConfig, rightLabelConfig, optionalLabelConfig)!;
            if (barConfig is BarConfig bar)
            {
                bar.FillColor = PluginConfigColor.FromHex(bgColor);
                bar.BackgroundColor = PluginConfigColor.FromHex(0xAA000000);
            }

            if (barConfig is NameplateBarConfig nameplateBar)
            {
                nameplateBar.SizeWhenTargeted = nameplateBar.Size;
            }

            EditableLabelConfig nameLabelConfig = new EditableLabelConfig(new Vector2(0, -20), "[name]", DrawAnchor.Top, DrawAnchor.Bottom)
            {
                Color = PluginConfigColor.FromHex(bgColor),
                OutlineColor = PluginConfigColor.FromHex(borderColor),
                FontID = FontsConfig.DefaultMediumFontKey,
                Strata = StrataLevel.LOWEST
            };
            EditableNonFormattableLabelConfig titleLabelConfig = new EditableNonFormattableLabelConfig(new Vector2(0, 0), "<[title]>", DrawAnchor.Top, DrawAnchor.Bottom)
            {
                Color = PluginConfigColor.FromHex(bgColor),
                OutlineColor = PluginConfigColor.FromHex(borderColor),
                FontID = FontsConfig.DefaultMediumFontKey,
                Strata = StrataLevel.LOWEST
            };

            return (T)Activator.CreateInstance(typeof(T), Vector2.Zero, nameLabelConfig, titleLabelConfig, barConfig)!;
        }
    }

    internal static class NameplatesTabPreviewRenderer
    {
        private const string PreviewName = "Aether UI Preview Name";
        private const string PreviewTitle = "<Aether UI Preview Title>";
        private static readonly Dictionary<string, Nameplate> RendererCache = new Dictionary<string, Nameplate>();
        private static readonly Dictionary<string, float> PreviewZoomByType = new Dictionary<string, float>();
        internal static float ActiveTextScale { get; set; } = 1f;

        internal static void DrawForConfig(NameplateConfig config)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Live preview for this Nameplate tab");
            string zoomKey = config.GetType().Name;
            float zoom = PreviewZoomByType.TryGetValue(zoomKey, out float storedZoom) ? storedZoom : 1.0f;

            ImGui.SetNextItemWidth(180f);
            if (ImGui.SliderFloat($"Preview Zoom##AetherUI_NameplatePreviewZoom_{zoomKey}", ref zoom, 0.5f, 1.75f, "%.2fx"))
            {
                PreviewZoomByType[zoomKey] = zoom;
            }

            ImGui.SameLine();
            if (ImGui.Button($"Reset##AetherUI_NameplatePreviewZoomReset_{zoomKey}"))
            {
                zoom = 1.0f;
                PreviewZoomByType[zoomKey] = zoom;
            }

            Vector2 requestedSize = new Vector2(Math.Max(320, ImGui.GetContentRegionAvail().X), 260);
            bool begin = ImGui.BeginChild(
                $"AetherUI_NameplateTabPreview_{config.GetType().Name}",
                requestedSize,
                true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
            );

            if (begin)
            {
                Vector2 canvasPos = ImGui.GetCursorScreenPos();
                Vector2 canvasSize = ImGui.GetContentRegionAvail();
                var drawList = ImGui.GetWindowDrawList();

                drawList.AddRectFilled(canvasPos, canvasPos + canvasSize, 0x22101822);
                drawList.AddRect(canvasPos, canvasPos + canvasSize, 0x553C4A5F);

                float scale = ComputePreviewScale(config, canvasSize) * zoom;

                DrawExactPreview(drawList, config, canvasPos, canvasSize, scale);
            }

            ImGui.EndChild();
        }

        private static void DrawExactPreview(ImDrawListPtr drawList, NameplateConfig config, Vector2 canvasPos, Vector2 canvasSize, float scale)
        {
            Vector2 originalPosition = config.Position;
            bool originalOnlyShowWhenTargeted = config.OnlyShowWhenTargeted;
            bool hasEnemyCastbar = config is NameplateWithEnemyBarConfig;
            bool originalCastbarPreview = false;
            bool originalDebuffsPreview = false;
            Vector2 originalNameLabelPosition = config.NameLabelConfig.Position;
            Vector2 originalTitleLabelPosition = config.TitleLabelConfig.Position;
            Vector2? originalBarPosition = null;
            Vector2? originalBarSize = null;
            Vector2? originalIconPosition = null;
            Vector2? originalIconSize = null;
            Vector2? originalCastbarPosition = null;
            Vector2? originalCastbarSize = null;
            Vector2? originalCastbarNameLabelPosition = null;
            Vector2? originalCastbarTimeLabelPosition = null;
            Vector2? originalDebuffsPosition = null;
            Vector2? originalDebuffsSize = null;
            Vector2? originalDebuffsIconSize = null;
            Vector2? originalDebuffsIconPadding = null;
            Vector2? originalDebuffsDurationLabelPosition = null;
            Vector2? originalDebuffsStacksLabelPosition = null;
            Vector2? originalRoleIconPosition = null;
            Vector2? originalRoleIconSize = null;
            Vector2? originalStateIconPosition = null;
            Vector2? originalStateIconSize = null;
            try
            {
                config.Position = Vector2.Zero;
                config.OnlyShowWhenTargeted = false;
                config.NameLabelConfig.Position = config.NameLabelConfig.Position * scale;
                config.TitleLabelConfig.Position = config.TitleLabelConfig.Position * scale;

                if (config is NameplateWithBarConfig withBar)
                {
                    NameplateBarConfig barConfig = withBar.GetBarConfig();
                    originalBarPosition = barConfig.Position;
                    originalBarSize = barConfig.Size;
                    barConfig.Position = barConfig.Position * scale;
                    barConfig.Size *= scale;
                }

                if (hasEnemyCastbar)
                {
                    NameplateWithEnemyBarConfig enemyConfig = (NameplateWithEnemyBarConfig)config;
                    originalCastbarPreview = enemyConfig.CastbarConfig.Preview;
                    originalDebuffsPreview = enemyConfig.DebuffsConfig.Preview;
                    originalIconPosition = enemyConfig.IconConfig.Position;
                    originalIconSize = enemyConfig.IconConfig.Size;
                    originalCastbarPosition = enemyConfig.CastbarConfig.Position;
                    originalCastbarSize = enemyConfig.CastbarConfig.Size;
                    originalCastbarNameLabelPosition = enemyConfig.CastbarConfig.CastNameLabel.Position;
                    originalCastbarTimeLabelPosition = enemyConfig.CastbarConfig.CastTimeLabel.Position;
                    originalDebuffsPosition = enemyConfig.DebuffsConfig.Position;
                    originalDebuffsSize = enemyConfig.DebuffsConfig.Size;
                    originalDebuffsIconSize = enemyConfig.DebuffsConfig.IconConfig.Size;
                    originalDebuffsIconPadding = enemyConfig.DebuffsConfig.IconPadding;
                    originalDebuffsDurationLabelPosition = enemyConfig.DebuffsConfig.IconConfig.DurationLabelConfig.Position;
                    originalDebuffsStacksLabelPosition = enemyConfig.DebuffsConfig.IconConfig.StacksLabelConfig.Position;

                    enemyConfig.IconConfig.Position = enemyConfig.IconConfig.Position * scale;
                    enemyConfig.IconConfig.Size *= scale;
                    enemyConfig.CastbarConfig.Position = enemyConfig.CastbarConfig.Position * scale;
                    enemyConfig.CastbarConfig.Size *= scale;
                    enemyConfig.CastbarConfig.CastNameLabel.Position = enemyConfig.CastbarConfig.CastNameLabel.Position * scale;
                    enemyConfig.CastbarConfig.CastTimeLabel.Position = enemyConfig.CastbarConfig.CastTimeLabel.Position * scale;
                    enemyConfig.DebuffsConfig.Position = enemyConfig.DebuffsConfig.Position * scale;
                    enemyConfig.DebuffsConfig.Size *= scale;
                    enemyConfig.DebuffsConfig.IconConfig.Size *= scale;
                    enemyConfig.DebuffsConfig.IconPadding *= scale;
                    enemyConfig.DebuffsConfig.IconConfig.DurationLabelConfig.Position = enemyConfig.DebuffsConfig.IconConfig.DurationLabelConfig.Position * scale;
                    enemyConfig.DebuffsConfig.IconConfig.StacksLabelConfig.Position = enemyConfig.DebuffsConfig.IconConfig.StacksLabelConfig.Position * scale;
                    enemyConfig.CastbarConfig.Preview = true;
                    enemyConfig.DebuffsConfig.Preview = true;
                }
                else if (config is NameplateWithPlayerBarConfig playerConfig)
                {
                    originalRoleIconPosition = playerConfig.RoleIconConfig.Position;
                    originalRoleIconSize = playerConfig.RoleIconConfig.Size;
                    originalStateIconPosition = playerConfig.StateIconConfig.Position;
                    originalStateIconSize = playerConfig.StateIconConfig.Size;

                    playerConfig.RoleIconConfig.Position = playerConfig.RoleIconConfig.Position * scale;
                    playerConfig.RoleIconConfig.Size *= scale;
                    playerConfig.StateIconConfig.Position = playerConfig.StateIconConfig.Position * scale;
                    playerConfig.StateIconConfig.Size *= scale;
                }

                ActiveTextScale = scale;
                Nameplate renderer = GetOrCreateRenderer(config);
                NameplateData sample = CreateSampleData(config, canvasPos, canvasSize, 1f);

                if (renderer is NameplateWithBar rendererWithBar)
                {
                    var barActions = rendererWithBar.GetBarDrawActions(sample);
                    foreach ((_, Action action) in barActions)
                    {
                        action();
                    }
                }

                var elementActions = renderer.GetElementsDrawActions(sample);
                foreach ((_, Action action) in elementActions)
                {
                    action();
                }
            }
            finally
            {
                ActiveTextScale = 1f;
                config.Position = originalPosition;
                config.OnlyShowWhenTargeted = originalOnlyShowWhenTargeted;
                config.NameLabelConfig.Position = originalNameLabelPosition;
                config.TitleLabelConfig.Position = originalTitleLabelPosition;
                if (originalBarPosition.HasValue && config is NameplateWithBarConfig withBar)
                {
                    NameplateBarConfig barConfig = withBar.GetBarConfig();
                    barConfig.Position = originalBarPosition.Value;
                    if (originalBarSize.HasValue) { barConfig.Size = originalBarSize.Value; }
                }
                if (hasEnemyCastbar)
                {
                    NameplateWithEnemyBarConfig enemyConfig = (NameplateWithEnemyBarConfig)config;
                    enemyConfig.CastbarConfig.Preview = originalCastbarPreview;
                    enemyConfig.DebuffsConfig.Preview = originalDebuffsPreview;
                    if (originalIconPosition.HasValue) { enemyConfig.IconConfig.Position = originalIconPosition.Value; }
                    if (originalIconSize.HasValue) { enemyConfig.IconConfig.Size = originalIconSize.Value; }
                    if (originalCastbarPosition.HasValue) { enemyConfig.CastbarConfig.Position = originalCastbarPosition.Value; }
                    if (originalCastbarSize.HasValue) { enemyConfig.CastbarConfig.Size = originalCastbarSize.Value; }
                    if (originalCastbarNameLabelPosition.HasValue) { enemyConfig.CastbarConfig.CastNameLabel.Position = originalCastbarNameLabelPosition.Value; }
                    if (originalCastbarTimeLabelPosition.HasValue) { enemyConfig.CastbarConfig.CastTimeLabel.Position = originalCastbarTimeLabelPosition.Value; }
                    if (originalDebuffsPosition.HasValue) { enemyConfig.DebuffsConfig.Position = originalDebuffsPosition.Value; }
                    if (originalDebuffsSize.HasValue) { enemyConfig.DebuffsConfig.Size = originalDebuffsSize.Value; }
                    if (originalDebuffsIconSize.HasValue) { enemyConfig.DebuffsConfig.IconConfig.Size = originalDebuffsIconSize.Value; }
                    if (originalDebuffsIconPadding.HasValue) { enemyConfig.DebuffsConfig.IconPadding = originalDebuffsIconPadding.Value; }
                    if (originalDebuffsDurationLabelPosition.HasValue) { enemyConfig.DebuffsConfig.IconConfig.DurationLabelConfig.Position = originalDebuffsDurationLabelPosition.Value; }
                    if (originalDebuffsStacksLabelPosition.HasValue) { enemyConfig.DebuffsConfig.IconConfig.StacksLabelConfig.Position = originalDebuffsStacksLabelPosition.Value; }
                }
                else if (config is NameplateWithPlayerBarConfig playerConfig)
                {
                    if (originalRoleIconPosition.HasValue) { playerConfig.RoleIconConfig.Position = originalRoleIconPosition.Value; }
                    if (originalRoleIconSize.HasValue) { playerConfig.RoleIconConfig.Size = originalRoleIconSize.Value; }
                    if (originalStateIconPosition.HasValue) { playerConfig.StateIconConfig.Position = originalStateIconPosition.Value; }
                    if (originalStateIconSize.HasValue) { playerConfig.StateIconConfig.Size = originalStateIconSize.Value; }
                }
            }
        }

        private static Nameplate GetOrCreateRenderer(NameplateConfig config)
        {
            if (RendererCache.TryGetValue(config.ID, out Nameplate? renderer))
            {
                return renderer;
            }

            if (config is NameplateWithEnemyBarConfig enemyConfig)
            {
                renderer = new NameplateWithEnemyBar(enemyConfig);
                RendererCache[config.ID] = renderer;
                return renderer;
            }

            if (config is NameplateWithPlayerBarConfig playerConfig)
            {
                renderer = new NameplateWithPlayerBar(playerConfig);
                RendererCache[config.ID] = renderer;
                return renderer;
            }

            if (config is NameplateWithNPCBarConfig npcConfig)
            {
                renderer = new NameplateWithBar(npcConfig);
                RendererCache[config.ID] = renderer;
                return renderer;
            }

            renderer = new Nameplate(config);
            RendererCache[config.ID] = renderer;
            return renderer;
        }

        private static NameplateData CreateSampleData(NameplateConfig config, Vector2 canvasPos, Vector2 canvasSize, float scale)
        {
            IGameObject? actor = ResolvePreviewActor(config);
            Vector2 center = canvasPos + canvasSize / 2f;

            if (config is NameplateWithBarConfig withBar)
            {
                NameplateBarConfig bar = withBar.GetBarConfig();
                Vector2 barSize = bar.Size * scale;
                Vector2 topLeft = center - barSize / 2f;
                Vector2 screenPos = topLeft + barSize / 2f - bar.Position * scale;

                return new NameplateData(
                    actor,
                    SampleNameFor(config),
                    SampleTitleFor(config),
                    false,
                    SampleIconFor(config),
                    "A",
                    ResolveObjectKind(config, actor),
                    actor?.SubKind ?? ResolveSubKind(config),
                    screenPos,
                    Vector3.Zero,
                    12f,
                    true,
                    true
                );
            }

            return new NameplateData(
                actor,
                SampleNameFor(config),
                SampleTitleFor(config),
                false,
                SampleIconFor(config),
                "",
                ResolveObjectKind(config, actor),
                actor?.SubKind ?? ResolveSubKind(config),
                center,
                Vector3.Zero,
                12f,
                true,
                true
            );
        }

        private static float ComputePreviewScale(NameplateConfig config, Vector2 canvasSize)
        {
            float margin = 14f;
            Vector2 estimatedUnscaledSize = EstimateUnscaledBounds(config);

            float widthScale = (canvasSize.X - margin * 2f) / MathF.Max(1f, estimatedUnscaledSize.X);
            float heightScale = (canvasSize.Y - margin * 2f) / MathF.Max(1f, estimatedUnscaledSize.Y);
            float fitScale = MathF.Min(widthScale, heightScale);

            return MathF.Max(0.35f, MathF.Min(1f, fitScale));
        }

        private static Vector2 EstimateUnscaledBounds(NameplateConfig config)
        {
            float minX = -160f;
            float maxX = 160f;
            float minY = -80f;
            float maxY = 80f;

            void includeRect(Vector2 topLeft, Vector2 size)
            {
                minX = MathF.Min(minX, topLeft.X);
                minY = MathF.Min(minY, topLeft.Y);
                maxX = MathF.Max(maxX, topLeft.X + size.X);
                maxY = MathF.Max(maxY, topLeft.Y + size.Y);
            }

            void includeAnchored(Vector2 position, Vector2 size, DrawAnchor anchor)
            {
                includeRect(Utils.GetAnchoredPosition(position, size, anchor), size);
            }

            if (config is NameplateWithBarConfig withBar)
            {
                NameplateBarConfig bar = withBar.GetBarConfig();
                includeAnchored(bar.Position, bar.Size, bar.Anchor);
            }

            if (config.NameLabelConfig.Enabled)
            {
                includeRect(config.NameLabelConfig.Position + new Vector2(-130, -18), new Vector2(260, 36));
            }

            if (config.TitleLabelConfig.Enabled)
            {
                includeRect(config.TitleLabelConfig.Position + new Vector2(-120, -16), new Vector2(240, 32));
            }

            if (config is NameplateWithEnemyBarConfig enemyConfig)
            {
                if (enemyConfig.IconConfig.Enabled)
                {
                    includeAnchored(enemyConfig.IconConfig.Position, enemyConfig.IconConfig.Size, enemyConfig.IconConfig.Anchor);
                }

                if (enemyConfig.CastbarConfig.Enabled)
                {
                    Vector2 castSize = enemyConfig.CastbarConfig.Size;
                    if (enemyConfig.CastbarConfig.MatchWidth && config is NameplateWithBarConfig castParent)
                    {
                        castSize.X = castParent.GetBarConfig().Size.X;
                    }

                    if (enemyConfig.CastbarConfig.MatchHeight && config is NameplateWithBarConfig castParent2)
                    {
                        castSize.Y = castParent2.GetBarConfig().Size.Y;
                    }

                    includeAnchored(enemyConfig.CastbarConfig.Position, castSize, enemyConfig.CastbarConfig.Anchor);
                }

                if (enemyConfig.DebuffsConfig.Enabled)
                {
                    includeAnchored(enemyConfig.DebuffsConfig.Position, enemyConfig.DebuffsConfig.Size, DrawAnchor.TopLeft);
                }
            }

            return new Vector2(MathF.Max(1f, maxX - minX), MathF.Max(1f, maxY - minY));
        }


        private static IGameObject? ResolvePreviewActor(NameplateConfig config)
        {
            IPlayerCharacter? player = Plugin.ObjectTable.LocalPlayer;
            IGameObject? target = Plugin.TargetManager.Target;

            if (config is EnemyNameplateConfig)
            {
                if (target is IBattleNpc battleNpc)
                {
                    BattleNpcSubKind subKind = (BattleNpcSubKind)battleNpc.SubKind;
                    if (subKind == BattleNpcSubKind.Combatant || subKind == BattleNpcSubKind.BNpcPart)
                    {
                        return target;
                    }
                }
            }
            else if (config is PartyMembersNameplateConfig or AllianceMembersNameplateConfig or FriendPlayerNameplateConfig or OtherPlayerNameplateConfig)
            {
                if (target is IPlayerCharacter targetPlayer && player != null && targetPlayer.GameObjectId != player.GameObjectId)
                {
                    return targetPlayer;
                }
            }
            else if (config is PetNameplateConfig or NPCNameplateConfig)
            {
                if (target is IBattleNpc)
                {
                    return target;
                }
            }

            return player ?? target;
        }

        private static ObjectKind ResolveObjectKind(NameplateConfig config, IGameObject? actor)
        {
            if (actor != null)
            {
                return actor.ObjectKind;
            }

            if (config is EnemyNameplateConfig or PetNameplateConfig or NPCNameplateConfig)
            {
                return ObjectKind.BattleNpc;
            }

            if (config is MinionNPCNameplateConfig)
            {
                return ObjectKind.Companion;
            }

            if (config is ObjectsNameplateConfig)
            {
                return ObjectKind.EventObj;
            }

            return ObjectKind.Pc;
        }

        private static byte ResolveSubKind(NameplateConfig config)
        {
            if (config is EnemyNameplateConfig)
            {
                return (byte)BattleNpcSubKind.Combatant;
            }

            if (config is PetNameplateConfig)
            {
                return (byte)BattleNpcSubKind.Pet;
            }

            return 0;
        }

        private static string SampleNameFor(NameplateConfig config)
        {
            if (config is PlayerNameplateConfig) { return "Player"; }
            if (config is EnemyNameplateConfig) { return "Enemy"; }
            if (config is PartyMembersNameplateConfig) { return "Party Member"; }
            if (config is AllianceMembersNameplateConfig) { return "Alliance Member"; }
            if (config is FriendPlayerNameplateConfig) { return "Friend"; }
            if (config is OtherPlayerNameplateConfig) { return "Other Player"; }
            if (config is PetNameplateConfig) { return "Pet"; }
            if (config is NPCNameplateConfig) { return "NPC"; }
            if (config is MinionNPCNameplateConfig) { return "Minion"; }
            if (config is ObjectsNameplateConfig) { return "Object"; }
            return "Nameplate";
        }

        private static string SampleTitleFor(NameplateConfig config)
        {
            if (config is EnemyNameplateConfig) { return ""; }
            if (config is ObjectsNameplateConfig) { return ""; }
            if (config is MinionNPCNameplateConfig) { return ""; }
            return "<The Azure Echo>";
        }

        private static int SampleIconFor(NameplateConfig config)
        {
            if (config is EnemyNameplateConfig)
            {
                return 1;
            }

            return 0;
        }
    }
}

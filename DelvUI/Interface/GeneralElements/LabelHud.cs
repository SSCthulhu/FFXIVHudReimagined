using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using DelvUI.Config;
using DelvUI.Helpers;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using System;
using System.Linq;
using System.Numerics;

namespace DelvUI.Interface.GeneralElements
{
    public class LabelHud : HudElement
    {
        private LabelConfig Config => (LabelConfig)_config;

        public LabelHud(LabelConfig config) : base(config)
        {
        }

        protected override void CreateDrawActions(Vector2 origin)
        {
            // unused
        }

        public override void Draw(Vector2 origin)
        {
            Draw(origin);
        }

        public virtual void Draw(
            Vector2 origin,
            Vector2? parentSize = null,
            IGameObject? actor = null,
            string? actorName = null,
            uint? actorCurrentHp = null,
            uint? actorMaxHp = null,
            bool? isPlayerName = null,
            string? title = null)
        {
            if (!Config.Enabled || Config.GetText() == null)
            {
                return;
            }

            string? text = actor == null && actorName == null && actorCurrentHp == null && actorMaxHp == null && title == null ?
                Config.GetText() :
                TextTagsHelper.FormattedText(Config.GetText(), actor, actorName, actorCurrentHp, actorMaxHp, isPlayerName, title);

            DrawLabel(text, origin, parentSize ?? Vector2.Zero, actor);
        }

        protected virtual void DrawLabel(string text, Vector2 parentPos, Vector2 parentSize, IGameObject? actor = null)
        {
            Vector2 size;
            Vector2 pos;
            float previewScale = NameplatesTabPreviewRenderer.ActiveTextScale;
            float effectiveFontScale = Config.GetFontScale() * previewScale;
            string? effectiveFontId = ResolveEffectiveFontId();

            if (Config.UseSystemFont())
            {
                ImGui.PushFont(UiBuilder.DefaultFont);
                size = ImGui.CalcTextSize(text) * effectiveFontScale;
                pos = Utils.GetAnchoredPosition(Utils.GetAnchoredPosition(parentPos + Config.Position, -parentSize, Config.FrameAnchor), size, Config.TextAnchor);
                ImGui.PopFont();
            }
            else
            {
                using (FontsManager.Instance.PushFont(effectiveFontId))
                {
                    size = ImGui.CalcTextSize(text) * effectiveFontScale;
                    pos = Utils.GetAnchoredPosition(Utils.GetAnchoredPosition(parentPos + Config.Position, -parentSize, Config.FrameAnchor), size, Config.TextAnchor);
                }
            }

            DrawLabel(text, pos, size, Color(actor));
        }

        public void DrawLabel(string text, Vector2 pos, Vector2 size, PluginConfigColor color, float? alpha = null)
        {
            if (!Config.Enabled) { return; }
            float previewScale = NameplatesTabPreviewRenderer.ActiveTextScale;
            float effectiveFontScale = Config.GetFontScale() * previewScale;
            string? effectiveFontId = ResolveEffectiveFontId();

            PluginConfigColor fillColor = color;
            PluginConfigColor shadowColor = Config.ShadowConfig.Color;
            PluginConfigColor outlineColor = Config.GetOutlineColor();

            if (alpha.HasValue)
            {
                fillColor = fillColor.WithAlpha(alpha.Value);
                shadowColor = shadowColor.WithAlpha(alpha.Value);
                outlineColor = outlineColor.WithAlpha(alpha.Value);
            }

            Action<ImDrawListPtr> action = (ImDrawListPtr drawList) =>
            {
                if (Config.ShadowConfig.Enabled)
                {
                    DrawHelper.DrawShadowText(text, pos, fillColor.Base, shadowColor.Base, drawList, Config.ShadowConfig.Offset, Config.ShadowConfig.Thickness);
                }

                if (Config.ShowOutline)
                {
                    DrawHelper.DrawOutlinedText(text, pos, fillColor.Base, outlineColor.Base, drawList);
                }

                if (!Config.ShowOutline && !Config.ShadowConfig.Enabled)
                {
                    drawList.AddText(pos, fillColor.Base, text);
                }
            };

            DrawHelper.DrawInWindow(ID, pos, size, false, (drawList) =>
            {
                if (Config.UseSystemFont())
                {
                    ImGui.SetWindowFontScale(effectiveFontScale);
                    ImGui.PushFont(UiBuilder.DefaultFont);
                    action(drawList);
                    ImGui.PopFont();
                    ImGui.SetWindowFontScale(1);
                }
                else
                {
                    using (FontsManager.Instance.PushFont(effectiveFontId))
                    {
                        ImGui.SetWindowFontScale(effectiveFontScale);
                        action(drawList);
                        ImGui.SetWindowFontScale(1);
                    }
                }
            });
        }

        public virtual PluginConfigColor Color(IGameObject? actor = null)
        {
            switch (Config.UseJobColor)
            {
                case true when (actor is ICharacter || actor is IBattleNpc battleNpc && battleNpc.ClassJob.RowId > 0):
                    return ColorUtils.ColorForActor(actor);
                case true when actor is not ICharacter:
                    return GlobalColors.Instance.NPCFriendlyColor;
            }

            switch (Config.UseRoleColor)
            {
                case true when (actor is ICharacter || actor is IBattleNpc battleNpc && battleNpc.ClassJob.RowId > 0):
                    {
                        ICharacter? character = actor as ICharacter;
                        return character != null && character.ClassJob.RowId > 0 ?
                            GlobalColors.Instance.SafeRoleColorForJobId(character.ClassJob.RowId) :
                            ColorUtils.ColorForActor(character);
                    }
                case true when actor is not ICharacter:
                    return GlobalColors.Instance.NPCFriendlyColor;
                default:
                    return Config.GetColor();
            }
        }

        public virtual (string, Vector2, Vector2, PluginConfigColor) PreCalculate(
            Vector2 origin,
            Vector2? parentSize = null,
            IGameObject? actor = null,
            string? actorName = null,
            uint? actorCurrentHp = null,
            uint? actorMaxHp = null,
            bool? isPlayerName = null,
            string? title = null)
        {
            if (!Config.Enabled || Config.GetText() == null)
            {
                return ("", Vector2.Zero, Vector2.Zero, Color(null));
            }

            string? text = actor == null && actorName == null && actorCurrentHp == null && actorMaxHp == null && title == null ?
                Config.GetText() :
                TextTagsHelper.FormattedText(Config.GetText(), actor, actorName, actorCurrentHp, actorMaxHp, isPlayerName, title);

            Vector2 pSize = parentSize ?? Vector2.Zero;
            Vector2 size;
            Vector2 pos;
            float previewTextScale = NameplatesTabPreviewRenderer.ActiveTextScale;
            float effectiveFontScale = Config.GetFontScale() * previewTextScale;
            string? effectiveFontId = ResolveEffectiveFontId();

            if (Config.UseSystemFont())
            {
                ImGui.PushFont(UiBuilder.DefaultFont);
                size = ImGui.CalcTextSize(text) * effectiveFontScale;
                pos = Utils.GetAnchoredPosition(Utils.GetAnchoredPosition(origin + Config.Position, -pSize, Config.FrameAnchor), size, Config.TextAnchor);
                ImGui.PopFont();
            }
            else
            {
                using (FontsManager.Instance.PushFont(effectiveFontId))
                {
                    size = ImGui.CalcTextSize(text) * effectiveFontScale;
                    pos = Utils.GetAnchoredPosition(Utils.GetAnchoredPosition(origin + Config.Position, -pSize, Config.FrameAnchor), size, Config.TextAnchor);
                }
            }

            return (text, pos, size, Color(actor));
        }

        private string? ResolveEffectiveFontId()
        {
            if (Config.FontID != null)
            {
                return Config.FontID;
            }

            FontsConfig fontsConfig = ConfigurationManager.Instance.GetConfigObject<FontsConfig>();
            string? configuredGlobal = Config is NumericLabelConfig ? fontsConfig.GlobalNumericFontKey : fontsConfig.GlobalFontKey;
            if (string.IsNullOrEmpty(configuredGlobal))
            {
                return null;
            }

            if (fontsConfig.Fonts.ContainsKey(configuredGlobal))
            {
                return configuredGlobal;
            }

            // Global selectors store style names; resolve to a concrete key by best size match.
            var matchingFonts = fontsConfig.Fonts
                .Where(entry => entry.Value.Name == configuredGlobal)
                .ToArray();

            if (matchingFonts.Length == 0)
            {
                return null;
            }

            int targetSize = 20;
            if (Config.FontID != null && fontsConfig.Fonts.TryGetValue(Config.FontID, out FontData currentData))
            {
                targetSize = currentData.Size;
            }
            else if (fontsConfig.Fonts.TryGetValue(FontsConfig.DefaultMediumFontKey, out FontData defaultData))
            {
                targetSize = defaultData.Size;
            }

            var bestMatch = matchingFonts
                .OrderBy(entry => Math.Abs(entry.Value.Size - targetSize))
                .ThenBy(entry => entry.Value.Size)
                .First();

            return bestMatch.Key;
        }
    }

    public class IconLabelHud : LabelHud
    {
        private IconLabelConfig Config => (IconLabelConfig)_config;

        public IconLabelHud(IconLabelConfig config) : base(config)
        {
        }

        public override void Draw(Vector2 origin,
            Vector2? parentSize = null,
            IGameObject? actor = null,
            string? actorName = null,
            uint? actorCurrentHp = null,
            uint? actorMaxHp = null,
            bool? isPlayerName = null,
            string? title = null)
        {
            string? text = Config.GetText();
            if (!Config.Enabled || text == null)
            {
                return;
            }

            DrawLabel(text, origin, parentSize ?? Vector2.Zero, actor);
        }

        protected override void DrawLabel(string text, Vector2 parentPos, Vector2 parentSize, IGameObject? actor = null)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            float previewScale = NameplatesTabPreviewRenderer.ActiveTextScale;
            float effectiveFontScale = Config.GetFontScale() * previewScale;
            Vector2 size = ImGui.CalcTextSize(text) * effectiveFontScale;
            Vector2 pos = Utils.GetAnchoredPosition(Utils.GetAnchoredPosition(parentPos + Config.Position, -parentSize, Config.FrameAnchor), size, Config.TextAnchor);
            ImGui.PopFont();

            DrawHelper.DrawInWindow(ID, pos, size, false, (drawList) =>
            {
                ImGui.SetWindowFontScale(effectiveFontScale);
                ImGui.PushFont(UiBuilder.IconFont);

                PluginConfigColor? color = Color(actor);

                if (Config.ShadowConfig.Enabled)
                {
                    DrawHelper.DrawShadowText(text, pos, color.Base, Config.ShadowConfig.Color.Base, drawList, Config.ShadowConfig.Offset, Config.ShadowConfig.Thickness);
                }

                if (Config.ShowOutline)
                {
                    DrawHelper.DrawOutlinedText(text, pos, color.Base, Config.OutlineColor.Base, drawList);
                }

                if (!Config.ShowOutline && !Config.ShadowConfig.Enabled)
                {
                    drawList.AddText(pos, color.Base, text);
                }

                ImGui.PopFont();
                ImGui.SetWindowFontScale(1);
            });
        }
    }
}
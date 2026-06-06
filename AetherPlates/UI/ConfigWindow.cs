using Dalamud.Bindings.ImGui;
using FFXIVHudPlugin.AetherPlates.Configuration;
using FFXIVHudPlugin.AetherPlates.Layout;
using FFXIVHudPlugin.AetherPlates.Styles;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.UI;

public sealed class ConfigWindow
{
    private readonly PluginConfiguration config;
    private readonly Action onConfigChanged;
    private string buffWhitelistInput = string.Empty;
    private string buffBlacklistInput = string.Empty;
    private string debuffWhitelistInput = string.Empty;
    private string debuffBlacklistInput = string.Empty;
    private GroupTab selectedGroupTab = GroupTab.Own;

    public ConfigWindow(
        PluginConfiguration config,
        Action onConfigChanged)
    {
        this.config = config;
        this.onConfigChanged = onConfigChanged;
    }

    public void DrawSection()
    {
        ImGui.TextUnformatted("Nameplates");
        ImGui.Separator();
        ImGui.Spacing();

        var enabled = this.config.Enabled;
        if (ImGui.Checkbox("Enable AetherPlates Nameplates", ref enabled))
        {
            this.config.Enabled = enabled;
            this.onConfigChanged();
        }

        var verticalOffset = this.config.VerticalOffset;
        if (ImGui.DragFloat("Vertical Offset", ref verticalOffset, 0.01f, -5f, 8f, "%.2f"))
        {
            this.config.VerticalOffset = verticalOffset;
            this.onConfigChanged();
        }

        var temporaryScale = this.config.TemporaryGlobalScale;
        if (ImGui.DragFloat("Temporary Global Nameplate Scale", ref temporaryScale, 0.01f, 0.5f, 3.0f, "%.2f"))
        {
            this.config.TemporaryGlobalScale = temporaryScale;
            this.onConfigChanged();
        }

        this.DrawAdvancedCategoryMapping();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Range Filtering");

        var enableDistanceCulling = this.config.EnableDistanceCulling;
        if (ImGui.Checkbox("Enable Distance Culling", ref enableDistanceCulling))
        {
            this.config.EnableDistanceCulling = enableDistanceCulling;
            this.onConfigChanged();
        }

        if (this.config.EnableDistanceCulling)
        {
            var enemyRange = this.config.EnemyMaxDistanceYalms;
            if (ImGui.DragFloat("Enemy Range (yalms)", ref enemyRange, 0.5f, 5f, 120f, "%.1f"))
            {
                this.config.EnemyMaxDistanceYalms = enemyRange;
                this.onConfigChanged();
            }

            var friendlyRange = this.config.FriendlyMaxDistanceYalms;
            if (ImGui.DragFloat("Friendly Range (yalms)", ref friendlyRange, 0.5f, 5f, 120f, "%.1f"))
            {
                this.config.FriendlyMaxDistanceYalms = friendlyRange;
                this.onConfigChanged();
            }

            var playerRange = this.config.PlayerMaxDistanceYalms;
            if (ImGui.DragFloat("Player Range (yalms)", ref playerRange, 0.5f, 5f, 200f, "%.1f"))
            {
                this.config.PlayerMaxDistanceYalms = playerRange;
                this.onConfigChanged();
            }

            var dynamicCombat = this.config.EnableDynamicCombatRange;
            if (ImGui.Checkbox("Enable Dynamic Combat Range", ref dynamicCombat))
            {
                this.config.EnableDynamicCombatRange = dynamicCombat;
                this.onConfigChanged();
            }

            if (this.config.EnableDynamicCombatRange)
            {
                var combatEnemyRange = this.config.CombatEnemyMaxDistanceYalms;
                if (ImGui.DragFloat("Combat Enemy Range (yalms)", ref combatEnemyRange, 0.5f, 5f, 160f, "%.1f"))
                {
                    this.config.CombatEnemyMaxDistanceYalms = combatEnemyRange;
                    this.onConfigChanged();
                }

                var combatFriendlyRange = this.config.CombatFriendlyMaxDistanceYalms;
                if (ImGui.DragFloat("Combat Friendly Range (yalms)", ref combatFriendlyRange, 0.5f, 5f, 160f, "%.1f"))
                {
                    this.config.CombatFriendlyMaxDistanceYalms = combatFriendlyRange;
                    this.onConfigChanged();
                }
            }

            ImGui.TextColored(
                0xFF9AA1AB,
                "Recommended open-world defaults: Enemy 30-40, Friendly 20-30. Dynamic mode raises ranges during active combat contexts.");
        }

        ImGui.Spacing();
        ImGui.TextColored(0xFF9AA1AB, "Per-category widget visibility and layout are configured in Advanced Category tabs above.");
    }

    private static void DrawIdSetEditor(string label, ref string input, HashSet<uint> target, Action onChanged)
    {
        ImGui.TextUnformatted(label);
        ImGui.SetNextItemWidth(180f);
        ImGui.InputText($"##{label}_input", ref input, 32);
        ImGui.SameLine();
        if (ImGui.Button($"Add##{label}") && uint.TryParse(input.Trim(), out var parsed))
        {
            if (target.Add(parsed))
            {
                onChanged();
            }

            input = string.Empty;
        }

        if (target.Count == 0)
        {
            ImGui.TextColored(0xFF9AA1AB, "None");
            return;
        }

        var ordered = target.OrderBy(x => x).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var id = ordered[i];
            if (ImGui.SmallButton($"x##{label}_remove_{id}"))
            {
                target.Remove(id);
                onChanged();
                break;
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(id.ToString());
            if (i < ordered.Length - 1)
            {
                ImGui.SameLine();
            }
        }
    }

    private void DrawAdvancedCategoryMapping()
    {
        var v = this.config.CategoryVisibility;
        ImGui.TextUnformatted("Advanced Category Mapping (Native 1:1)");
        ImGui.TextColored(0xFF9AA1AB, "These match Character Config > Display Name Settings categories.");
        ImGui.Spacing();

        DrawGroupTabs();
        ImGui.Spacing();

        switch (this.selectedGroupTab)
        {
            case GroupTab.Own:
                DrawCategorySection("Own (Self)", v.Self, value => v.Self = value, this.config.SelfVisual);
                DrawCategorySection("Companions (Own)", v.SelfCompanion, value => v.SelfCompanion = value, this.config.SelfCompanionVisual);
                DrawCategorySection("Pets (Own)", v.SelfPet, value => v.SelfPet = value, this.config.SelfPetVisual);
                break;
            case GroupTab.Others:
                DrawCategorySection("Party Members", v.PartyMember, value => v.PartyMember = value, this.config.PartyVisual);
                DrawCategorySection("Party Companions", v.PartyCompanion, value => v.PartyCompanion = value, this.config.PartyCompanionVisual);
                DrawCategorySection("Party Pets", v.PartyPet, value => v.PartyPet = value, this.config.PartyPetVisual);
                DrawCategorySection("Alliance Members", v.AllianceMember, value => v.AllianceMember = value, this.config.AllianceVisual);
                DrawCategorySection("Alliance Pets", v.AlliancePet, value => v.AlliancePet = value, this.config.AlliancePetVisual);
                DrawCategorySection("Friends", v.Friend, value => v.Friend = value, this.config.FriendVisual);
                DrawCategorySection("Friend Companions", v.FriendCompanion, value => v.FriendCompanion = value, this.config.FriendCompanionVisual);
                DrawCategorySection("Friend Pets", v.FriendPet, value => v.FriendPet = value, this.config.FriendPetVisual);
                DrawCategorySection("Other PCs", v.OtherPc, value => v.OtherPc = value, this.config.OtherPcVisual);
                DrawCategorySection("Other Companions", v.OtherCompanion, value => v.OtherCompanion = value, this.config.OtherCompanionVisual);
                DrawCategorySection("Other Pets", v.OtherPet, value => v.OtherPet = value, this.config.OtherPetVisual);
                break;
            case GroupTab.Npcs:
                DrawCategorySection("Unengaged Enemies", v.EnemyUnengaged, value => v.EnemyUnengaged = value, this.config.EnemyUnengagedVisual);
                DrawCategorySection("Engaged Enemies", v.EnemyEngaged, value => v.EnemyEngaged = value, this.config.EnemyEngagedVisual);
                DrawCategorySection("Claimed Enemies", v.EnemyClaimed, value => v.EnemyClaimed = value, this.config.EnemyClaimedVisual);
                DrawCategorySection("Unclaimed Enemies", v.EnemyUnclaimed, value => v.EnemyUnclaimed = value, this.config.EnemyUnclaimedVisual);
                DrawCategorySection("Feast Enemies", v.EnemyFeast, value => v.EnemyFeast = value, this.config.EnemyFeastVisual);
                DrawCategorySection("Feast Enemy Pets", v.EnemyFeastPet, value => v.EnemyFeastPet = value, this.config.EnemyFeastPetVisual);
                DrawCategorySection("NPCs", v.Npc, value => v.Npc = value, this.config.NpcVisual);
                DrawCategorySection("Objects", v.Object, value => v.Object = value, this.config.ObjectVisual);
                DrawCategorySection("Minions", v.Minion, value => v.Minion = value, this.config.MinionVisual);
                DrawCategorySection("Housing Furniture", v.HousingFurniture, value => v.HousingFurniture = value, this.config.HousingFurnitureVisual);
                DrawCategorySection("Housing Gardens", v.HousingField, value => v.HousingField = value, this.config.HousingFieldVisual);
                break;
        }
    }

    private void DrawCategoryToggle(string label, bool value, Action<bool> onSet)
    {
        var localValue = value;
        var changed = ImGui.Checkbox(label, ref localValue);
        if (changed)
        {
            onSet(localValue);
            this.onConfigChanged();
        }
    }

    private void DrawGroupTabs()
    {
        DrawGroupTabButton(GroupTab.Own, "Own");
        ImGui.SameLine();
        DrawGroupTabButton(GroupTab.Others, "Others");
        ImGui.SameLine();
        DrawGroupTabButton(GroupTab.Npcs, "NPCs");
    }

    private void DrawGroupTabButton(GroupTab tab, string label)
    {
        var selected = this.selectedGroupTab == tab;
        if (selected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF6B4A24);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF866034);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF9A6F3B);
        }

        if (ImGui.Button(label))
        {
            this.selectedGroupTab = tab;
        }

        if (selected)
        {
            ImGui.PopStyleColor(3);
        }
    }

    private void DrawCategorySection(
        string title,
        bool categoryEnabled,
        Action<bool> setCategoryEnabled,
        CategoryVisualSettings visuals)
    {
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted(title);
        var categoryId = title.Replace(" ", "_", StringComparison.Ordinal).Replace("(", string.Empty, StringComparison.Ordinal).Replace(")", string.Empty, StringComparison.Ordinal);
        DrawCategoryToggle($"Enable Category##{categoryId}_enabled", categoryEnabled, setCategoryEnabled);

        DrawCategoryWidgetEditor(visuals, categoryId, "health_bar", "Health Bar");
        DrawCategoryWidgetEditor(visuals, categoryId, "name_text", "Name Text");
        DrawCategoryWidgetEditor(visuals, categoryId, "target_indicator", "Target Indicator");
        DrawCategoryWidgetEditor(visuals, categoryId, "cast_bar", "Cast Bar");
        DrawCategoryWidgetEditor(visuals, categoryId, "buff_row", "Buff Row");
        DrawCategoryWidgetEditor(visuals, categoryId, "debuff_row", "Debuff Row");
    }

    private void DrawCategoryWidgetEditor(CategoryVisualSettings visuals, string categoryId, string widgetId, string label)
    {
        var enabled = visuals.IsWidgetEnabled(widgetId);
        if (ImGui.Checkbox($"{label}##{categoryId}_{widgetId}_enabled", ref enabled))
        {
            visuals.SetWidgetEnabled(widgetId, enabled);
            this.onConfigChanged();
        }

        if (!visuals.WidgetLayouts.TryGetValue(widgetId, out var rule))
        {
            visuals.EnsureDefaults();
            rule = visuals.WidgetLayouts[widgetId];
        }

        if (!enabled)
        {
            return;
        }

        ImGui.Indent();
        var offset = rule.Offset;
        if (ImGui.DragFloat2($"Offset (X/Y)##{categoryId}_{widgetId}_offset", ref offset, 0.5f, -600f, 600f, "%.1f"))
        {
            rule.Offset = offset;
            this.onConfigChanged();
        }

        var size = rule.Size;
        if (ImGui.DragFloat2($"Size (W/H)##{categoryId}_{widgetId}_size", ref size, 0.5f, 0f, 600f, "%.1f"))
        {
            rule.Size = new Vector2(Math.Max(0f, size.X), Math.Max(0f, size.Y));
            this.onConfigChanged();
        }

        ImGui.Unindent();
    }

    private enum GroupTab
    {
        Own = 0,
        Others = 1,
        Npcs = 2,
    }
}

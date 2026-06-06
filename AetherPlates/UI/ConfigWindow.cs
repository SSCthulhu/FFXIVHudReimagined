using Dalamud.Bindings.ImGui;
using FFXIVHudPlugin.AetherPlates.Configuration;
using FFXIVHudPlugin.AetherPlates.Core;
using FFXIVHudPlugin.AetherPlates.Layout;
using Dalamud.Plugin.Services;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.UI;

public sealed class ConfigWindow
{
    private readonly PluginConfiguration config;
    private readonly Action onConfigChanged;
    private readonly LayoutEditorWindow layoutEditorWindow;
    private string buffWhitelistInput = string.Empty;
    private string buffBlacklistInput = string.Empty;
    private string debuffWhitelistInput = string.Empty;
    private string debuffBlacklistInput = string.Empty;
    private GroupTab selectedGroupTab = GroupTab.Own;

    public ConfigWindow(
        PluginConfiguration config,
        Action onConfigChanged,
        ITextureProvider textureProvider)
    {
        this.config = config;
        this.onConfigChanged = onConfigChanged;
        this.layoutEditorWindow = new LayoutEditorWindow(config, onConfigChanged, textureProvider);
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
        this.layoutEditorWindow.Draw();

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
        ImGui.TextUnformatted("Category Designer");
        ImGui.TextColored(0xFF9AA1AB, "Select a category to open its dedicated visual editor.");
        ImGui.Spacing();

        DrawGroupTabs();
        ImGui.Spacing();

        switch (this.selectedGroupTab)
        {
            case GroupTab.Own:
                DrawCategorySection(
                    "Own (Self)",
                    "own_self",
                    NameplateManager.NameplateCategory.Self,
                    () => v.Self,
                    value => v.Self = value,
                    this.config.SelfVisual);
                DrawCategorySection(
                    "Companions (Own)",
                    "own_companion",
                    NameplateManager.NameplateCategory.SelfCompanion,
                    () => v.SelfCompanion,
                    value => v.SelfCompanion = value,
                    this.config.SelfCompanionVisual);
                DrawCategorySection(
                    "Pets (Own)",
                    "own_pet",
                    NameplateManager.NameplateCategory.SelfPet,
                    () => v.SelfPet,
                    value => v.SelfPet = value,
                    this.config.SelfPetVisual);
                break;
            case GroupTab.Others:
                DrawCategorySection("Party Members", "party_member", NameplateManager.NameplateCategory.Party, () => v.PartyMember, value => v.PartyMember = value, this.config.PartyVisual);
                DrawCategorySection("Party Companions", "party_companion", NameplateManager.NameplateCategory.PartyCompanion, () => v.PartyCompanion, value => v.PartyCompanion = value, this.config.PartyCompanionVisual);
                DrawCategorySection("Party Pets", "party_pet", NameplateManager.NameplateCategory.PartyPet, () => v.PartyPet, value => v.PartyPet = value, this.config.PartyPetVisual);
                DrawCategorySection("Alliance Members", "alliance_member", NameplateManager.NameplateCategory.Alliance, () => v.AllianceMember, value => v.AllianceMember = value, this.config.AllianceVisual);
                DrawCategorySection("Alliance Pets", "alliance_pet", NameplateManager.NameplateCategory.AlliancePet, () => v.AlliancePet, value => v.AlliancePet = value, this.config.AlliancePetVisual);
                DrawCategorySection("Friends", "friend", NameplateManager.NameplateCategory.Friend, () => v.Friend, value => v.Friend = value, this.config.FriendVisual);
                DrawCategorySection("Friend Companions", "friend_companion", NameplateManager.NameplateCategory.FriendCompanion, () => v.FriendCompanion, value => v.FriendCompanion = value, this.config.FriendCompanionVisual);
                DrawCategorySection("Friend Pets", "friend_pet", NameplateManager.NameplateCategory.FriendPet, () => v.FriendPet, value => v.FriendPet = value, this.config.FriendPetVisual);
                DrawCategorySection("Other PCs", "other_pc", NameplateManager.NameplateCategory.OtherPc, () => v.OtherPc, value => v.OtherPc = value, this.config.OtherPcVisual);
                DrawCategorySection("Other Companions", "other_companion", NameplateManager.NameplateCategory.OtherCompanion, () => v.OtherCompanion, value => v.OtherCompanion = value, this.config.OtherCompanionVisual);
                DrawCategorySection("Other Pets", "other_pet", NameplateManager.NameplateCategory.OtherPet, () => v.OtherPet, value => v.OtherPet = value, this.config.OtherPetVisual);
                break;
            case GroupTab.Npcs:
                DrawCategorySection("Unengaged Enemies", "enemy_unengaged", NameplateManager.NameplateCategory.EnemyUnengaged, () => v.EnemyUnengaged, value => v.EnemyUnengaged = value, this.config.EnemyUnengagedVisual);
                DrawCategorySection("Engaged Enemies", "enemy_engaged", NameplateManager.NameplateCategory.EnemyEngaged, () => v.EnemyEngaged, value => v.EnemyEngaged = value, this.config.EnemyEngagedVisual);
                DrawCategorySection("Claimed Enemies", "enemy_claimed", NameplateManager.NameplateCategory.EnemyClaimed, () => v.EnemyClaimed, value => v.EnemyClaimed = value, this.config.EnemyClaimedVisual);
                DrawCategorySection("Unclaimed Enemies", "enemy_unclaimed", NameplateManager.NameplateCategory.EnemyUnclaimed, () => v.EnemyUnclaimed, value => v.EnemyUnclaimed = value, this.config.EnemyUnclaimedVisual);
                DrawCategorySection("Feast Enemies", "enemy_feast", NameplateManager.NameplateCategory.EnemyFeast, () => v.EnemyFeast, value => v.EnemyFeast = value, this.config.EnemyFeastVisual);
                DrawCategorySection("Feast Enemy Pets", "enemy_feast_pet", NameplateManager.NameplateCategory.EnemyFeastPet, () => v.EnemyFeastPet, value => v.EnemyFeastPet = value, this.config.EnemyFeastPetVisual);
                DrawCategorySection("NPCs", "npc", NameplateManager.NameplateCategory.Npc, () => v.Npc, value => v.Npc = value, this.config.NpcVisual);
                DrawCategorySection("Objects", "object", NameplateManager.NameplateCategory.Object, () => v.Object, value => v.Object = value, this.config.ObjectVisual);
                DrawCategorySection("Minions", "minion", NameplateManager.NameplateCategory.Minion, () => v.Minion, value => v.Minion = value, this.config.MinionVisual);
                DrawCategorySection("Housing Furniture", "housing_furniture", NameplateManager.NameplateCategory.HousingFurniture, () => v.HousingFurniture, value => v.HousingFurniture = value, this.config.HousingFurnitureVisual);
                DrawCategorySection("Housing Gardens", "housing_field", NameplateManager.NameplateCategory.HousingField, () => v.HousingField, value => v.HousingField = value, this.config.HousingFieldVisual);
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
        string categoryId,
        NameplateManager.NameplateCategory category,
        Func<bool> getCategoryEnabled,
        Action<bool> setCategoryEnabled,
        CategoryVisualSettings visuals)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var enabled = getCategoryEnabled();
        var buttonLabel = $"{title} {(enabled ? "(Enabled)" : "(Disabled)")}";
        if (ImGui.Button(buttonLabel, new Vector2(-1f, 28f)))
        {
            this.layoutEditorWindow.Open(new LayoutEditorWindow.CategoryEditorTarget(
                categoryId,
                title,
                category,
                getCategoryEnabled,
                setCategoryEnabled,
                visuals));
        }
    }

    private enum GroupTab
    {
        Own = 0,
        Others = 1,
        Npcs = 2,
    }
}

using Dalamud.Bindings.ImGui;
using FFXIVHudPlugin.AetherPlates.Configuration;
using FFXIVHudPlugin.AetherPlates.Core;
using FFXIVHudPlugin.AetherPlates.Layout;
using Dalamud.Plugin.Services;
using System.Text.Json;
using System.Numerics;

namespace FFXIVHudPlugin.AetherPlates.UI;

public sealed class ConfigWindow
{
    private PluginConfiguration config;
    private readonly Action onConfigChanged;
    private readonly LayoutEditorWindow layoutEditorWindow;
    private string buffWhitelistInput = string.Empty;
    private string buffBlacklistInput = string.Empty;
    private string debuffWhitelistInput = string.Empty;
    private string debuffBlacklistInput = string.Empty;
    private string profileNameInput = string.Empty;
    private GroupTab selectedGroupTab = GroupTab.Own;
    private GeneralSessionBaseline generalBaseline;
    private CategoryVisualSettings? copiedCategoryVisualSettings;
    private string copiedCategoryVisualSourceLabel = string.Empty;

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
        this.DrawGeneralSettingsSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        this.DrawCategoryDesignerSection();
    }

    public void UpdateConfiguration(PluginConfiguration configuration)
    {
        this.config = configuration;
        this.layoutEditorWindow.UpdateConfiguration(configuration);
        this.CaptureSessionBaselines();
    }

    public void DrawGeneralSettingsSection()
    {
        ImGui.TextUnformatted("Nameplate General Settings");
        if (ImGui.Button("Reset to Opened Values##NameplateGeneral"))
        {
            this.ResetGeneralSettings();
            this.onConfigChanged();
        }
        ImGui.SameLine();
        ImGui.TextColored(0xFF9AA1AB, "Changes save automatically.");
        ImGui.Separator();
        ImGui.Spacing();
        this.DrawSavedProfilesSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var enabled = this.config.Enabled;
        if (ImGui.Checkbox("Enable Nameplates", ref enabled))
        {
            this.config.Enabled = enabled;
            this.onConfigChanged();
        }

        var temporaryScale = this.config.TemporaryGlobalScale;
        if (ImGui.DragFloat("Global Nameplate Scale", ref temporaryScale, 0.01f, 0.5f, 3.0f, "%.2f"))
        {
            this.config.TemporaryGlobalScale = temporaryScale;
            this.onConfigChanged();
        }

        ImGui.Spacing();
        DrawGlobalDefaultFontSelector();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Distance Filtering");

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
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Occlusion");

        var enableOcclusionCulling = this.config.EnableOcclusionCulling;
        if (ImGui.Checkbox("Hide Nameplates Behind Geometry", ref enableOcclusionCulling))
        {
            this.config.EnableOcclusionCulling = enableOcclusionCulling;
            this.onConfigChanged();
        }

        if (this.config.EnableOcclusionCulling)
        {
            var modeIndex = this.config.OcclusionMode switch
            {
                NameplateOcclusionMode.Simple => 1,
                NameplateOcclusionMode.Full => 2,
                _ => 0,
            };
            var modeLabels = new[] { "Disabled", "Simple", "Full" };
            if (ImGui.Combo("Occlusion Mode", ref modeIndex, modeLabels, modeLabels.Length))
            {
                this.config.OcclusionMode = modeIndex switch
                {
                    2 => NameplateOcclusionMode.Full,
                    1 => NameplateOcclusionMode.Simple,
                    _ => NameplateOcclusionMode.None,
                };
                this.onConfigChanged();
            }

            var typeIndex = this.config.OcclusionType == NameplateOcclusionType.WallsAndObjects ? 1 : 0;
            var typeLabels = new[] { "Walls", "Walls and Objects" };
            if (ImGui.Combo("Occlusion Type", ref typeIndex, typeLabels, typeLabels.Length))
            {
                this.config.OcclusionType = typeIndex == 1
                    ? NameplateOcclusionType.WallsAndObjects
                    : NameplateOcclusionType.Walls;
                this.onConfigChanged();
            }

            ImGui.TextColored(
                0xFF9AA1AB,
                "Simple uses one LOS ray. Full samples center/left/right rays for fewer false occlusions.");
        }

        ImGui.Spacing();
        ImGui.TextColored(0xFF9AA1AB, "Per-category widget visibility and layout are configured in Category Designer.");
    }

    private void DrawSavedProfilesSection()
    {
        this.config.SavedProfiles ??= new List<NameplateSavedProfile>();
        this.config.SelectedSavedProfileId ??= string.Empty;
        this.profileNameInput ??= string.Empty;

        ImGui.TextUnformatted("Saved Nameplate Profiles");
        ImGui.TextColored(0xFF9AA1AB, "Save and load full Nameplate setups (general, visibility, and category visuals).");
        ImGui.Spacing();

        if (this.config.SavedProfiles.Count == 0)
        {
            ImGui.TextColored(0xFF9AA1AB, "No saved profiles yet.");
        }
        else
        {
            var selectedIndex = 0;
            var labels = new string[this.config.SavedProfiles.Count];
            for (var i = 0; i < this.config.SavedProfiles.Count; i++)
            {
                labels[i] = this.config.SavedProfiles[i].DisplayName;
                if (string.Equals(this.config.SavedProfiles[i].Id, this.config.SelectedSavedProfileId, StringComparison.Ordinal))
                {
                    selectedIndex = i;
                }
            }

            if (ImGui.Combo("Saved Profile", ref selectedIndex, labels, labels.Length))
            {
                this.config.SelectedSavedProfileId = this.config.SavedProfiles[selectedIndex].Id;
                this.profileNameInput = this.config.SavedProfiles[selectedIndex].DisplayName;
                this.onConfigChanged();
            }

            if (string.IsNullOrWhiteSpace(this.config.SelectedSavedProfileId))
            {
                this.config.SelectedSavedProfileId = this.config.SavedProfiles[0].Id;
                this.profileNameInput = this.config.SavedProfiles[0].DisplayName;
            }
        }

        ImGui.InputText("Profile Name", ref this.profileNameInput, 80);
        if (ImGui.Button("Save as New"))
        {
            this.SaveCurrentAsNewProfile();
        }

        ImGui.SameLine();
        var hasSelection = this.TryGetSelectedSavedProfileIndex(out var selectedProfileIndex);
        if (!hasSelection)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Update Selected"))
        {
            this.UpdateSelectedSavedProfile(selectedProfileIndex);
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Selected"))
        {
            this.LoadSelectedSavedProfile(selectedProfileIndex);
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete Selected"))
        {
            this.DeleteSelectedSavedProfile(selectedProfileIndex);
        }

        if (!hasSelection)
        {
            ImGui.EndDisabled();
        }
    }

    private void SaveCurrentAsNewProfile()
    {
        var name = string.IsNullOrWhiteSpace(this.profileNameInput)
            ? $"Profile {this.config.SavedProfiles.Count + 1}"
            : this.profileNameInput.Trim();
        var snapshot = this.CloneNameplateConfiguration(this.config);
        var saved = new NameplateSavedProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            DisplayName = name,
            Snapshot = snapshot,
        };
        this.config.SavedProfiles.Add(saved);
        this.config.SelectedSavedProfileId = saved.Id;
        this.profileNameInput = saved.DisplayName;
        this.onConfigChanged();
    }

    private void UpdateSelectedSavedProfile(int selectedProfileIndex)
    {
        if (selectedProfileIndex < 0 || selectedProfileIndex >= this.config.SavedProfiles.Count)
        {
            return;
        }

        var selected = this.config.SavedProfiles[selectedProfileIndex];
        selected.DisplayName = string.IsNullOrWhiteSpace(this.profileNameInput) ? selected.DisplayName : this.profileNameInput.Trim();
        selected.Snapshot = this.CloneNameplateConfiguration(this.config);
        this.config.SavedProfiles[selectedProfileIndex] = selected;
        this.config.SelectedSavedProfileId = selected.Id;
        this.profileNameInput = selected.DisplayName;
        this.onConfigChanged();
    }

    private void LoadSelectedSavedProfile(int selectedProfileIndex)
    {
        if (selectedProfileIndex < 0 || selectedProfileIndex >= this.config.SavedProfiles.Count)
        {
            return;
        }

        var selected = this.config.SavedProfiles[selectedProfileIndex];
        this.ApplySnapshotToCurrentConfiguration(selected.Snapshot);
        this.config.SelectedSavedProfileId = selected.Id;
        this.profileNameInput = selected.DisplayName;
        this.layoutEditorWindow.UpdateConfiguration(this.config);
        this.CaptureSessionBaselines();
        this.onConfigChanged();
    }

    private void DeleteSelectedSavedProfile(int selectedProfileIndex)
    {
        if (selectedProfileIndex < 0 || selectedProfileIndex >= this.config.SavedProfiles.Count)
        {
            return;
        }

        this.config.SavedProfiles.RemoveAt(selectedProfileIndex);
        if (this.config.SavedProfiles.Count == 0)
        {
            this.config.SelectedSavedProfileId = string.Empty;
            this.profileNameInput = string.Empty;
        }
        else
        {
            var nextIndex = Math.Clamp(selectedProfileIndex, 0, this.config.SavedProfiles.Count - 1);
            this.config.SelectedSavedProfileId = this.config.SavedProfiles[nextIndex].Id;
            this.profileNameInput = this.config.SavedProfiles[nextIndex].DisplayName;
        }

        this.onConfigChanged();
    }

    private bool TryGetSelectedSavedProfileIndex(out int index)
    {
        index = -1;
        if (this.config.SavedProfiles.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(this.config.SelectedSavedProfileId))
        {
            index = 0;
            this.config.SelectedSavedProfileId = this.config.SavedProfiles[0].Id;
            this.profileNameInput = this.config.SavedProfiles[0].DisplayName;
            return true;
        }

        for (var i = 0; i < this.config.SavedProfiles.Count; i++)
        {
            if (string.Equals(this.config.SavedProfiles[i].Id, this.config.SelectedSavedProfileId, StringComparison.Ordinal))
            {
                index = i;
                return true;
            }
        }

        index = 0;
        this.config.SelectedSavedProfileId = this.config.SavedProfiles[0].Id;
        this.profileNameInput = this.config.SavedProfiles[0].DisplayName;
        return true;
    }

    private PluginConfiguration CloneNameplateConfiguration(PluginConfiguration source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<PluginConfiguration>(json) ?? new PluginConfiguration();
    }

    private void ApplySnapshotToCurrentConfiguration(PluginConfiguration snapshot)
    {
        var clone = this.CloneNameplateConfiguration(snapshot);
        this.config.Enabled = clone.Enabled;
        this.config.VerticalOffset = clone.VerticalOffset;
        this.config.TemporaryGlobalScale = clone.TemporaryGlobalScale;
        this.config.EnableDistanceCulling = clone.EnableDistanceCulling;
        this.config.EnemyMaxDistanceYalms = clone.EnemyMaxDistanceYalms;
        this.config.FriendlyMaxDistanceYalms = clone.FriendlyMaxDistanceYalms;
        this.config.PlayerMaxDistanceYalms = clone.PlayerMaxDistanceYalms;
        this.config.EnableDynamicCombatRange = clone.EnableDynamicCombatRange;
        this.config.CombatEnemyMaxDistanceYalms = clone.CombatEnemyMaxDistanceYalms;
        this.config.CombatFriendlyMaxDistanceYalms = clone.CombatFriendlyMaxDistanceYalms;
        this.config.EnableOcclusionCulling = clone.EnableOcclusionCulling;
        this.config.OcclusionMode = clone.OcclusionMode;
        this.config.OcclusionType = clone.OcclusionType;
        this.config.BossTargetBarAnchorOffset = clone.BossTargetBarAnchorOffset;
        this.config.DefaultFontFamilyId = clone.DefaultFontFamilyId;
        this.config.CategoryVisibility = clone.CategoryVisibility;
        this.config.SelfVisual = clone.SelfVisual;
        this.config.SelfCompanionVisual = clone.SelfCompanionVisual;
        this.config.SelfPetVisual = clone.SelfPetVisual;
        this.config.PartyVisual = clone.PartyVisual;
        this.config.PartyCompanionVisual = clone.PartyCompanionVisual;
        this.config.PartyPetVisual = clone.PartyPetVisual;
        this.config.AllianceVisual = clone.AllianceVisual;
        this.config.AlliancePetVisual = clone.AlliancePetVisual;
        this.config.FriendVisual = clone.FriendVisual;
        this.config.FriendCompanionVisual = clone.FriendCompanionVisual;
        this.config.FriendPetVisual = clone.FriendPetVisual;
        this.config.OtherPcVisual = clone.OtherPcVisual;
        this.config.OtherCompanionVisual = clone.OtherCompanionVisual;
        this.config.OtherPetVisual = clone.OtherPetVisual;
        this.config.EnemyUnengagedVisual = clone.EnemyUnengagedVisual;
        this.config.EnemyEngagedVisual = clone.EnemyEngagedVisual;
        this.config.EnemyClaimedVisual = clone.EnemyClaimedVisual;
        this.config.EnemyUnclaimedVisual = clone.EnemyUnclaimedVisual;
        this.config.EnemyFeastVisual = clone.EnemyFeastVisual;
        this.config.EnemyFeastPetVisual = clone.EnemyFeastPetVisual;
        this.config.BossVisual = clone.BossVisual;
        this.config.NpcVisual = clone.NpcVisual;
        this.config.ObjectVisual = clone.ObjectVisual;
        this.config.MinionVisual = clone.MinionVisual;
        this.config.HousingFurnitureVisual = clone.HousingFurnitureVisual;
        this.config.HousingFieldVisual = clone.HousingFieldVisual;
        this.config.ActiveProfileId = clone.ActiveProfileId;
        this.config.Profiles = clone.Profiles;
        this.config.InvalidateCaches();
        _ = this.config.GetActiveProfile();
    }

    private void ResetGeneralSettings()
    {
        this.config.Enabled = this.generalBaseline.Enabled;
        this.config.TemporaryGlobalScale = this.generalBaseline.TemporaryGlobalScale;
        this.config.EnableDistanceCulling = this.generalBaseline.EnableDistanceCulling;
        this.config.EnemyMaxDistanceYalms = this.generalBaseline.EnemyMaxDistanceYalms;
        this.config.FriendlyMaxDistanceYalms = this.generalBaseline.FriendlyMaxDistanceYalms;
        this.config.PlayerMaxDistanceYalms = this.generalBaseline.PlayerMaxDistanceYalms;
        this.config.EnableDynamicCombatRange = this.generalBaseline.EnableDynamicCombatRange;
        this.config.CombatEnemyMaxDistanceYalms = this.generalBaseline.CombatEnemyMaxDistanceYalms;
        this.config.CombatFriendlyMaxDistanceYalms = this.generalBaseline.CombatFriendlyMaxDistanceYalms;
        this.config.EnableOcclusionCulling = this.generalBaseline.EnableOcclusionCulling;
        this.config.OcclusionMode = this.generalBaseline.OcclusionMode;
        this.config.OcclusionType = this.generalBaseline.OcclusionType;
    }

    public void CaptureSessionBaselines()
    {
        this.generalBaseline = new GeneralSessionBaseline(
            this.config.Enabled,
            this.config.TemporaryGlobalScale,
            this.config.EnableDistanceCulling,
            this.config.EnemyMaxDistanceYalms,
            this.config.FriendlyMaxDistanceYalms,
            this.config.PlayerMaxDistanceYalms,
            this.config.EnableDynamicCombatRange,
            this.config.CombatEnemyMaxDistanceYalms,
            this.config.CombatFriendlyMaxDistanceYalms,
            this.config.EnableOcclusionCulling,
            this.config.OcclusionMode,
            this.config.OcclusionType);
    }

    public void DrawCategoryDesignerSection()
    {
        this.DrawAdvancedCategoryMapping();
        this.layoutEditorWindow.Draw();
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

    private void DrawGlobalDefaultFontSelector()
    {
        ImGui.TextUnformatted("Global Default Nameplate Font");
        var (ids, labels) = Rendering.GameFontRegistry.GetFontOptions();
        var normalized = Rendering.GameFontRegistry.NormalizeFamilyId(this.config.DefaultFontFamilyId);
        var current = Array.IndexOf(ids, normalized);
        if (current < 0)
        {
            current = 0;
        }

        if (ImGui.Combo("Default Font##nameplate_global_default_font", ref current, labels, labels.Length))
        {
            var selectedId = current >= 0 && current < ids.Length ? ids[current] : 0;
            this.config.DefaultFontFamilyId = Rendering.GameFontRegistry.NormalizeFamilyId(selectedId);
            this.onConfigChanged();
        }

        ImGui.TextColored(0xFF9AA1AB, "Categories can inherit this font or override it in Designer.");
    }

    private readonly record struct GeneralSessionBaseline(
        bool Enabled,
        float TemporaryGlobalScale,
        bool EnableDistanceCulling,
        float EnemyMaxDistanceYalms,
        float FriendlyMaxDistanceYalms,
        float PlayerMaxDistanceYalms,
        bool EnableDynamicCombatRange,
        float CombatEnemyMaxDistanceYalms,
        float CombatFriendlyMaxDistanceYalms,
        bool EnableOcclusionCulling,
        NameplateOcclusionMode OcclusionMode,
        NameplateOcclusionType OcclusionType);

    private void DrawAdvancedCategoryMapping()
    {
        _ = this.config.GetActiveProfile();
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
                    "Own",
                    "own_self",
                    NameplateManager.NameplateCategory.Self,
                    () => v.Self,
                    value => v.Self = value,
                    this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Self));
                DrawCategorySection(
                    "Companions",
                    "own_companion",
                    NameplateManager.NameplateCategory.SelfCompanion,
                    () => v.SelfCompanion,
                    value => v.SelfCompanion = value,
                    this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.SelfCompanion));
                DrawCategorySection(
                    "Pets",
                    "own_pet",
                    NameplateManager.NameplateCategory.SelfPet,
                    () => v.SelfPet,
                    value => v.SelfPet = value,
                    this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.SelfPet));
                break;
            case GroupTab.Others:
                DrawCategorySection("Party Members", "party_member", NameplateManager.NameplateCategory.Party, () => v.PartyMember, value => v.PartyMember = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Party));
                DrawCategorySection("Party Companions", "party_companion", NameplateManager.NameplateCategory.PartyCompanion, () => v.PartyCompanion, value => v.PartyCompanion = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.PartyCompanion));
                DrawCategorySection("Party Pets", "party_pet", NameplateManager.NameplateCategory.PartyPet, () => v.PartyPet, value => v.PartyPet = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.PartyPet));
                DrawCategorySection("Alliance Members", "alliance_member", NameplateManager.NameplateCategory.Alliance, () => v.AllianceMember, value => v.AllianceMember = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Alliance));
                DrawCategorySection("Alliance Pets", "alliance_pet", NameplateManager.NameplateCategory.AlliancePet, () => v.AlliancePet, value => v.AlliancePet = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.AlliancePet));
                DrawCategorySection("Friends", "friend", NameplateManager.NameplateCategory.Friend, () => v.Friend, value => v.Friend = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Friend));
                DrawCategorySection("Friend Companions", "friend_companion", NameplateManager.NameplateCategory.FriendCompanion, () => v.FriendCompanion, value => v.FriendCompanion = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.FriendCompanion));
                DrawCategorySection("Friend Pets", "friend_pet", NameplateManager.NameplateCategory.FriendPet, () => v.FriendPet, value => v.FriendPet = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.FriendPet));
                DrawCategorySection("Other PCs", "other_pc", NameplateManager.NameplateCategory.OtherPc, () => v.OtherPc, value => v.OtherPc = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.OtherPc));
                DrawCategorySection("Other Companions", "other_companion", NameplateManager.NameplateCategory.OtherCompanion, () => v.OtherCompanion, value => v.OtherCompanion = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.OtherCompanion));
                DrawCategorySection("Other Pets", "other_pet", NameplateManager.NameplateCategory.OtherPet, () => v.OtherPet, value => v.OtherPet = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.OtherPet));
                break;
            case GroupTab.Npcs:
                DrawCategorySection("Unengaged Enemies", "enemy_unengaged", NameplateManager.NameplateCategory.EnemyUnengaged, () => v.EnemyUnengaged, value => v.EnemyUnengaged = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyUnengaged));
                DrawCategorySection("Engaged Enemies", "enemy_engaged", NameplateManager.NameplateCategory.EnemyEngaged, () => v.EnemyEngaged, value => v.EnemyEngaged = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyEngaged));
                DrawCategorySection("Claimed Enemies", "enemy_claimed", NameplateManager.NameplateCategory.EnemyClaimed, () => v.EnemyClaimed, value => v.EnemyClaimed = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyClaimed));
                DrawCategorySection("Unclaimed Enemies", "enemy_unclaimed", NameplateManager.NameplateCategory.EnemyUnclaimed, () => v.EnemyUnclaimed, value => v.EnemyUnclaimed = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyUnclaimed));
                DrawCategorySection("Feast Enemies", "enemy_feast", NameplateManager.NameplateCategory.EnemyFeast, () => v.EnemyFeast, value => v.EnemyFeast = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyFeast));
                DrawCategorySection("Feast Enemy Pets", "enemy_feast_pet", NameplateManager.NameplateCategory.EnemyFeastPet, () => v.EnemyFeastPet, value => v.EnemyFeastPet = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.EnemyFeastPet));
                DrawCategorySection("Bosses (Target Bar)", "boss", NameplateManager.NameplateCategory.Boss, () => v.Boss, value => v.Boss = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Boss));
                DrawCategorySection("NPCs", "npc", NameplateManager.NameplateCategory.Npc, () => v.Npc, value => v.Npc = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Npc));
                DrawCategorySection("Objects", "object", NameplateManager.NameplateCategory.Object, () => v.Object, value => v.Object = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Object));
                DrawCategorySection("Minions", "minion", NameplateManager.NameplateCategory.Minion, () => v.Minion, value => v.Minion = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.Minion));
                DrawCategorySection("Housing Furniture", "housing_furniture", NameplateManager.NameplateCategory.HousingFurniture, () => v.HousingFurniture, value => v.HousingFurniture = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.HousingFurniture));
                DrawCategorySection("Housing Gardens", "housing_field", NameplateManager.NameplateCategory.HousingField, () => v.HousingField, value => v.HousingField = value, this.config.GetVisualSettingsForCategory(NameplateManager.NameplateCategory.HousingField));
                break;
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
        var isEditing = string.Equals(this.layoutEditorWindow.ActiveCategoryId, categoryId, StringComparison.Ordinal);
        var usesGlobalFont = visuals.UseGlobalFont ?? visuals.FontFamilyId == 0;
        var fontModeLabel = usesGlobalFont ? "Global Font" : "Custom Font";
        if (isEditing)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF4E3112);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF6B431A);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF7A4D1E);
        }

        var buttonLabel = isEditing
            ? $"{title} [Editing]"
            : title;
        var buttonId = $"{buttonLabel}##category_open_{categoryId}";
        var availWidth = ImGui.GetContentRegionAvail().X;
        var toggleWidth = 28f;
        var copyPasteWidth = 56f;
        var badgeWidth = 115f;
        var buttonWidth = Math.Max(180f, availWidth - badgeWidth - toggleWidth - copyPasteWidth - 20f);
        ImGui.AlignTextToFramePadding();
        var categoryEnabled = enabled;
        if (ImGui.Checkbox($"##category_enabled_{categoryId}", ref categoryEnabled))
        {
            setCategoryEnabled(categoryEnabled);
            this.onConfigChanged();
            enabled = categoryEnabled;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Enable or disable this nameplate category.");
        }

        ImGui.SameLine();
        if (ImGui.Button(buttonId, new Vector2(buttonWidth, 28f)))
        {
            this.layoutEditorWindow.Open(new LayoutEditorWindow.CategoryEditorTarget(
                categoryId,
                title,
                category,
                getCategoryEnabled,
                setCategoryEnabled,
                visuals));
        }

        ImGui.SameLine();
        var copyClicked = ImGui.SmallButton($"C##category_copy_{categoryId}");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Copy this category layout and visual settings.");
        }

        if (copyClicked)
        {
            this.copiedCategoryVisualSettings = LayoutEditorWindow.CloneVisualSettings(visuals);
            this.copiedCategoryVisualSourceLabel = title;
        }

        ImGui.SameLine();
        var canPaste = this.copiedCategoryVisualSettings is not null;
        if (!canPaste)
        {
            ImGui.BeginDisabled();
        }

        var pasteClicked = ImGui.SmallButton($"P##category_paste_{categoryId}");
        if (!canPaste)
        {
            ImGui.EndDisabled();
        }

        if (ImGui.IsItemHovered())
        {
            var tooltip = canPaste
                ? $"Paste copied layout settings from {this.copiedCategoryVisualSourceLabel}."
                : "Copy a category first, then paste here.";
            ImGui.SetTooltip(tooltip);
        }

        if (pasteClicked && this.copiedCategoryVisualSettings is not null)
        {
            LayoutEditorWindow.ApplyVisualSettings(visuals, this.copiedCategoryVisualSettings);
            visuals.EnsureDefaults();
            this.onConfigChanged();
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        var badgeColor = usesGlobalFont ? 0xFFA6C8FF : 0xFFFFC38A;
        ImGui.TextColored(badgeColor, fontModeLabel);

        if (isEditing)
        {
            ImGui.PopStyleColor(3);
        }
    }

    private enum GroupTab
    {
        Own = 0,
        Others = 1,
        Npcs = 2,
    }
}

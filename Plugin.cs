using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FFXIVHudPlugin;

public sealed unsafe class Plugin : IDalamudPlugin
{
    private static readonly TimeSpan TransitionHideGrace = TimeSpan.FromMilliseconds(450);

    public string Name => "FFXIV Hud Reimagined";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly IObjectTable objectTable;
    private readonly HudConfiguration configuration;
    private readonly HudStateProvider stateProvider;
    private readonly HudWindow hudWindow;
    private readonly ConfigWindow configWindow;
    private DateTime hideHudUntilUtc = DateTime.MinValue;
    private bool nativeUiHiddenApplied;
    private static readonly string[] NativeUiAddonNamesToHide =
    {
        "_ActionBar",
        "_ActionBar01",
        "_ActionBar02",
        "_ActionBar03",
        "_ActionBar04",
        "_ActionBar05",
        "_ActionBar06",
        "_ActionBar07",
        "_ActionBar08",
        "_ActionBar09",
        "_ActionBarEx",
        "_CastBar",
        "_ParameterWidget",
        "_Status",
        "_StatusCustom0",
        "_StatusCustom1",
        "_StatusCustom2",
        "_StatusCustom3",
        "_StatusCustom4",
        "_LimitBreak",
    };

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IClientState clientState,
        ICondition condition,
        IObjectTable objectTable,
        IDataManager dataManager,
        ITextureProvider textureProvider,
        IPartyList partyList,
        IPluginLog pluginLog)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.clientState = clientState;
        this.condition = condition;
        this.objectTable = objectTable;
        this.configuration = pluginInterface.GetPluginConfig() as HudConfiguration ?? new HudConfiguration();
        this.configuration.Initialize(pluginInterface);

        this.stateProvider = new HudStateProvider(
            objectTable,
            dataManager,
            textureProvider,
            pluginLog,
            partyList,
            condition,
            clientState,
            this.configuration);
        this.hudWindow = new HudWindow(this.configuration, this.stateProvider);
        this.configWindow = new ConfigWindow(this.configuration, this.stateProvider);

        this.commandManager.AddHandler(PluginCommands.MainCommand, PluginCommands.CreateCommand(this.ToggleConfig));

        pluginInterface.UiBuilder.Draw += this.DrawUi;
        pluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfig;
        pluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;
    }

    public void Dispose()
    {
        NativeMinimapVisibility.Apply(false);

        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfig;
        this.pluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        this.commandManager.RemoveHandler(PluginCommands.MainCommand);
    }

    private void DrawUi()
    {
        this.ApplyNativeUiVisibility();
        this.ApplyCustomMinimapVisibility();

        if (this.ShouldDrawHud())
        {
            this.stateProvider.Update();
            this.hudWindow.Draw();
        }

        this.configWindow.Draw();
    }

    private bool ShouldDrawHud()
    {
        if (!this.configuration.Enabled)
        {
            return false;
        }

        if (!this.clientState.IsLoggedIn || this.objectTable.LocalPlayer is null)
        {
            return false;
        }

        if (this.condition[ConditionFlag.OccupiedInCutSceneEvent] ||
            this.condition[ConditionFlag.WatchingCutscene] ||
            this.condition[ConditionFlag.WatchingCutscene78] ||
            this.condition[ConditionFlag.BetweenAreas] ||
            this.condition[ConditionFlag.BetweenAreas51])
        {
            this.hideHudUntilUtc = DateTime.UtcNow + TransitionHideGrace;
            return false;
        }

        if (DateTime.UtcNow < this.hideHudUntilUtc)
        {
            return false;
        }

        return true;
    }

    private void ToggleConfig()
    {
        this.configWindow.IsOpen = !this.configWindow.IsOpen;
    }

    private void ToggleMainUi()
    {
        this.configuration.Enabled = !this.configuration.Enabled;
        this.configuration.Save();
    }

    private void ApplyNativeUiVisibility()
    {
        var shouldHide = this.clientState.IsLoggedIn &&
                         this.objectTable.LocalPlayer is not null;

        if (shouldHide)
        {
            this.SetNativeUiVisibility(false);
            this.nativeUiHiddenApplied = true;
            return;
        }

        if (!this.nativeUiHiddenApplied)
        {
            return;
        }

        this.SetNativeUiVisibility(true);
        this.nativeUiHiddenApplied = false;
    }

    private void ApplyCustomMinimapVisibility()
    {
        var useCustomMinimap = this.clientState.IsLoggedIn &&
                               this.objectTable.LocalPlayer is not null &&
                               this.configuration.Enabled &&
                               this.configuration.MinimapEnabled;
        NativeMinimapVisibility.Apply(useCustomMinimap);

        if (useCustomMinimap)
        {
            MinimapNativeNorthLock.Apply(this.configuration.MinimapNorthLocked);
        }
    }

    private void SetNativeUiVisibility(bool visible)
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            return;
        }

        var manager = stage->RaptureAtkUnitManager;
        foreach (var addonName in NativeUiAddonNamesToHide)
        {
            var addon = manager->GetAddonByName(addonName, 1);
            if (addon is null)
            {
                continue;
            }

            addon->IsVisible = visible;
        }
    }
}

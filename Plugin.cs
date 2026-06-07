using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Gui.NamePlate;
using FFXIVHudPlugin.AetherPlates.Rendering;
using FFXIVHudPlugin.AetherPlates.Core;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;

namespace FFXIVHudPlugin;

public sealed unsafe class Plugin : IDalamudPlugin
{
    private static readonly TimeSpan TransitionHideGrace = TimeSpan.FromMilliseconds(450);
    private static readonly TimeSpan DrawErrorLogThrottle = TimeSpan.FromSeconds(5);

    public string Name => "FFXIV Hud Reimagined";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly IClientState clientState;
    private readonly ICondition condition;
    private readonly IObjectTable objectTable;
    private readonly IPluginLog pluginLog;
    private readonly IFramework framework;
    private readonly IAddonLifecycle addonLifecycle;
    private readonly INamePlateGui namePlateGui;
    private readonly ITextureProvider textureProvider;
    private readonly ITargetManager targetManager;
    private readonly IPartyList partyList;
    private readonly IDataManager dataManager;
    private readonly HudConfiguration configuration;
    private readonly HudStateProvider stateProvider;
    private readonly HudWindow hudWindow;
    private readonly ConfigWindow configWindow;
    private readonly ActionCameraPlugin actionCameraPlugin;
    private readonly AetherPlates.Core.NameplateManager nameplateManager;
    private readonly AetherPlates.Services.NativeNameplateAnchorService nativeNameplateAnchorService;
    private DateTime hideHudUntilUtc = DateTime.MinValue;
    private DateTime nextDrawErrorLogUtc = DateTime.MinValue;
    private bool nativeUiHiddenApplied;
    private bool layoutMigrationInitialized;
    private bool nativeNorthLockCaptured;
    private bool nativeNorthLockApplied;
    private bool nativeNorthLockOriginal;
    private bool nativeCastBarSuppressed;
    private bool nativeCastBarOriginalPosCaptured;
    private Vector2 nativeCastBarOriginalPos;
    private int suppressedDrawErrors;
    private bool isDisposed;
    private string lastSelfNativeSuppressionState = "inactive";
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
        IFramework framework,
        IAddonLifecycle addonLifecycle,
        INamePlateGui namePlateGui,
        IKeyState keyState,
        IGameGui gameGui,
        ITargetManager targetManager,
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
        this.framework = framework;
        this.addonLifecycle = addonLifecycle;
        this.namePlateGui = namePlateGui;
        this.textureProvider = textureProvider;
        this.targetManager = targetManager;
        this.partyList = partyList;
        this.dataManager = dataManager;
        this.pluginLog = pluginLog;
        GameFontRegistry.Initialize(
            pluginInterface.UiBuilder,
            pluginLog,
            pluginInterface.AssemblyLocation.DirectoryName,
            pluginInterface.GetPluginConfigDirectory());
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
        this.hudWindow = new HudWindow(this.configuration, this.stateProvider, this.OpenMinimapConfig);
        var inputManager = new InputManager(keyState, this.configuration.ActionCamera);
        var uiStateService = new UiStateService(gameGui);
        var cursorManager = new CursorManager();
        var cameraProvider = new FfxivClientStructsCameraProvider();
        var cameraController = new CameraController(this.configuration.ActionCamera, cameraProvider);
        var rmbLatchBackend = new RmbLatchCameraBackend(this.configuration.ActionCamera);
        var directBackend = new DirectCameraControlBackend(cameraController);
        var softTargetService = new SoftTargetService(this.configuration.ActionCamera, objectTable, partyList, targetManager);
        this.actionCameraPlugin = new ActionCameraPlugin(
            this.configuration,
            this.clientState,
            this.objectTable,
            inputManager,
            uiStateService,
            cursorManager,
            targetManager,
            cameraController,
            rmbLatchBackend,
            directBackend,
            softTargetService,
            pluginLog);
        this.configWindow = new ConfigWindow(this.configuration, this.stateProvider, _ => { });
        this.nativeNameplateAnchorService = new AetherPlates.Services.NativeNameplateAnchorService(
            this.namePlateGui);
        this.nameplateManager = this.CreateNameplateManager();

        this.commandManager.AddHandler(PluginCommands.MainCommand, PluginCommands.CreateCommand(this.ToggleConfig));
        this.commandManager.AddHandler(
            PluginCommands.ActionCameraCommand,
            PluginCommands.CreateActionCameraCommand(this.ToggleActionCamera));

        pluginInterface.UiBuilder.Draw += this.DrawUi;
        pluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfig;
        pluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;
        this.framework.Update += this.OnFrameworkUpdate;
    }

    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.isDisposed = true;

        this.RestoreNativeUiState();

        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfig;
        this.pluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        this.framework.Update -= this.OnFrameworkUpdate;
        this.commandManager.RemoveHandler(PluginCommands.MainCommand);
        this.commandManager.RemoveHandler(PluginCommands.ActionCameraCommand);
        this.actionCameraPlugin.Dispose();
        this.nativeNameplateAnchorService.Dispose();
    }

    private void DrawUi()
    {
        this.ExecuteDrawSection("action camera update", this.actionCameraPlugin.Update);
        this.ExecuteDrawSection("action camera late update", this.actionCameraPlugin.LateUpdate);
        this.ExecuteDrawSection("layout migration init", this.ApplyLayoutMigrationIfNeeded);
        this.ExecuteDrawSection("native UI visibility", this.ApplyNativeUiVisibility);
        this.ExecuteDrawSection("native minimap north-lock sync", this.ApplyMinimapNorthLockWhenCustom);

        var configOpen = this.configWindow.IsOpen;
        if (this.ShouldDrawHud())
        {
            this.ExecuteDrawSection("state update", this.stateProvider.Update);
            this.ExecuteDrawSection("hud draw", () => this.hudWindow.Draw(configOpen));
        }

        this.ExecuteDrawSection("config draw", this.configWindow.Draw);
        this.ExecuteDrawSection("aetherplates draw", this.nameplateManager.UpdateAndDraw);
        this.ExecuteDrawSection(
            "action camera overlay",
            () => ActionCameraOverlay.Draw(this.configuration.ActionCamera, this.actionCameraPlugin.RuntimeState));
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

    private void OpenMinimapConfig()
    {
        this.configWindow.IsOpen = true;
        this.configWindow.SelectMinimapTab();
    }

    private void ToggleMainUi()
    {
        this.configuration.Enabled = !this.configuration.Enabled;
        this.configuration.Save();
    }

    private void ToggleActionCamera()
    {
        this.actionCameraPlugin.Toggle();
    }

    private void OnFrameworkUpdate(IFramework _) { }

    private void ApplyNativeUiVisibility()
    {
        var inWorld = this.clientState.IsLoggedIn &&
                      this.objectTable.LocalPlayer is not null;

        if (!inWorld)
        {
            if (this.nativeUiHiddenApplied)
            {
                this.SetNativeUiVisibility(true);
                this.UpdateNativeCastBarSuppression(false);
                NativeMinimapVisibility.SetVisible(true);
                this.nativeUiHiddenApplied = false;
            }

            this.RestoreNativeNorthLockState();

            return;
        }

        this.UpdateNativeNameplateVisibility();

        if (!this.configuration.Enabled)
        {
            if (!this.nativeUiHiddenApplied)
            {
                return;
            }

            this.SetNativeUiVisibility(true);
            this.UpdateNativeCastBarSuppression(false);
            NativeMinimapVisibility.SetVisible(true);
            this.nativeUiHiddenApplied = false;
            return;
        }

        this.SetNativeUiVisibility(false);
        this.UpdateNativeCastBarSuppression(true);
        NativeMinimapVisibility.SetVisible(!this.configuration.MinimapEnabled);
        this.nativeUiHiddenApplied = true;
    }

    private void ApplyMinimapNorthLockWhenCustom()
    {
        if (!this.clientState.IsLoggedIn ||
            this.objectTable.LocalPlayer is null ||
            !this.configuration.Enabled ||
            !this.configuration.MinimapEnabled)
        {
            this.RestoreNativeNorthLockState();
            return;
        }

        this.CaptureNativeNorthLockStateIfNeeded();
        MinimapNativeNorthLock.Apply(this.configuration.MinimapNorthLocked);
        this.nativeNorthLockApplied = true;
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

    private void UpdateNativeNameplateVisibility()
    {
        this.lastSelfNativeSuppressionState = this.configuration.AetherPlates.Enabled
            ? "native-config=unmanaged"
            : "plugin=off";
    }

    private void UpdateNativeNameplateConfigSuppression(AetherPlates.Configuration.PluginConfiguration aetherPlates)
    {
        // Native nameplate character-config options are user-owned and never modified by this plugin.
        this.lastSelfNativeSuppressionState = aetherPlates.Enabled
            ? "native-config=unmanaged"
            : "plugin=off";
    }

    private void ApplyLayoutMigrationIfNeeded()
    {
        if (this.layoutMigrationInitialized)
        {
            return;
        }

        var viewport = Dalamud.Bindings.ImGui.ImGui.GetMainViewport();
        if (viewport.Size.X <= 0f || viewport.Size.Y <= 0f)
        {
            return;
        }

        HudLayoutMigration.MigrateLayoutOffsetsIfNeeded(this.configuration, viewport.Pos, viewport.Size);
        this.layoutMigrationInitialized = true;
    }

    private void CaptureNativeNorthLockStateIfNeeded()
    {
        if (this.nativeNorthLockCaptured)
        {
            return;
        }

        if (!MinimapNativeNorthLock.TryGetCurrent(out var currentNorthLock))
        {
            return;
        }

        this.nativeNorthLockOriginal = currentNorthLock;
        this.nativeNorthLockCaptured = true;
    }

    private void RestoreNativeNorthLockState()
    {
        if (!this.nativeNorthLockApplied)
        {
            return;
        }

        if (this.nativeNorthLockCaptured)
        {
            MinimapNativeNorthLock.Apply(this.nativeNorthLockOriginal);
        }

        this.nativeNorthLockApplied = false;
        this.nativeNorthLockCaptured = false;
        this.nativeNorthLockOriginal = false;
    }

    private void RestoreNativeUiState()
    {
        this.ExecuteDrawSection("native UI restore", () =>
        {
            this.UpdateNativeCastBarSuppression(false);
            this.SetNativeUiVisibility(true);
            NativeMinimapVisibility.SetVisible(true);
            this.nativeUiHiddenApplied = false;
            this.RestoreNativeNorthLockState();
        });
    }

    private void UpdateNativeCastBarSuppression(bool suppress)
    {
        if (suppress)
        {
            if (this.nativeCastBarSuppressed)
            {
                return;
            }

            this.addonLifecycle.RegisterListener(AddonEvent.PreDraw, "_CastBar", (_, args) =>
            {
                var addon = (AtkUnitBase*)args.Addon.Address;
                this.HideCastBarOffscreen(addon);
            });
            this.nativeCastBarSuppressed = true;

            // Apply immediately if castbar is already present this frame.
            var stage = AtkStage.Instance();
            var manager = stage is null ? null : stage->RaptureAtkUnitManager;
            var addonNow = manager is null ? null : manager->GetAddonByName("_CastBar", 1);
            this.HideCastBarOffscreen(addonNow);
            return;
        }

        if (!this.nativeCastBarSuppressed)
        {
            return;
        }

        this.addonLifecycle.UnregisterListener(AddonEvent.PreDraw, "_CastBar");
        this.nativeCastBarSuppressed = false;
        this.TryRestoreCastBarPosition();
    }

    private void HideCastBarOffscreen(AtkUnitBase* addon)
    {
        if (addon is null || addon->RootNode is null)
        {
            return;
        }

        if (!this.nativeCastBarOriginalPosCaptured)
        {
            this.nativeCastBarOriginalPos = new Vector2(
                addon->RootNode->GetXFloat(),
                addon->RootNode->GetYFloat());
            this.nativeCastBarOriginalPosCaptured = true;
        }

        addon->RootNode->SetPositionFloat(-9999f, -9999f);
    }

    private void TryRestoreCastBarPosition()
    {
        if (!this.nativeCastBarOriginalPosCaptured)
        {
            return;
        }

        var stage = AtkStage.Instance();
        if (stage is null)
        {
            this.nativeCastBarOriginalPosCaptured = false;
            this.nativeCastBarOriginalPos = Vector2.Zero;
            return;
        }

        var addon = stage->RaptureAtkUnitManager->GetAddonByName("_CastBar", 1);
        if (addon is not null && addon->RootNode is not null)
        {
            addon->RootNode->SetPositionFloat(
                this.nativeCastBarOriginalPos.X,
                this.nativeCastBarOriginalPos.Y);
        }

        this.nativeCastBarOriginalPosCaptured = false;
        this.nativeCastBarOriginalPos = Vector2.Zero;
    }

    private void ExecuteDrawSection(string sectionName, Action drawAction)
    {
        try
        {
            drawAction();
        }
        catch (Exception ex)
        {
            this.LogDrawFailure(sectionName, ex);
        }
    }

    private void LogDrawFailure(string sectionName, Exception ex)
    {
        var now = DateTime.UtcNow;
        if (now >= this.nextDrawErrorLogUtc)
        {
            if (this.suppressedDrawErrors > 0)
            {
                this.pluginLog.Warning($"Suppressed {this.suppressedDrawErrors} draw exceptions while throttled.");
                this.suppressedDrawErrors = 0;
            }

            this.pluginLog.Warning(ex, $"Draw section '{sectionName}' failed; rendering will continue.");
            this.nextDrawErrorLogUtc = now + DrawErrorLogThrottle;
            return;
        }

        this.suppressedDrawErrors++;
    }

    private AetherPlates.Core.NameplateManager CreateNameplateManager()
    {
        var widgetRegistry = new AetherPlates.Widgets.WidgetRegistry();
        AetherPlates.Widgets.WidgetRegistration.RegisterBuiltIns(widgetRegistry);

        var styleManager = new AetherPlates.Styles.StyleManager(this.configuration.AetherPlates.GetActiveStyles);
        var layoutEngine = new AetherPlates.Layout.LayoutEngine();
        var renderer = new AetherPlates.Rendering.ImGuiRenderer();
        var objectService = new AetherPlates.Services.ObjectService(
            this.objectTable,
            this.targetManager,
            this.partyList,
            this.clientState,
            this.dataManager);
        var tracker = new AetherPlates.Core.NameplateTracker(objectService);
        var projectionService = new AetherPlates.Services.ProjectionService();
        var nameplateRenderer = new AetherPlates.Core.NameplateRenderer(
            widgetRegistry,
            layoutEngine,
            styleManager,
            renderer);

        return new AetherPlates.Core.NameplateManager(
            tracker,
            nameplateRenderer,
            projectionService,
            this.nativeNameplateAnchorService,
            this.textureProvider,
            this.configuration.AetherPlates);
    }
}

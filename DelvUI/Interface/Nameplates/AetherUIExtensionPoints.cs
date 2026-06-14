using System.Collections.Generic;

namespace DelvUI.Interface.Nameplates
{
    public interface IAetherUiNameplatesBackendHooks
    {
        void OnBeforeProfileApply(string previousProfileName, string nextProfileName);
        void OnAfterProfileApply(string activeProfileName, bool appliedSuccessfully);
    }

    public static class AetherUiNameplatesExtensionRegistry
    {
        public static IAetherUiNameplatesBackendHooks? BackendHooks { get; set; }

        // Optional preview source for future settings/front-end rework.
        public static IReadOnlyCollection<NameplateData>? OverridePreviewData { get; set; }
    }
}

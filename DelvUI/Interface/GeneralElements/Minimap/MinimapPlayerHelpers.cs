namespace DelvUI.Interface.GeneralElements
{
    internal static class MinimapPlayerPinColor
    {
        public static uint Resolve(MinimapConfig config, uint classJobId)
        {
            if (config.UseRolePinColor)
            {
                var roleColor = GlobalColors.Instance.SafeRoleColorForJobId(classJobId);
                return roleColor.Base;
            }

            if (config.UseJobPinColor)
            {
                var jobColor = GlobalColors.Instance.SafeColorForJobId(classJobId);
                return jobColor.Base;
            }

            return config.PlayerPinColor.Base;
        }
    }
}

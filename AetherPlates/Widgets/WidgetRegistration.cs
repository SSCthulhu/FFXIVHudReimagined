namespace FFXIVHudPlugin.AetherPlates.Widgets;

public static class WidgetRegistration
{
    public static void RegisterBuiltIns(WidgetRegistry registry)
    {
        var widgetType = typeof(INameplateWidget);
        var assembly = typeof(WidgetRegistration).Assembly;
        var types = assembly.GetTypes();
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (!widgetType.IsAssignableFrom(type))
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            if (Activator.CreateInstance(type) is INameplateWidget widget)
            {
                registry.Register(widget);
            }
        }
    }
}

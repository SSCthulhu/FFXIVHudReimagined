namespace FFXIVHudPlugin.AetherPlates.Widgets;

public sealed class WidgetRegistry
{
    private readonly Dictionary<string, INameplateWidget> widgets = new(StringComparer.Ordinal);

    public IReadOnlyCollection<INameplateWidget> Widgets => this.widgets.Values;

    public void Register(INameplateWidget widget)
    {
        this.widgets[widget.Id] = widget;
    }

    public void Register<T>() where T : INameplateWidget, new()
    {
        var widget = new T();
        this.widgets[widget.Id] = widget;
    }

    public bool TryGet(string id, out INameplateWidget? widget)
    {
        return this.widgets.TryGetValue(id, out widget);
    }
}

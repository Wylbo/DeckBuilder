public interface IUIServiceContext
{
    IUIViewFactory ViewFactory { get; }
    IUILayerController LayerController { get; }
    IUIHistoryTracker History { get; }
    IPauseService PauseService { get; }
}

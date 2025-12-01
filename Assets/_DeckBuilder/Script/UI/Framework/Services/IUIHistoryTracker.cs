public interface IUIHistoryTracker
{
    UIView GetLastOpenedView();
    void TrackViewOpened(UIView view);
    void TrackViewHidden(UIView view);
}

using System.Collections.Generic;

public class UIHistoryTracker : IUIHistoryTracker
{
    private readonly List<UIView> openHistory = new List<UIView>();

    public UIView GetLastOpenedView()
    {
        for (int i = openHistory.Count - 1; i >= 0; i--)
        {
            var candidate = openHistory[i];
            if (candidate == null)
            {
                openHistory.RemoveAt(i);
                continue;
            }

            if (candidate.IsVisible)
                return candidate;

            openHistory.RemoveAt(i);
        }

        return null;
    }

    public void TrackViewOpened(UIView view)
    {
        if (view == null)
            return;

        openHistory.Remove(view);
        openHistory.Add(view);
    }

    public void TrackViewHidden(UIView view)
    {
        if (view == null)
            return;

        openHistory.Remove(view);
    }
}

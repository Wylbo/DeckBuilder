using System.Collections.Generic;

public interface IUIViewFactory
{
    void RegisterPrefabs(IEnumerable<UIView> views);
    bool TryGetExisting<TView>(out TView view) where TView : UIView;
    TView GetOrCreate<TView>() where TView : UIView;
}

using UnityEngine;

public class RootNode : Node
{
    [HideInInspector] public Node child;

    public override Node Clone()
    {
        RootNode node = Instantiate(this);
        node.child = child;
        return node;
    }

    protected override void OnStart()
    {
    }

    protected override void OnStop()
    {
    }

    protected override State OnUpdate()
    {
        return child.Update();
    }
}

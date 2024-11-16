using UnityEngine;

[CreateAssetMenu(fileName = nameof(Blackboard), menuName = FileName.BehaviourTree + nameof(Blackboard))]
public class Blackboard : ScriptableObject
{
    [SerializeField]
    public Vector3 moveToPosition;

}

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Targetable : MonoBehaviour
{
    [SerializeField] private Collider targetableCollider;
    [SerializeField] private Character character;
    public Character Character => character;
}
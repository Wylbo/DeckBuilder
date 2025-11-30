using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<Ability> abilities;

    public List<Ability> Abilities => abilities;
}
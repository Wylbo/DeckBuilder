using UnityEngine;
using System;

public interface IOwnable
{
    public Character Owner { get; }

    public void SetOwner(Character character);
}
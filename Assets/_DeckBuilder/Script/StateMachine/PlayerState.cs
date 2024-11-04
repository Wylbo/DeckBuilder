using System;
using UnityEngine;

[Serializable]
public class PlayerState : State
{

}

[CreateAssetMenu(fileName = nameof(PlayerStateNone),
menuName = FileName.State + nameof(PlayerStateNone),
 order = 0)]

public class PlayerStateNone : PlayerState
{
	public override void Update() { }
}

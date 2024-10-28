using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VFXPool : MonoBehaviour
{
	private void OnParticleSystemStopped()
	{
		PoolManager.Release(gameObject);
	}
}

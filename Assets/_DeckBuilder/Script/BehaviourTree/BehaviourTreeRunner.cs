using UnityEngine;

namespace BehaviourTree
{
	public class BehaviourTreeRunner : MonoBehaviour
	{
		[SerializeField] private BehaviourTree tree;

		public BehaviourTree Tree { get => tree; set { tree = value; } }

		private void Start()
		{
			tree = tree.Clone();
		}

		private void Update()
		{
			tree.Update();
		}
	}
}

using UnityEngine;

namespace BehaviourTree
{
	public class BehaviourTreeRunner : MonoBehaviour
	{
		[SerializeField] private BehaviourTree tree;
		[SerializeField] private Character character;

		public BehaviourTree Tree { get => tree; set { tree = value; } }

		private void Start()
		{
			tree = tree.Clone();
			tree.Bind(character);
		}

		private void Update()
		{
			tree.Update();
		}
	}
}

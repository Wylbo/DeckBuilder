using System;
using UnityEditor;
using UnityEngine;

namespace BehaviourTree.Nodes
{
	public abstract class Node : ScriptableObject
	{
		public enum State
		{
			Running,
			Failure,
			Success,
		}

		[field: SerializeField, HideInInspector] public State CurrentState { get; protected set; } = State.Running;
		[field: SerializeField, HideInInspector] public bool Started { get; private set; } = false;
		[field: SerializeField, HideInInspector] public string GUID { get; private set; }
		[field: SerializeField, HideInInspector] public Vector2 Position { get; set; }
		[field: SerializeField, HideInInspector] public Blackboard Blackboard { get; set; }
		[field: SerializeField, HideInInspector] public Character Character { get; set; }

		public State Update()
		{
			if (!Started)
			{
				OnStart();
				Started = true;
			}

			CurrentState = OnUpdate();

			if (CurrentState == State.Failure || CurrentState == State.Success)
			{
				OnStop();
				Started = false;
			}

			return CurrentState;
		}

		public virtual Node Clone()
		{
			return Instantiate(this);
		}

		public String GenerateGUID()
		{
			GUID = UnityEditor.GUID.Generate().ToString();
			return GUID;
		}

		public void SetPosition(Rect rect)
		{
			SetPosition(new Vector2(rect.xMin, rect.yMin));
		}

		public void SetPosition(Vector2 position)
		{
			Position = position;
		}
		protected abstract void OnStart();
		protected abstract void OnStop();
		protected abstract State OnUpdate();
	}
}

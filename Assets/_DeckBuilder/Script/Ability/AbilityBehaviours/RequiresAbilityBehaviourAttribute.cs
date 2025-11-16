using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequiresAbilityBehaviourAttribute : Attribute
{
	public Type RequiredType { get; }
	public bool RequirePriorOccurrence { get; }

	public RequiresAbilityBehaviourAttribute(Type requiredType, bool requirePriorOccurrence = false)
	{
		RequiredType = requiredType;
		RequirePriorOccurrence = requirePriorOccurrence;
	}
}

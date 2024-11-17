using System;
using System.Collections.Generic;
using TheKiwiCoder;
using UnityEngine;

[System.Serializable]
public class ListKey<T> : BlackboardKey<List<T>>
{

}

public class BoolList : ListKey<bool>
{

}

public class IntList : ListKey<int>
{

}

public class FloatList : ListKey<float>
{

}

[System.Serializable]
public class DoubleList : ListKey<double>
{

}

[System.Serializable]
public class StringList : ListKey<string>
{

}

[System.Serializable]
public class Vector2List : ListKey<Vector2>
{

}

[System.Serializable]
public class Vector3List : ListKey<Vector3>
{

}

[System.Serializable]
public class Vector4List : ListKey<Vector4>
{

}

[System.Serializable]
public class Vector2IntList : ListKey<Vector2Int>
{

}

[System.Serializable]
public class Vector3IntList : ListKey<Vector3Int>
{

}

[System.Serializable]
public class GradientList : ListKey<Gradient>
{

}

[System.Serializable]
public class ColorList : ListKey<Color>
{

}

[System.Serializable]
public class LayerList : ListKey<int>
{

}

[System.Serializable]
public class LayerMaskList : ListKey<LayerMask>
{

}

[System.Serializable]
public class TagList : ListKey<string>
{

}

[System.Serializable]
public class CurveList : ListKey<AnimationCurve>
{

}

[System.Serializable]
public class BoundsList : ListKey<Bounds>
{

}

[System.Serializable]
public class BoundsIntList : ListKey<BoundsInt>
{

}

[System.Serializable]
public class GameObjectList : ListKey<GameObject>
{

}

[System.Serializable]
public class MaterialList : ListKey<Material>
{

}

[System.Serializable]
public class RigidBodyList : ListKey<Rigidbody>
{

}

[System.Serializable]
public class ColliderList : ListKey<Collider>
{

}

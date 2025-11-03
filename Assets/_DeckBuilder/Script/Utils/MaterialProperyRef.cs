using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public class MaterialPropertyRef
{
    public enum Kind { Float, Range, Color, Vector, Texture }


    [Title("Material & Property")]
    [Required, SerializeField] private Renderer renderer;

    [SerializeField, ShowIf(nameof(HasRenderer))]
    [ValueDropdown(nameof(GetMaterialIndexOptions))]
    private int materialIndex;

    [SerializeField, HideInInspector] private string propertyName = "";
    [SerializeField, HideInInspector] private Kind kind = Kind.Float;

    [ShowIf(nameof(CanPickProperty))]
    [LabelText("Shader Property")]
    [ValueDropdown(nameof(GetShaderPropertyOptions))]
    [OnValueChanged(nameof(OnPropertySelectionChanged))]
    [ShowInInspector]
    private PropertySelection _selectionProxy;


    [FoldoutGroup("Edit Value"), LabelText("Auto Apply")]
    public bool autoApply = true;

    [FoldoutGroup("Edit Value"), Button, DisableIf(nameof(IsInvalid))]
    public void Apply()
    {
        switch (kind)
        {
            case Kind.Float:
            case Kind.Range: SetValue(floatValue); break;
            case Kind.Color: SetValue(colorValue); break;
            case Kind.Vector: SetValue(vectorValue); break;
            case Kind.Texture: SetValue(textureValue); break;
        }
    }

    [FoldoutGroup("Edit Value"), Button, DisableIf(nameof(IsInvalid))]
    public void PullFromMaterial()
    {
        if (IsInvalid) return;
        EnsureIds();
        var mat = renderer.sharedMaterials[materialIndex];
        if (!mat) return;

        switch (kind)
        {
            case Kind.Float:
            case Kind.Range:
                if (mat.HasProperty(_propId)) floatValue = mat.GetFloat(_propId);
                break;
            case Kind.Color:
                if (mat.HasProperty(_propId)) colorValue = mat.GetColor(_propId);
                break;
            case Kind.Vector:
                if (mat.HasProperty(_propId)) vectorValue = mat.GetVector(_propId);
                break;
            case Kind.Texture:
                if (mat.HasProperty(_propId)) textureValue = mat.GetTexture(_propId);
                break;
        }
    }

    [FoldoutGroup("Edit Value")]
    [ShowIf(nameof(IsFloatLike))]
    [OnValueChanged(nameof(OnFloatChanged))]
    [PropertyRange(nameof(GetRangeMin), nameof(GetRangeMax))]
    [LabelText("Value")]
    public float floatValue;

    [FoldoutGroup("Edit Value")]
    [ShowIf(nameof(IsColor))]
    [OnValueChanged(nameof(OnColorChanged))]
    [LabelText("Color")]
    public Color colorValue = Color.white;

    [FoldoutGroup("Edit Value")]
    [ShowIf(nameof(IsVector))]
    [OnValueChanged(nameof(OnVectorChanged))]
    [LabelText("Vector4")]
    public Vector4 vectorValue = Vector4.zero;

    [FoldoutGroup("Edit Value")]
    [ShowIf(nameof(IsTexture))]
    [OnValueChanged(nameof(OnTextureChanged))]
    [LabelText("Texture")]
    public Texture textureValue;


    public Renderer Renderer => renderer;
    public int MaterialIndex => materialIndex;
    public string PropertyName => propertyName;
    public Kind PropertyKind => kind;


    [NonSerialized] private int _propId = -1;
    [NonSerialized] private MaterialPropertyBlock _block;

    public bool IsValid =>
        renderer != null &&
        renderer.sharedMaterials != null &&
        materialIndex >= 0 &&
        materialIndex < renderer.sharedMaterials.Length &&
        !string.IsNullOrEmpty(propertyName);

    private bool IsInvalid => !IsValid;
    private bool HasRenderer => renderer != null;
    private bool CanPickProperty => HasRenderer && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0;

    private bool IsFloatLike => kind == Kind.Float || kind == Kind.Range;
    private bool IsColor => kind == Kind.Color;
    private bool IsVector => kind == Kind.Vector;
    private bool IsTexture => kind == Kind.Texture;

    private void EnsureIds()
    {
        if (_propId == -1 && !string.IsNullOrEmpty(propertyName))
            _propId = Shader.PropertyToID(propertyName);
        if (_block == null) _block = new MaterialPropertyBlock();
    }

    private void GetBlock()
    {
        EnsureIds();
        renderer.GetPropertyBlock(_block, materialIndex);
    }

    private void ApplyBlock()
    {
        renderer.SetPropertyBlock(_block, materialIndex);
    }


    public void SetValue(float value)
    {
        if (!IsValid || !IsFloatLike) return;
        GetBlock();
        _block.SetFloat(_propId, value);
        ApplyBlock();
    }

    public void SetValue(Color value)
    {
        if (!IsValid || !IsColor) return;
        GetBlock();
        _block.SetColor(_propId, value);
        ApplyBlock();
    }

    public void SetValue(Vector4 value)
    {
        if (!IsValid || !IsVector) return;
        GetBlock();
        _block.SetVector(_propId, value);
        ApplyBlock();
    }

    public void SetValue(Texture value)
    {
        if (!IsValid || !IsTexture) return;
        GetBlock();
        _block.SetTexture(_propId, value);
        ApplyBlock();
    }

    public override string ToString()
        => IsValid ? $"{renderer.name}[{materialIndex}].{propertyName} ({kind})" : "(unassigned MaterialPropertyRef)";


    private void OnFloatChanged()
    {
        if (autoApply && IsFloatLike) SetValue(floatValue);
    }

    private void OnColorChanged()
    {
        if (autoApply && IsColor) SetValue(colorValue);
    }

    private void OnVectorChanged()
    {
        if (autoApply && IsVector) SetValue(vectorValue);
    }

    private void OnTextureChanged()
    {
        if (autoApply && IsTexture) SetValue(textureValue);
    }


    [Serializable]
    private struct PropertySelection
    {
        public string Name;
        public Kind Kind;
        public override string ToString() => string.IsNullOrEmpty(Name) ? "(none)" : $"{Name}  ({Kind})";
    }

    private void OnPropertySelectionChanged()
    {
        propertyName = _selectionProxy.Name;
        kind = _selectionProxy.Kind;
        _propId = -1;
        PullFromMaterial();
    }

#if UNITY_EDITOR
    private System.Collections.Generic.IEnumerable<ValueDropdownItem<int>> GetMaterialIndexOptions()
    {
        var list = new System.Collections.Generic.List<ValueDropdownItem<int>>();
        if (renderer == null) return list;

        var mats = renderer.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            list.Add(new ValueDropdownItem<int>("(No Materials)", 0));
            return list;
        }

        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            string name = mat ? mat.name : "(null)";
            list.Add(new ValueDropdownItem<int>($"{i}: {name}", i));
        }
        materialIndex = Mathf.Clamp(materialIndex, 0, mats.Length - 1);
        return list;
    }

    private System.Collections.Generic.IEnumerable<ValueDropdownItem<PropertySelection>> GetShaderPropertyOptions()
    {
        var list = new System.Collections.Generic.List<ValueDropdownItem<PropertySelection>>();

        if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
            return list;

        int idx = Mathf.Clamp(materialIndex, 0, renderer.sharedMaterials.Length - 1);
        var mat = renderer.sharedMaterials[idx];
        if (mat == null || mat.shader == null) return list;

        var shader = mat.shader;
        int count = shader.GetPropertyCount();

        for (int i = 0; i < count; i++)
        {
            var type = shader.GetPropertyType(i);
            if (!TryMap(type, out Kind k)) continue;

            string name = shader.GetPropertyName(i);
            var sel = new PropertySelection { Name = name, Kind = k };
            list.Add(new ValueDropdownItem<PropertySelection>($"{name}  ({k})", sel));
        }

        // keep dropdown showing the current selection
        if (!string.IsNullOrEmpty(propertyName))
            _selectionProxy = new PropertySelection { Name = propertyName, Kind = kind };

        return list;

        static bool TryMap(UnityEngine.Rendering.ShaderPropertyType t, out Kind k)
        {
            switch (t)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Float: k = Kind.Float; return true;
                case UnityEngine.Rendering.ShaderPropertyType.Range: k = Kind.Range; return true;
                case UnityEngine.Rendering.ShaderPropertyType.Color: k = Kind.Color; return true;
                case UnityEngine.Rendering.ShaderPropertyType.Vector: k = Kind.Vector; return true;
                case UnityEngine.Rendering.ShaderPropertyType.Texture: k = Kind.Texture; return true;
                default: k = Kind.Float; return false;
            }
        }
    }

    // Range slider limits pulled from the shader when Kind == Range
    private float GetRangeMin()
    {
        if (!IsValid || kind != Kind.Range) return 0f;
        if (!TryGetShaderRange(out float min, out _)) return 0f;
        return min;
    }
    private float GetRangeMax()
    {
        if (!IsValid || kind != Kind.Range) return 1f;
        if (!TryGetShaderRange(out _, out float max)) return 1f;
        return max;
    }

    private bool TryGetShaderRange(out float min, out float max)
    {
        min = 0f; max = 1f;
        if (!IsValid) return false;

        var mat = renderer.sharedMaterials[materialIndex];
        if (!mat || !mat.shader) return false;

        var shader = mat.shader;
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            if (shader.GetPropertyName(i) == propertyName &&
                shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Range)
            {
                min = shader.GetPropertyRangeLimits(i).x;
                max = shader.GetPropertyRangeLimits(i).y;
                return true;
            }
        }
        return false;
    }
#endif
}

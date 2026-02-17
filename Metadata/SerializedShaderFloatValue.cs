using System.Globalization;
using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedShaderFloatValue
{
    public float Value;
    public string Name;

    public bool IsPropertyRef => !string.IsNullOrEmpty(Name) && !Name.StartsWith('<');

    public SerializedShaderFloatValue(AssetTypeValueField field)
    {
        Value = field["val"].AsFloat;
        Name = field["name"].AsString;
    }

    public string ToShaderLab()
    {
        return IsPropertyRef ? $"[{Name}]" : Value.ToString(CultureInfo.InvariantCulture);
    }

    public string ToShaderLabInt()
    {
        return IsPropertyRef ? $"[{Name}]" : ((int)Value).ToString();
    }
}

public class SerializedShaderFloatValue<T>
    where T : Enum
{
    public T Value;
    public string Name;

    public bool IsPropertyRef => !string.IsNullOrEmpty(Name) && !Name.StartsWith('<');

    public SerializedShaderFloatValue(AssetTypeValueField field)
    {
        Value = (T)Enum.ToObject(typeof(T), (int)field["val"].AsFloat);
        Name = field["name"].AsString;
    }

    public string ToShaderLab()
    {
        return IsPropertyRef ? $"[{Name}]" : Value.ToString();
    }
}

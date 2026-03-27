using AssetsTools.NET;
using USCSandbox.Common;

namespace USCSandbox.Metadata;
public class SerializedSubProgram
{
    public List<ushort> KeywordIndices;
    public ShaderGpuProgramType GpuProgramType;
    public uint BlobIndex;
    public uint ParameterBlobIndex;

    public SerializedProgramParameters? Params;

    public bool UsesParameterBlob => ParameterBlobIndex != uint.MaxValue;

    public SerializedSubProgram(AssetTypeValueField field, Dictionary<int, string> nameTable, uint paramBlobIdx = uint.MaxValue)
    {
        KeywordIndices = field["m_KeywordIndices.Array"].Select(i => i.AsUShort).ToList();
        GpuProgramType = (ShaderGpuProgramType)(int)field["m_GpuProgramType"].AsSByte;
        BlobIndex = field["m_BlobIndex"].AsUInt;
        ParameterBlobIndex = paramBlobIdx;

        if (!field["m_VectorParam"].IsDummy)
        {
            
            Params = new SerializedProgramParameters(field, nameTable);
        }
        else if (!field["m_Parameters"].IsDummy)
        {
            
            Params = new SerializedProgramParameters(field["m_Parameters"], nameTable);
        }
        // else: no inline params and no parameter blob index — params are in the binary blob data
        // (ShaderSubProgramData.ShaderParams), which converters handle via UsesParameterBlob = false
    }
}

using AssetsTools.NET;

namespace USCSandbox.Metadata;
public class SerializedProgram
{
    public string Name;
    public List<SerializedSubProgram> SubProgramInfos;
    public SerializedProgramParameters? CommonParams;

    public SerializedProgram(AssetTypeValueField program, string fieldName, Dictionary<int, string> nameTable)
    {
        Name = fieldName;
        if (!program["m_PlayerSubPrograms"].IsDummy)
        {
            uint[]? parameterBlobIndicesArr;
            var parameterBlobIndices = program["m_ParameterBlobIndices.Array"];
            if (parameterBlobIndices.Children.Count > 0)
            {
                parameterBlobIndicesArr = SerializedMetadataHelpers.GetArrayFirstValue(parameterBlobIndices)
                    .Select(i => i.AsUInt)
                    .ToArray();
            }
            else
            {
                parameterBlobIndicesArr = null;
            }

            var subProgramInfos = program["m_PlayerSubPrograms.Array"];
            if (subProgramInfos.Children.Count > 0)
            {
                if (parameterBlobIndicesArr is not null)
                {
                    SubProgramInfos = SerializedMetadataHelpers.GetArrayFirstValue(subProgramInfos)
                        .Select((i, idx) => new SerializedSubProgram(i, nameTable, parameterBlobIndicesArr[idx]))
                        .ToList();
                }
                else
                {
                    SubProgramInfos = SerializedMetadataHelpers.GetArrayFirstValue(subProgramInfos)
                        .Select((i, idx) => new SerializedSubProgram(i, nameTable, uint.MaxValue))
                        .ToList();
                }
            }
            else
            {
                SubProgramInfos = [];
            }
        }
        else
        {
            SubProgramInfos = program["m_SubPrograms.Array"]
                .Select(i => new SerializedSubProgram(i, nameTable))
                .ToList();
        }

        if (!program["m_CommonParameters"].IsDummy)
        {
            
            CommonParams = new SerializedProgramParameters(program["m_CommonParameters"], nameTable);
        }
    }
}

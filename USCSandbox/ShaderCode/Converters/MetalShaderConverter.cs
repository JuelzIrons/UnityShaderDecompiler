using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderCode.Converters;
public static class MetalShaderConverter
{
    public static List<MetalShaderSubprogram> Convert(SerializedPass pass, BlobManager blobMan, UnityVersion version)
    {
        var subProgs = new List<MetalShaderSubprogram>();

        foreach (var program in pass.Programs)
        {
            var subProgInfs = program.SubProgramInfos
                .Where(i => IsSupportedProgram(i.GpuProgramType))
                .ToArray();

            if (subProgInfs.Length < 1)
                continue;

            var subProgInf = subProgInfs[0];

            var subProgData = blobMan.GetShaderSubProgram((int)subProgInf.BlobIndex);
            var programType = subProgData.GetProgramType(version);
            var funcType = GetFunctionType(programType);

            var mslSource = ExtractMslSource(subProgData.ProgramData);

            var shaderParams = subProgInf.UsesParameterBlob
                ? blobMan.GetShaderParams((int)subProgInf.ParameterBlobIndex)
                : subProgData.ShaderParams!;

            if (program.CommonParams is { } commonParams)
            {
                shaderParams.CombineCommon(commonParams);
            }

            var comboKeywords = subProgData.GlobalKeywords.Concat(subProgData.LocalKeywords)
                .Order()
                .ToArray();

            subProgs.Add(new MetalShaderSubprogram(mslSource, shaderParams, funcType, comboKeywords));
        }

        return subProgs;
    }

    private static string ExtractMslSource(byte[] programData)
    {
        if (programData.Length == 0)
            return "// Empty program data";

        int offset = 0;

        if (programData.Length > 0)
            offset = 1;
        for (int i = 0; i < Math.Min(programData.Length, 128); i++)
        {
            if (i + 8 < programData.Length)
            {
                var snippet = System.Text.Encoding.UTF8.GetString(programData, i, Math.Min(24, programData.Length - i));
                if (snippet.StartsWith("#include <metal_stdlib>") ||
                    snippet.StartsWith("using namespace metal") ||
                    snippet.StartsWith("vertex ") ||
                    snippet.StartsWith("fragment ") ||
                    snippet.StartsWith("kernel ") ||
                    snippet.StartsWith("//") ||
                    snippet.StartsWith("#"))
                {
                    offset = i;
                    break;
                }
            }
        }

        int end = programData.Length;
        for (int i = offset; i < programData.Length; i++)
        {
            if (programData[i] == 0)
            {
                end = i;
                break;
            }
        }

        if (end <= offset)
            return "";

        return System.Text.Encoding.UTF8.GetString(programData, offset, end - offset);
    }

    private static bool IsSupportedProgram(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.MetalVS or
            ShaderGpuProgramType.MetalFS => true,
            _ => false,
        };
    }

    private static string GetFunctionType(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.MetalVS => "MetalVS",
            ShaderGpuProgramType.MetalFS => "MetalFS",
            _ => "Unknown",
        };
    }

    public class MetalShaderSubprogram
    {
        public string MslSource;
        public ShaderParameters Parameters;
        public string FunctionType;
        public string[] Keywords;

        public MetalShaderSubprogram(string mslSource, ShaderParameters parameters, string functionType, string[] keywords)
        {
            MslSource = mslSource;
            Parameters = parameters;
            FunctionType = functionType;
            Keywords = keywords;
        }
    }
}

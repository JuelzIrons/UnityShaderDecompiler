using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderCode.Converters;
public static class Gles3ShaderConverter
{
    public static List<Gles3ShaderSubprogram> Convert(SerializedPass pass, BlobManager blobMan, UnityVersion version)
    {
        var subProgs = new List<Gles3ShaderSubprogram>();

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

            // GLES3 program data is GLSL source text with a small header
            var glslSource = ExtractGlslSource(subProgData.ProgramData);

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

            subProgs.Add(new Gles3ShaderSubprogram(glslSource, shaderParams, funcType, comboKeywords));
        }

        return subProgs;
    }

    private static string ExtractGlslSource(byte[] programData)
    {
        // GLES3 program data: first byte is header version, then GLSL source
        // The source is typically stored after a small header
        if (programData.Length == 0)
            return "// Empty program data";

        // Try to find the start of the GLSL source
        // Unity GLES3 blobs have a small header, then the GLSL text
        int offset = 0;

        // Skip the header byte
        if (programData.Length > 0)
            offset = 1;

        // Look for #version or other GLSL markers to find start of source
        for (int i = 0; i < Math.Min(programData.Length, 64); i++)
        {
            if (i + 8 < programData.Length)
            {
                var snippet = System.Text.Encoding.UTF8.GetString(programData, i, Math.Min(8, programData.Length - i));
                if (snippet.StartsWith("#version") || snippet.StartsWith("#ifdef") ||
                    snippet.StartsWith("//") || snippet.StartsWith("uniform") ||
                    snippet.StartsWith("precis"))
                {
                    offset = i;
                    break;
                }
            }
        }

        // Find the end of the source (null terminator or end of data)
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
            return "// Could not extract GLSL source";

        return System.Text.Encoding.UTF8.GetString(programData, offset, end - offset);
    }

    private static bool IsSupportedProgram(ShaderGpuProgramType progType)
    {
        return progType switch
        {
            ShaderGpuProgramType.GLES3 or
            ShaderGpuProgramType.GLES31 or
            ShaderGpuProgramType.GLES31AEP => true,
            _ => false,
        };
    }

    private static string GetFunctionType(ShaderGpuProgramType progType)
    {
        // For GLES3 we can't distinguish vertex/fragment from the program type enum alone
        // since GLES3/GLES31/GLES31AEP are shared. The actual type is in the GLSL source.
        return progType switch
        {
            ShaderGpuProgramType.GLES3 or
            ShaderGpuProgramType.GLES31 or
            ShaderGpuProgramType.GLES31AEP => "GLES3",
            _ => "Unknown",
        };
    }

    public class Gles3ShaderSubprogram
    {
        public string GlslSource;
        public ShaderParameters Parameters;
        public string FunctionType;
        public string[] Keywords;

        public Gles3ShaderSubprogram(string glslSource, ShaderParameters parameters, string functionType, string[] keywords)
        {
            GlslSource = glslSource;
            Parameters = parameters;
            FunctionType = functionType;
            Keywords = keywords;
        }
    }
}

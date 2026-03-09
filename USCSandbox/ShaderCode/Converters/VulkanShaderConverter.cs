using System.Diagnostics;
using System.Text;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.ShaderCode.Converters;
public static class VulkanShaderConverter
{
    private static string? _spirvCrossPath;

    public static List<VulkanShaderSubprogram> Convert(SerializedPass pass, BlobManager blobMan, UnityVersion version)
    {
        var subProgs = new List<VulkanShaderSubprogram>();

        foreach (var program in pass.Programs)
        {
            var subProgInfs = program.SubProgramInfos
                .Where(i => i.GpuProgramType == ShaderGpuProgramType.SPIRV)
                .ToArray();

            if (subProgInfs.Length < 1)
                continue;

            var subProgInf = subProgInfs[0];

            var subProgData = blobMan.GetShaderSubProgram((int)subProgInf.BlobIndex);

            var spirvData = ExtractSpirvData(subProgData.ProgramData);

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

            var disassembly = DecompileSpirv(spirvData);

            subProgs.Add(new VulkanShaderSubprogram(disassembly, spirvData, shaderParams, comboKeywords));
        }

        return subProgs;
    }

    private static byte[] ExtractSpirvData(byte[] programData)
    {
        if (programData.Length < 20)
            return programData;

        uint headerSize = BitConverter.ToUInt32(programData, 12);

        if (headerSize >= (uint)programData.Length || headerSize < 16)
        {
            headerSize = 0;
        }

        int dataStart = (int)headerSize;
        int dataLen = programData.Length - dataStart;
        if (dataLen < 4)
            return programData;

        var data = new byte[dataLen];
        Array.Copy(programData, dataStart, data, 0, dataLen);

        uint magic = BitConverter.ToUInt32(data, 0);
        if (magic == 0x534D4F4C)
        {
            var decoded = SmolVDecoder.Decode(data);
            if (decoded != null)
            {
                Console.WriteLine($"    SMOL-V decoded: {data.Length} -> {decoded.Length} bytes");
                return decoded;
            }
            Console.Error.WriteLine($"    SMOL-V decode failed for {data.Length} bytes");
            return data;
        }

        if (magic == 0x07230203)
            return data;

        for (int i = 0; i < programData.Length - 4; i++)
        {
            uint m = BitConverter.ToUInt32(programData, i);
            if (m == 0x07230203)
            {
                var result = new byte[programData.Length - i];
                Array.Copy(programData, i, result, 0, result.Length);
                return result;
            }
            if (m == 0x534D4F4C)
            {
                var smolData = new byte[programData.Length - i];
                Array.Copy(programData, i, smolData, 0, smolData.Length);
                var decoded = SmolVDecoder.Decode(smolData);
                if (decoded != null)
                    return decoded;
            }
        }

        return data;
    }

    private static string? FindSpirvCross()
    {
        if (_spirvCrossPath != null)
            return _spirvCrossPath == "" ? null : _spirvCrossPath;

        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(exeDir, "spirv-cross.exe"),
            Path.Combine(exeDir, "spirv-cross"),
            Path.Combine(exeDir, "bin", "spirv-cross.exe"),
            Path.Combine(exeDir, "bin", "spirv-cross"),
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                _spirvCrossPath = candidate;
                return _spirvCrossPath;
            }
        }

        try
        {
            var psi = new ProcessStartInfo("spirv-cross", "--help")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            proc?.WaitForExit(2000);
            proc?.Kill();
            _spirvCrossPath = "spirv-cross";
            return _spirvCrossPath;
        }
        catch
        {
            _spirvCrossPath = "";
            return null;
        }
    }

    private static string DecompileSpirv(byte[] spirvData)
    {
        if (spirvData.Length < 20)
            return "// SPIR-V data too short\n";

        var spirvCross = FindSpirvCross();
        if (spirvCross == null)
        {
            throw new FileNotFoundException(
                $"spirv-cross is required to decompile Vulkan shaders but was not found.\n" +
                $"Download it from: https://github.com/KhronosGroup/SPIRV-Cross/releases\n" +
                $"Place spirv-cross.exe next to the exporter executable or in a 'bin/' subfolder.");
        }

        var tempSpv = Path.GetTempFileName() + ".spv";
        try
        {
            File.WriteAllBytes(tempSpv, spirvData);

            var hlsl = RunSpirvCross(spirvCross, tempSpv, "--hlsl", "--shader-model", "50",
                "--force-zero-initialized-variables");

            if (hlsl != null)
                return hlsl;

            var glsl = RunSpirvCross(spirvCross, tempSpv);

            if (glsl != null)
                return $"// spirv-cross HLSL failed, falling back to GLSL:\n{glsl}";

            return $"// spirv-cross failed to decompile ({spirvData.Length} bytes)\n";
        }
        finally
        {
            try { File.Delete(tempSpv); } catch { }
        }
    }

    private static string? RunSpirvCross(string spirvCrossPath, string spvFile, params string[] extraArgs)
    {
        try
        {
            var args = new List<string> { spvFile };
            args.AddRange(extraArgs);

            var psi = new ProcessStartInfo(spirvCrossPath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            var proc = Process.Start(psi);
            if (proc == null)
                return null;

            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(30000);

            if (proc.ExitCode != 0)
            {
                Console.Error.WriteLine($"    spirv-cross error: {stderr.Trim()}");
                return null;
            }

            return stdout;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"    spirv-cross exception: {ex.Message}");
            return null;
        }
    }

    public class VulkanShaderSubprogram
    {
        public string Disassembly;
        public byte[] SpirvData;
        public ShaderParameters Parameters;
        public string[] Keywords;

        public VulkanShaderSubprogram(string disassembly, byte[] spirvData, ShaderParameters parameters, string[] keywords)
        {
            Disassembly = disassembly;
            SpirvData = spirvData;
            Parameters = parameters;
            Keywords = keywords;
        }
    }
}

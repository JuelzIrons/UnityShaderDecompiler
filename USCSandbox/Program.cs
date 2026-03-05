using AssetsTools.NET;
using AssetsTools.NET.Extra;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.Processor;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox;
internal class Program
{
    static void Main(string[] args)
    {
        GPUPlatform platform = GPUPlatform.d3d11;
        UnityVersion? ver = null;
        bool allSet = false;
        string? batchDir = null;
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string outDir = Path.Combine(exeDir, "Shaders");

        List<string> argList = [];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                switch (arg)
                {
                    case "--platform":
                        platform = Enum.Parse<GPUPlatform>(args[++i]);
                        break;
                    case "--version":
                        ver = UnityVersion.Parse(args[++i]);
                        break;
                    case "--all":
                        allSet = true;
                        break;
                    case "--dir":
                        batchDir = args[++i];
                        break;
                    case "--out":
                        outDir = args[++i];
                        break;
                    default:
                        Console.WriteLine($"Optional argmuent {arg} is invalid.");
                        return;
                }
            }
            else
            {
                argList.Add(arg);
            }
        }

        // Interactive mode: no args, just double-clicked the exe
        if (args.Length == 0)
        {
            Console.WriteLine("=== USCS Shader Exporter ===");
            Console.WriteLine();

            Console.Write("Enter the path to the folder containing asset bundles: ");
            batchDir = Console.ReadLine()?.Trim().Trim('"');

            if (string.IsNullOrEmpty(batchDir))
            {
                Console.WriteLine("No path entered.");
                WaitForExit();
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Platform options: gles3, vulkan, d3d11, Switch");
            Console.Write("Enter platform (or press Enter for gles3): ");
            platform = GPUPlatform.gles3;
            var platInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(platInput))
            {
                if (!Enum.TryParse<GPUPlatform>(platInput, true, out platform))
                {
                    Console.WriteLine($"Invalid platform: {platInput}");
                    WaitForExit();
                    return;
                }
            }

            Console.WriteLine();
            ExportAllFromDirectory(batchDir, outDir, platform, ver);
            WaitForExit();
            return;
        }

        if (batchDir != null)
        {
            ExportAllFromDirectory(batchDir, outDir, platform, ver);
            return;
        }

        if (argList.Count == 0)
        {
            Console.WriteLine("USCS [bundle path] [assets path] [shader path id] <--platform> <--version> <--all>");
            Console.WriteLine("  [bundle path (or \"null\" for no bundle)]");
            Console.WriteLine("  [assets path (or file name in bundle)]");
            Console.WriteLine("  [shader path id (or --all to load all shaders)]");
            Console.WriteLine("  --platform <[d3d11, Switch] (or skip this arg for d3d11)>");
            Console.WriteLine("  --version <unity version override>");
            Console.WriteLine("  --dir <directory> : export all shaders from all asset bundles in a directory");
            Console.WriteLine("  --out <directory> : output directory for exported shaders (default: ./Shaders)");
            Console.WriteLine();
            Console.WriteLine("Or just run the exe with no arguments for interactive mode.");
            return;
        }

        var manager = new AssetsManager();
        AssetsFileInstance afileInst;

        var bundlePath = argList[0];
        if (argList.Count == 1)
        {
            var bundleFile = manager.LoadBundleFile(bundlePath, true);
            var dirInfs = bundleFile.file.BlockAndDirInfo.DirectoryInfos;
            Console.WriteLine("Available files in bundle:");
            foreach (var dirInf in dirInfs)
            {
                if ((dirInf.Flags & 4) == 0)
                    continue;

                Console.WriteLine($"  {dirInf.Name}");
            }
            return;
        }

        var assetsFileName = argList[1];
        if (argList.Count == 2 && !allSet)
        {
            if (bundlePath != "null")
            {
                var bundleFile = manager.LoadBundleFile(bundlePath, true);
                afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(bundleFile.file.Header.EngineVersion);

                Console.WriteLine("Available shaders in bundle:");
            }
            else
            {
                afileInst = manager.LoadAssetsFile(assetsFileName);

                manager.LoadClassPackage("classdata.tpk");
                manager.LoadClassDatabaseFromPackage(afileInst.file.Metadata.UnityVersion);

                Console.WriteLine("Available shaders in assets file:");
            }

            foreach (var shaderInf in afileInst.file.GetAssetsOfType(AssetClassID.Shader))
            {
                var tmpShaderBf = manager.GetBaseField(afileInst, shaderInf);
                var tmpShaderName = tmpShaderBf["m_ParsedForm"]["m_Name"].AsString;
                Console.WriteLine($"  {tmpShaderName} (path id {shaderInf.PathId})");
            }
            return;
        }

        long shaderPathId = 0;
        if (argList.Count > 2)
            shaderPathId = long.Parse(argList[2]);

        Dictionary<long, string> files = [];
        if (bundlePath != "null")
        {
            var bundleFile = manager.LoadBundleFile(bundlePath, true);
            afileInst = manager.LoadAssetsFileFromBundle(bundleFile, assetsFileName);

            if (ver is null)
            {
                var verStr = bundleFile.file.Header.EngineVersion;
                if (verStr != "0.0.0")
                {
                    var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                    ver = UnityVersion.Parse(fixedVerStr);
                }
            }
        }
        else
        {
            afileInst = manager.LoadAssetsFile(assetsFileName);

            if (ver is null)
            {
                var verStr = afileInst.file.Metadata.UnityVersion;
                if (verStr != "0.0.0")
                {
                    var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                    ver = UnityVersion.Parse(fixedVerStr);
                }
            }
        }

        if (ver is null)
        {
            Console.WriteLine("File version was stripped. Please set --version flag.");
            return;
        }

        manager.LoadClassPackage("classdata.tpk");
        manager.LoadClassDatabaseFromPackage(ver.ToString());

        var shadersToLoad = new List<AssetFileInfo>();
        if (shaderPathId != 0)
            shadersToLoad.Add(afileInst.file.GetAssetInfo(shaderPathId));
        else
            shadersToLoad.AddRange(afileInst.file.GetAssetsOfType(AssetClassID.Shader));

        foreach (var shaderInf in shadersToLoad)
        {
            var shaderBf = manager.GetBaseField(afileInst, shaderInf);
            if (shaderBf == null)
            {
                Console.WriteLine("Shader asset not found or couldn't be read.");
                return;
            }

            var shaderName = shaderBf["m_ParsedForm"]["m_Name"].AsString;

            var shaderTextWriter = new ShaderTextWriter(shaderBf, ver.Value);
            var output = shaderTextWriter.LoadAndWrite(platform);
            Console.WriteLine(output);

            Console.WriteLine($"{shaderName} decompiled");
        }
    }

    static void ExportAllFromDirectory(string directory, string outDir, GPUPlatform platform, UnityVersion? versionOverride)
    {
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"Directory not found: {directory}");
            return;
        }

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
        int totalShaders = 0;
        int totalFiles = 0;
        int failedShaders = 0;

        Console.WriteLine($"Scanning directory: {directory}");
        Console.WriteLine($"Output directory: {outDir}");
        Console.WriteLine();

        foreach (var filePath in files)
        {
            // Skip .resource files and other non-asset files
            if (filePath.EndsWith(".resource", StringComparison.OrdinalIgnoreCase) ||
                filePath.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
                continue;

            var fileName = Path.GetFileName(filePath);
            var fileType = DetectFileType(filePath);

            if (fileType == UnityFileType.Bundle)
            {
                var manager = new AssetsManager();
                BundleFileInstance bundleFile;
                try
                {
                    bundleFile = manager.LoadBundleFile(filePath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Skipping {fileName}: failed to load bundle ({ex.Message})");
                    continue;
                }

                var dirInfs = bundleFile.file.BlockAndDirInfo.DirectoryInfos;

                foreach (var dirInf in dirInfs)
                {
                    if ((dirInf.Flags & 4) == 0)
                        continue;

                    AssetsFileInstance afileInst;
                    try
                    {
                        afileInst = manager.LoadAssetsFileFromBundle(bundleFile, dirInf.Name);
                    }
                    catch
                    {
                        continue;
                    }

                    UnityVersion? ver = versionOverride;
                    if (ver is null)
                    {
                        var verStr = bundleFile.file.Header.EngineVersion;
                        if (verStr != "0.0.0")
                        {
                            try
                            {
                                var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                                ver = UnityVersion.Parse(fixedVerStr);
                            }
                            catch { continue; }
                        }
                    }

                    if (ver is null)
                    {
                        Console.WriteLine($"  Skipping {fileName}/{dirInf.Name}: version stripped (use --version)");
                        continue;
                    }

                    int count = ExportShadersFromAssetsFile(manager, afileInst, ver.Value, platform, outDir, fileName, ref failedShaders);
                    if (count > 0)
                    {
                        totalFiles++;
                        totalShaders += count;
                    }
                }

                manager.UnloadAll();
            }
            else if (fileType == UnityFileType.Assets)
            {
                var manager = new AssetsManager();
                AssetsFileInstance afileInst;
                try
                {
                    afileInst = manager.LoadAssetsFile(filePath);
                }
                catch
                {
                    continue;
                }

                UnityVersion? ver = versionOverride;
                if (ver is null)
                {
                    try
                    {
                        var verStr = afileInst.file.Metadata.UnityVersion;
                        if (verStr != "0.0.0" && !string.IsNullOrEmpty(verStr))
                        {
                            var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                            ver = UnityVersion.Parse(fixedVerStr);
                        }
                    }
                    catch { }
                }

                if (ver is null)
                {
                    // Try reading version from globalgamemanagers in the same directory
                    ver = TryGetVersionFromDirectory(directory, versionOverride);
                }

                if (ver is null)
                {
                    continue;
                }

                int count = ExportShadersFromAssetsFile(manager, afileInst, ver.Value, platform, outDir, fileName, ref failedShaders);
                if (count > 0)
                {
                    totalFiles++;
                    totalShaders += count;
                }

                manager.UnloadAll();
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done. Exported {totalShaders} shader(s) from {totalFiles} file(s) to {outDir}");
        if (failedShaders > 0)
            Console.WriteLine($"  ({failedShaders} shader(s) failed)");
    }

    static int ExportShadersFromAssetsFile(AssetsManager manager, AssetsFileInstance afileInst, UnityVersion ver, GPUPlatform platform, string outDir, string sourceName, ref int failedShaders)
    {
        try
        {
            manager.LoadClassPackage("classdata.tpk");
            manager.LoadClassDatabaseFromPackage(ver.ToString());
        }
        catch
        {
            return 0;
        }

        IEnumerable<AssetFileInfo> shaderAssets;
        try
        {
            shaderAssets = afileInst.file.GetAssetsOfType(AssetClassID.Shader);
            if (!shaderAssets.Any())
                return 0;
        }
        catch
        {
            return 0;
        }

        int count = shaderAssets.Count();
        Console.WriteLine($"[{sourceName}] Found {count} shader(s)");

        int exported = 0;
        foreach (var shaderInf in shaderAssets)
        {
            try
            {
                var shaderBf = manager.GetBaseField(afileInst, shaderInf);
                if (shaderBf == null)
                    continue;

                var shaderName = shaderBf["m_ParsedForm"]["m_Name"].AsString;

                // Auto-detect platform if the requested one isn't available
                var actualPlatform = platform;
                var shaderPlatforms = shaderBf["platforms.Array"]
                    .Select(i => (GPUPlatform)i.AsInt).ToList();

                if (!shaderPlatforms.Contains(platform) && shaderPlatforms.Count > 0)
                {
                    // Prefer supported platforms: gles3, vulkan, d3d11, Switch
                    GPUPlatform[] preferred = [GPUPlatform.gles3, GPUPlatform.vulkan, GPUPlatform.d3d11, GPUPlatform.Switch];
                    actualPlatform = preferred.FirstOrDefault(p => shaderPlatforms.Contains(p), shaderPlatforms[0]);
                }

                var shaderTextWriter = new ShaderTextWriter(shaderBf, ver);
                var output = shaderTextWriter.LoadAndWrite(actualPlatform);

                var safeName = SanitizeFileName(shaderName);
                var shaderOutDir = Path.Combine(outDir, Path.GetDirectoryName(safeName) ?? "");
                Directory.CreateDirectory(shaderOutDir);

                var shaderOutPath = Path.Combine(outDir, safeName + ".shader");
                File.WriteAllText(shaderOutPath, output);

                exported++;
                Console.WriteLine($"  Exported: {shaderName}");
            }
            catch (Exception ex)
            {
                failedShaders++;
                Console.WriteLine($"  Failed to export shader (path id {shaderInf.PathId}): {ex.Message}");
            }
        }

        return exported;
    }

    static UnityVersion? TryGetVersionFromDirectory(string directory, UnityVersion? versionOverride)
    {
        if (versionOverride != null)
            return versionOverride;

        // Try globalgamemanagers first, then any .assets file
        var ggmPath = Path.Combine(directory, "globalgamemanagers");
        string[] candidates = File.Exists(ggmPath)
            ? [ggmPath, ..Directory.GetFiles(directory, "*.assets")]
            : Directory.GetFiles(directory, "*.assets");

        foreach (var candidate in candidates)
        {
            try
            {
                var tmpManager = new AssetsManager();
                var tmpInst = tmpManager.LoadAssetsFile(candidate);
                var verStr = tmpInst.file.Metadata.UnityVersion;
                tmpManager.UnloadAll();

                if (verStr != "0.0.0" && !string.IsNullOrEmpty(verStr))
                {
                    var fixedVerStr = new AssetsTools.NET.Extra.UnityVersion(verStr).ToString();
                    return UnityVersion.Parse(fixedVerStr);
                }
            }
            catch { }
        }

        return null;
    }

    static void WaitForExit()
    {
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(true);
    }

    enum UnityFileType { Unknown, Bundle, Assets }

    static UnityFileType DetectFileType(string filePath)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            if (fs.Length < 16)
                return UnityFileType.Unknown;

            var buf = new byte[16];
            fs.Read(buf, 0, 16);
            var sig = System.Text.Encoding.ASCII.GetString(buf, 0, 8);

            // Unity asset bundle signatures
            if (sig.StartsWith("UnityFS") || sig.StartsWith("UnityWeb") || sig.StartsWith("UnityRaw"))
                return UnityFileType.Bundle;

            // Serialized asset file detection:
            // First 4 bytes are big-endian metadata size, bytes 8-11 are big-endian file size or zero,
            // bytes 12-15 are big-endian data offset. Check for version number at bytes 4-7.
            // Version 0x16 (22) is common for Unity 2020+, 0x15 (21) for Unity 2019, etc.
            int version = (buf[4] << 24) | (buf[5] << 16) | (buf[6] << 8) | buf[7];
            if (version >= 9 && version <= 50)
            {
                // Likely a serialized file
                return UnityFileType.Assets;
            }

            // Also detect by known file extensions
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".assets")
                return UnityFileType.Assets;

            // Hash-named files (32+ hex chars, no extension) are often serialized assets
            var name = Path.GetFileName(filePath);
            if (name.Length >= 32 && !name.Contains('.') && name.All(c => "0123456789abcdef".Contains(c)))
                return UnityFileType.Assets;

            return UnityFileType.Unknown;
        }
        catch
        {
            return UnityFileType.Unknown;
        }
    }

    static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        // Preserve path separators by only sanitizing each segment
        var parts = name.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            foreach (var c in invalid)
                parts[i] = parts[i].Replace(c, '_');
        }
        return Path.Combine(parts);
    }
}
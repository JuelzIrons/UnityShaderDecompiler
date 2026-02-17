# UnityShaderDecompiler

A fork of [nesrak1/USCSandbox (rework branch)](https://github.com/nesrak1/USCSandbox/tree/rework)

The only reason this exists is because im reverse engineering a android game and couldnt get the shader accurate to the real game 

## INFO

Supported shader backends:
- **Vulkan (SPIR-V)** - Primary target, reason I forked this.
- **DirectX (DXBC/DXIL)** - decently well supported
- **OpenGL ES 3 (GLES3)** - supported
- **Switch NVN** - work in progress, not fully functional

## How to use

### Interactive mode (recommended)

Just run the executable by double-clicking it or launching it from a terminal with no arguments. You will be prompted to enter the path to a folder containing asset bundles, then choose a platform. The tool will scan the entire directory recursively, decompile every shader found, and write the results to a `Shaders/` folder next to the executable.

### Command line

```
USCSandbox.exe --dir <bundle directory> [--out <output directory>] [--platform <platform>] [--version <unity version>]
```

**Arguments:**
- `[bundle path]` - path to a `.bundle` file, or `null` for a bare `.assets` file
- `[assets path]` - name of the assets file inside the bundle (e.g. `CAB-abcdef0123456789`)
- `[shader path id]` - path ID of a specific shader asset
- `--all` - decompile all shaders instead of a single one
- `--platform` - target GPU platform: `d3d11`, `gles3`, `vulkan`, `Switch` (default: `gles3` in interactive, `d3d11` in CLI)
- `--version` - override the Unity version (required if the bundle has a stripped version)
- `--dir` - recursively scan a directory and decompile all shaders from all asset bundles found
- `--out` - output directory for exported shaders (default: `./Shaders`)

## How it works

A Unity shader asset has two major parts: serialized metadata (the `m_ParsedForm` field) and the compiled shader blob (`compressedBlob`). The metadata describes shader properties, passes, subshaders, render state, and constant buffer layouts. The blob contains the platform-specific compiled shader code.

The decompiler pairs up metadata and blob data into "shader baskets", then converts the platform bytecode into USIL (Ultra Shader Intermediate Language) using a per-platform converter (`DirectXProgramToUSIL`, `NvnProgramToUSIL`, etc.). USIL is then processed in three passes:

- **Fixers** - required corrections for accurate decompilation
- **Metadders** - inject metadata from outside the native shader format (constant buffer names, input/output semantics, etc.)
- **Optimizers** - optional cleanup for readability (loop reconstruction, matrix multiply detection, etc.)

Finally, `UShaderFunctionToHLSL` converts the processed USIL to an HLSL function body, and the metadata parsers reconstruct the surrounding ShaderLab structure.

---
Licence: GPLv3


## Credits

- **Juelz Irons** - Implementing Vulkan (SPIR-V) support
- **nesrak1** - original USCSandbox and USC rework
- **ds5678** - AssetRipper stuff
- **uTinyRipper** - base Unity shader parsing
- **3dmigoto contributors** - DirectX shader disassembler foundation
- **Ryujinx contributors** - NVN/Switch shader translation via Ryujinx.Graphics.Shader

*Claude AI was used to assist in speeding up development of this fork*

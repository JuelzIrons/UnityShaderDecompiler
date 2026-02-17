using AssetsTools.NET;
using System.Globalization;
using System.Text;
using USCSandbox.Common;
using USCSandbox.Metadata;
using USCSandbox.ShaderCode.Converters;
using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace USCSandbox.Processor;
public class ShaderTextWriter
{
    private readonly StringBuilderIndented _sb;
    private readonly SerializedShader _shader;
    private readonly UnityVersion _engVer;

    public ShaderTextWriter(AssetTypeValueField shaderBf, UnityVersion engVer)
    {
        _sb = new StringBuilderIndented();
        _shader = new SerializedShader(shaderBf, engVer);
        _engVer = engVer;
    }

    public string LoadAndWrite(GPUPlatform platformId)
    {
        _sb.Clear();

        var blobMan = _shader.MakeBlobManager(platformId)
            ?? throw new Exception($"{nameof(_shader.MakeBlobManager)} returned null. Is this platform used?");

        _sb.AppendLine($"Shader \"{_shader.Name}\" {{");
        _sb.Indent();
        {
            WriteProperties(_shader.PropsField);
            foreach (var subShader in _shader.SubShaders)
            {
                WriteSubShader(subShader, blobMan, platformId);
            }

            if (!string.IsNullOrEmpty(_shader.FallbackName))
                _sb.AppendLine($"Fallback \"{_shader.FallbackName}\"");
        }
        _sb.Unindent();
        _sb.AppendLine("}");

        return _sb.ToString();
    }

    private void WriteProperties(AssetTypeValueField props)
    {
        // todo: convert this to an object (maybe?)
        _sb.AppendLine("Properties {");
        _sb.Indent();
        foreach (var prop in props)
        {
            _sb.Append("");

            var attributes = prop["m_Attributes.Array"];
            foreach (var attribute in attributes)
            {
                _sb.AppendNoIndent($"[{attribute.AsString}] ");
            }

            var flags = (SerializedPropertyFlag)prop["m_Flags"].AsUInt;
            if (flags.HasFlag(SerializedPropertyFlag.HideInInspector))
                _sb.AppendNoIndent("[HideInInspector] ");
            if (flags.HasFlag(SerializedPropertyFlag.PerRendererData))
                _sb.AppendNoIndent("[PerRendererData] ");
            if (flags.HasFlag(SerializedPropertyFlag.NoScaleOffset))
                _sb.AppendNoIndent("[NoScaleOffset] ");
            if (flags.HasFlag(SerializedPropertyFlag.Normal))
                _sb.AppendNoIndent("[Normal] ");
            if (flags.HasFlag(SerializedPropertyFlag.HDR))
                _sb.AppendNoIndent("[HDR] ");
            if (flags.HasFlag(SerializedPropertyFlag.Gamma))
                _sb.AppendNoIndent("[Gamma] ");
            // any more?

            var name = prop["m_Name"].AsString;
            var description = prop["m_Description"].AsString;
            var type = (SerializedPropertyType)prop["m_Type"].AsInt;
            var defValues = new string[]
            {
                    prop["m_DefValue[0]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[1]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[2]"].AsFloat.ToString(CultureInfo.InvariantCulture),
                    prop["m_DefValue[3]"].AsFloat.ToString(CultureInfo.InvariantCulture)
            };
            var defTextureName = prop["m_DefTexture.m_DefaultName"].AsString;
            var defTextureDim = prop["m_DefTexture.m_TexDim"].AsInt;

            var typeName = type switch
            {
                SerializedPropertyType.Color => "Color",
                SerializedPropertyType.Vector => "Vector",
                SerializedPropertyType.Float => "Float",
                SerializedPropertyType.Range => $"Range({defValues[1]}, {defValues[2]})",
                SerializedPropertyType.Texture => defTextureDim switch
                {
                    1 => "any",
                    2 => "2D",
                    3 => "3D",
                    4 => "Cube",
                    5 => "2DArray",
                    6 => "CubeArray",
                    _ => throw new NotSupportedException("Bad texture dim")
                },
                SerializedPropertyType.Int => "Int",
                _ => throw new NotSupportedException("Bad property type")
            };

            var value = type switch
            {
                SerializedPropertyType.Color or
                SerializedPropertyType.Vector => $"({defValues[0]}, {defValues[1]}, {defValues[2]}, {defValues[3]})",
                SerializedPropertyType.Float or
                SerializedPropertyType.Range or
                SerializedPropertyType.Int => defValues[0],
                SerializedPropertyType.Texture => $"\"{defTextureName}\" {{}}",
                _ => throw new NotSupportedException("Bad property type")
            };

            _sb.AppendNoIndent($"{name} (\"{description}\", {typeName}) = {value}\n");
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WriteSubShader(SerializedSubShader subShader, BlobManager blobMan, GPUPlatform platformId)
    {
        _sb.AppendLine("SubShader {");
        _sb.Indent();
        {
            var tags = subShader.Tags;
            if (tags.Count > 0)
            {
                _sb.AppendLine("Tags {");
                _sb.Indent();
                {
                    foreach (var tag in tags)
                    {
                        _sb.AppendLine($"\"{tag.Key}\"=\"{tag.Value}\"");
                    }
                }
                _sb.Unindent();
                _sb.AppendLine("}");
            }

            var lod = subShader.LOD;
            if (lod != 0)
            {
                _sb.AppendLine($"LOD {lod}");
            }

            foreach (var pass in subShader.Passes)
            {
                WritePass(pass, blobMan, platformId);
            }
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WritePass(SerializedPass pass, BlobManager blobMan, GPUPlatform platformId)
    {
        var usePassName = pass.UseName;
        if (!string.IsNullOrEmpty(usePassName))
        {
            _sb.AppendLine($"UsePass \"{usePassName}\"");
            return;
        }

        _sb.AppendLine("Pass {");
        _sb.Indent();
        {
            WritePassState(pass.State);
            _sb.AppendLine("");

            void WriteParams(ShaderParameters shaderParams)
            {
                if (shaderParams != null)
                {
                    //_sb.Append(new string(' ', _sb.GetIndent() * 4));
                    _sb.AppendLine($"// CBs for {platformId}");

                    foreach (ConstantBuffer cbuffer in shaderParams.ConstantBuffers)
                    {
                        _sb.AppendNoIndent(WritePassCBuffer(shaderParams, [], cbuffer, _sb.GetIndent()));
                    }

                    //_sb.Append(new string(' ', _sb.GetIndent() * 4));
                    _sb.AppendLine($"// Textures for {platformId}");

                    _sb.AppendNoIndent(WritePassTextures(shaderParams, [], _sb.GetIndent()));
                }
            }

            if (platformId == GPUPlatform.d3d11)
            {
                var dx11SubPrograms = Dx11ShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in dx11SubPrograms)
                {
                    WriteParams(subProg.Parameters);

                    var hlslConv = new UShaderFunctionToHlsl(subProg.UShaderProg, _sb.GetIndent());
                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendNoIndent(hlslConv.WriteStruct());
                    _sb.AppendLine("");
                    _sb.AppendNoIndent(hlslConv.WriteFunction());
                    _sb.AppendLine("");
                }
            }
            else if (platformId == GPUPlatform.Switch)
            {
                var nvnSubprograms = NvnShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in nvnSubprograms)
                {
                    WriteParams(subProg.Parameters);

                    var hlslConv = new UShaderFunctionToHlsl(subProg.UShaderProg, _sb.GetIndent());
                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendNoIndent(hlslConv.WriteStruct());
                    _sb.AppendLine("");
                    _sb.AppendNoIndent(hlslConv.WriteFunction());
                    _sb.AppendLine("");
                }
            }
            else if (platformId == GPUPlatform.gles3)
            {
                var glesSubPrograms = Gles3ShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in glesSubPrograms)
                {
                    WriteParams(subProg.Parameters);

                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendLine($"// Platform: {subProg.FunctionType}");
                    _sb.AppendLine("/* GLSL Source:");
                    _sb.AppendNoIndent(subProg.GlslSource);
                    _sb.AppendLine("");
                    _sb.AppendLine("*/");
                    _sb.AppendLine("");
                }
            }
            else if (platformId == GPUPlatform.vulkan)
            {
                var vulkanSubPrograms = VulkanShaderConverter.Convert(pass, blobMan, _engVer);
                foreach (var subProg in vulkanSubPrograms)
                {
                    WriteParams(subProg.Parameters);

                    _sb.AppendLine($"// Keywords: {string.Join(" && ", subProg.Keywords)}");
                    _sb.AppendNoIndent(subProg.Disassembly);
                    _sb.AppendLine("");
                }
            }

            //


            // skipping other programs at this time
            //SerializedProgram vertInfo, fragInfo;
            //foreach (var prog in pass.Programs)
            //{
            //    if (prog.Name == "progVertex")
            //        vertInfo = prog;
            //    else if (prog.Name == "progFragment")
            //        fragInfo = prog;
            //}

            //var vertProgInfos = vertInfo.GetForPlatform((int)GetVertexProgramForPlatform(_platformId));
            //var fragProgInfos = fragInfo.GetForPlatform((int)GetFragmentProgramForPlatform(_platformId));

            //// we should hopefully only have one of each type, but just in case...
            //// todo: cleanup
            //List<ShaderProgramBasket> baskets = [];
            //for (var i = 0; i < vertProgInfos.Count; i++)
            //{
            //    baskets.Add(new ShaderProgramBasket(vertInfo, vertProgInfos[i],
            //        vertInfo.ParameterBlobIndices.Count > 0 ? (int)vertInfo.ParameterBlobIndices[i] : -1));
            //}
            //for (var i = 0; i < fragProgInfos.Count; i++)
            //{
            //    baskets.Add(new ShaderProgramBasket(fragInfo, fragProgInfos[i],
            //        fragInfo.ParameterBlobIndices.Count > 0 ? (int)fragInfo.ParameterBlobIndices[i] : -1));
            //}
            //if (baskets.Count > 0)
            //    WritePassBody(blobManager, baskets, _sb.GetIndent());
        }
        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WritePassState(SerializedShaderState state)
    {
        var name = state.Name;
        _sb.AppendLine($"Name \"{name}\"");

        var lod = state.LOD;
        if (lod != 0)
        {
            _sb.AppendLine($"LOD {lod}");
        }

        for (var i = 0; i < state.RtBlendState.Count; i++)
        {
            var index = state.RtBlendState.Count == 1 ? -1 : i;
            WritePassRtBlend(state.RtBlendState[i], index);
        }

        // AlphaToMask
        if (state.AlphaToMask.IsPropertyRef || state.AlphaToMask.Value > 0f)
        {
            _sb.AppendLine(state.AlphaToMask.IsPropertyRef
                ? $"AlphaToMask {state.AlphaToMask.ToShaderLab()}"
                : "AlphaToMask On");
        }

        // ZClip - default is On, only emit when Off or property ref
        if (state.ZClip.IsPropertyRef || state.ZClip.Value != ZClip.On)
        {
            _sb.AppendLine($"ZClip {state.ZClip.ToShaderLab()}");
        }

        // ZTest - default is LEqual, skip if LEqual/None and no property ref
        if (state.ZTest.IsPropertyRef
            || (state.ZTest.Value != ZTest.None && state.ZTest.Value != ZTest.LEqual))
        {
            _sb.AppendLine($"ZTest {state.ZTest.ToShaderLab()}");
        }

        // ZWrite - default is On
        if (state.ZWrite.IsPropertyRef || state.ZWrite.Value != ZWrite.On)
        {
            _sb.AppendLine($"ZWrite {state.ZWrite.ToShaderLab()}");
        }

        // Cull - default is Back
        if (state.Culling.IsPropertyRef || state.Culling.Value != CullMode.Back)
        {
            _sb.AppendLine($"Cull {state.Culling.ToShaderLab()}");
        }

        // Offset
        if (state.OffsetFactor.IsPropertyRef || state.OffsetUnits.IsPropertyRef
            || state.OffsetFactor.Value != 0f || state.OffsetUnits.Value != 0f)
        {
            _sb.AppendLine($"Offset {state.OffsetFactor.ToShaderLab()}, {state.OffsetUnits.ToShaderLab()}");
        }

        // Stencil
        WritePassStencil(state);

        // Fog
        var fogMode = state.FogMode;
        var fogColorX = state.FogColor.X.Value;
        var fogColorY = state.FogColor.Y.Value;
        var fogColorZ = state.FogColor.Z.Value;
        var fogColorW = state.FogColor.W.Value;
        var fogDensity = state.FogDensity.Value;
        var fogStart = state.FogStart.Value;
        var fogEnd = state.FogEnd.Value;

        if (fogMode != FogMode.Unknown || fogDensity != 0.0 || fogStart != 0.0 || fogEnd != 0.0
            || !(fogColorX == 0.0 && fogColorY == 0.0 && fogColorZ == 0.0 && fogColorW == 0.0))
        {
            _sb.AppendLine("Fog {");
            _sb.Indent();
            if (fogMode != FogMode.Unknown)
            {
                _sb.AppendLine($"Mode {fogMode}");
            }
            if (fogColorX != 0.0 || fogColorY != 0.0 || fogColorZ != 0.0 || fogColorW != 0.0)
            {
                _sb.AppendLine($"Color ({fogColorX.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorY.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorZ.ToString(CultureInfo.InvariantCulture)}," +
                               $"{fogColorW.ToString(CultureInfo.InvariantCulture)})");
            }
            if (fogDensity != 0.0)
            {
                _sb.AppendLine($"Density {fogDensity.ToString(CultureInfo.InvariantCulture)}");
            }
            if (fogStart != 0.0 || fogEnd != 0.0)
            {
                _sb.AppendLine($"Range {fogStart.ToString(CultureInfo.InvariantCulture)}, " +
                               $"{fogEnd.ToString(CultureInfo.InvariantCulture)}");
            }
            _sb.Unindent();
            _sb.AppendLine("}");
        }

        if (state.Lighting)
        {
            _sb.AppendLine("Lighting On");
        }

        var tags = state.Tags;
        if (tags.Count > 0)
        {
            _sb.AppendLine("Tags {");
            _sb.Indent();
            {
                foreach (var tag in tags)
                {
                    _sb.AppendLine($"\"{tag.Key}\"=\"{tag.Value}\"");
                }
            }
            _sb.Unindent();
            _sb.AppendLine("}");
        }
    }

    private bool HasAnyPropertyRef(params SerializedShaderFloatValue[] values)
    {
        foreach (var v in values)
            if (v.IsPropertyRef) return true;
        return false;
    }

    private bool HasAnyPropertyRef<T>(params SerializedShaderFloatValue<T>[] values) where T : Enum
    {
        foreach (var v in values)
            if (v.IsPropertyRef) return true;
        return false;
    }

    private void WritePassStencil(SerializedShaderState state)
    {
        var stencilRef = state.StencilRef;
        var stencilReadMask = state.StencilReadMask;
        var stencilWriteMask = state.StencilWriteMask;
        var stOp = state.StencilOp;
        var stFront = state.StencilOpFront;
        var stBack = state.StencilOpBack;

        bool hasAnyRef = HasAnyPropertyRef(stencilRef, stencilReadMask, stencilWriteMask)
            || HasAnyPropertyRef(stOp.Pass, stOp.Fail, stOp.ZFail)
            || HasAnyPropertyRef(stOp.Comp)
            || HasAnyPropertyRef(stFront.Pass, stFront.Fail, stFront.ZFail)
            || HasAnyPropertyRef(stFront.Comp)
            || HasAnyPropertyRef(stBack.Pass, stBack.Fail, stBack.ZFail)
            || HasAnyPropertyRef(stBack.Comp);

        bool hasNonDefaultValues =
            stencilRef.Value != 0.0 || stencilReadMask.Value != 255.0 || stencilWriteMask.Value != 255.0
            || !(stOp.Pass.Value == StencilOp.Keep && stOp.Fail.Value == StencilOp.Keep && stOp.ZFail.Value == StencilOp.Keep && stOp.Comp.Value == StencilComp.Always)
            || !(stFront.Pass.Value == StencilOp.Keep && stFront.Fail.Value == StencilOp.Keep && stFront.ZFail.Value == StencilOp.Keep && stFront.Comp.Value == StencilComp.Always)
            || !(stBack.Pass.Value == StencilOp.Keep && stBack.Fail.Value == StencilOp.Keep && stBack.ZFail.Value == StencilOp.Keep && stBack.Comp.Value == StencilComp.Always);

        if (!hasAnyRef && !hasNonDefaultValues)
            return;

        _sb.AppendLine("Stencil {");
        _sb.Indent();

        if (stencilRef.IsPropertyRef || stencilRef.Value != 0.0)
            _sb.AppendLine($"Ref {stencilRef.ToShaderLabInt()}");

        if (stencilReadMask.IsPropertyRef || stencilReadMask.Value != 255.0)
            _sb.AppendLine($"ReadMask {stencilReadMask.ToShaderLabInt()}");

        if (stencilWriteMask.IsPropertyRef || stencilWriteMask.Value != 255.0)
            _sb.AppendLine($"WriteMask {stencilWriteMask.ToShaderLabInt()}");

        // Generic stencil ops
        if (HasAnyPropertyRef(stOp.Pass, stOp.Fail, stOp.ZFail) || HasAnyPropertyRef(stOp.Comp)
            || stOp.Pass.Value != StencilOp.Keep || stOp.Fail.Value != StencilOp.Keep
            || stOp.ZFail.Value != StencilOp.Keep
            || (stOp.Comp.Value != StencilComp.Always && stOp.Comp.Value != StencilComp.Disabled))
        {
            _sb.AppendLine($"Comp {stOp.Comp.ToShaderLab()}");
            _sb.AppendLine($"Pass {stOp.Pass.ToShaderLab()}");
            _sb.AppendLine($"Fail {stOp.Fail.ToShaderLab()}");
            _sb.AppendLine($"ZFail {stOp.ZFail.ToShaderLab()}");
        }

        // Front face stencil ops
        if (HasAnyPropertyRef(stFront.Pass, stFront.Fail, stFront.ZFail) || HasAnyPropertyRef(stFront.Comp)
            || stFront.Pass.Value != StencilOp.Keep || stFront.Fail.Value != StencilOp.Keep
            || stFront.ZFail.Value != StencilOp.Keep
            || (stFront.Comp.Value != StencilComp.Always && stFront.Comp.Value != StencilComp.Disabled))
        {
            _sb.AppendLine($"CompFront {stFront.Comp.ToShaderLab()}");
            _sb.AppendLine($"PassFront {stFront.Pass.ToShaderLab()}");
            _sb.AppendLine($"FailFront {stFront.Fail.ToShaderLab()}");
            _sb.AppendLine($"ZFailFront {stFront.ZFail.ToShaderLab()}");
        }

        // Back face stencil ops
        if (HasAnyPropertyRef(stBack.Pass, stBack.Fail, stBack.ZFail) || HasAnyPropertyRef(stBack.Comp)
            || stBack.Pass.Value != StencilOp.Keep || stBack.Fail.Value != StencilOp.Keep
            || stBack.ZFail.Value != StencilOp.Keep
            || (stBack.Comp.Value != StencilComp.Always && stBack.Comp.Value != StencilComp.Disabled))
        {
            _sb.AppendLine($"CompBack {stBack.Comp.ToShaderLab()}");
            _sb.AppendLine($"PassBack {stBack.Pass.ToShaderLab()}");
            _sb.AppendLine($"FailBack {stBack.Fail.ToShaderLab()}");
            _sb.AppendLine($"ZFailBack {stBack.ZFail.ToShaderLab()}");
        }

        _sb.Unindent();
        _sb.AppendLine("}");
    }

    private void WritePassRtBlend(SerializedShaderRTBlendState rtBlendState, int index)
    {
        var srcBlend = rtBlendState.SrcBlend;
        var destBlend = rtBlendState.DestBlend;
        var srcBlendAlpha = rtBlendState.SrcBlendAlpha;
        var destBlendAlpha = rtBlendState.DestBlendAlpha;
        var blendOp = rtBlendState.BlendOp;
        var blendOpAlpha = rtBlendState.BlendOpAlpha;
        var colMask = rtBlendState.ColMask;

        string idxStr = index != -1 ? $"{index} " : "";

        // Blend
        bool hasBlendRef = HasAnyPropertyRef(srcBlend, destBlend, srcBlendAlpha, destBlendAlpha);
        bool hasNonDefaultBlend = srcBlend.Value != BlendMode.One || destBlend.Value != BlendMode.Zero
            || srcBlendAlpha.Value != BlendMode.One || destBlendAlpha.Value != BlendMode.Zero;

        if (hasBlendRef || hasNonDefaultBlend)
        {
            // Check if alpha blend differs from color blend (by name or value)
            bool alphaDiffersFromColor =
                srcBlendAlpha.ToShaderLab() != srcBlend.ToShaderLab()
                || destBlendAlpha.ToShaderLab() != destBlend.ToShaderLab();

            _sb.Append("");
            _sb.AppendNoIndent($"Blend {idxStr}{srcBlend.ToShaderLab()} {destBlend.ToShaderLab()}");
            if (alphaDiffersFromColor)
            {
                _sb.AppendNoIndent($", {srcBlendAlpha.ToShaderLab()} {destBlendAlpha.ToShaderLab()}");
            }
            _sb.AppendNoIndent("\n");
        }

        // BlendOp
        bool hasBlendOpRef = HasAnyPropertyRef(blendOp, blendOpAlpha);
        bool hasNonDefaultBlendOp = blendOp.Value != BlendOp.Add || blendOpAlpha.Value != BlendOp.Add;

        if (hasBlendOpRef || hasNonDefaultBlendOp)
        {
            _sb.Append("");
            _sb.AppendNoIndent($"BlendOp {idxStr}{blendOp.ToShaderLab()}");
            if (blendOpAlpha.IsPropertyRef || blendOpAlpha.Value != BlendOp.Add)
            {
                _sb.AppendNoIndent($", {blendOpAlpha.ToShaderLab()}");
            }
            _sb.AppendNoIndent("\n");
        }

        // ColorMask
        if (colMask.IsPropertyRef)
        {
            _sb.Append("");
            _sb.AppendNoIndent($"ColorMask {colMask.ToShaderLab()}");
            if (index != -1)
                _sb.AppendNoIndent($" {index}");
            _sb.AppendNoIndent("\n");
        }
        else if (colMask.Value != ColorWriteMask.All)
        {
            _sb.Append("");
            _sb.AppendNoIndent("ColorMask ");
            if (colMask.Value == ColorWriteMask.None)
            {
                _sb.AppendNoIndent("0");
            }
            else
            {
                if (colMask.Value.HasFlag(ColorWriteMask.Red))
                    _sb.AppendNoIndent("R");
                if (colMask.Value.HasFlag(ColorWriteMask.Green))
                    _sb.AppendNoIndent("G");
                if (colMask.Value.HasFlag(ColorWriteMask.Blue))
                    _sb.AppendNoIndent("B");
                if (colMask.Value.HasFlag(ColorWriteMask.Alpha))
                    _sb.AppendNoIndent("A");
            }
            if (index != -1)
                _sb.AppendNoIndent($" {index}");
            _sb.AppendNoIndent("\n");
        }
    }

    // todo: REPLACE
    private string WritePassCBuffer(
        ShaderParameters shaderParams, HashSet<string> declaredCBufs,
        ConstantBuffer? cbuffer, int depth)
    {
        StringBuilder sb = new StringBuilder();
        if (cbuffer != null)
        {
            bool nonGlobalCbuffer = cbuffer.Name != "$Globals";
            int cbufferIndex = shaderParams.ConstantBuffers.IndexOf(cbuffer);

            bool wroteCbufferHeaderYet = false;

            char[] chars = new char[] { 'x', 'y', 'z', 'w' };
            List<ConstantBufferParameter> allParams = cbuffer.CBParams;
            foreach (ConstantBufferParameter param in allParams)
            {
                string typeName = HlslNamingUtils.GetConstantBufferParamTypeName(param);
                string name = param.ParamName;

                // skip things like unity_MatrixVP if they show up in $Globals
                if (UnityShaderConstants.INCLUDED_UNITY_PROP_NAMES.Contains(name))
                {
                    continue;
                }

                if (!wroteCbufferHeaderYet && nonGlobalCbuffer)
                {
                    sb.Append(new string(' ', depth * 4)); // todo: new stringbuilder
                    sb.AppendLine($"// CBUFFER_START({cbuffer.Name}) // {cbufferIndex}");
                    depth++;
                }

                if (!declaredCBufs.Contains(name))
                {
                    if (param.ArraySize > 0)
                    {
                        sb.Append(new string(' ', depth * 4));
                        if (nonGlobalCbuffer)
                            sb.Append("// ");
                        sb.AppendLine($"{typeName} {name}[{param.ArraySize}]; // {param.Index} (starting at cb{cbufferIndex}[{param.Index / 16}].{chars[param.Index % 16 / 4]})");
                    }
                    else
                    {
                        sb.Append(new string(' ', depth * 4));
                        if (nonGlobalCbuffer && !cbuffer.Name.StartsWith("UnityPerDrawSprite"))
                            sb.Append("// ");
                        sb.AppendLine($"{typeName} {name}; // {param.Index} (starting at cb{cbufferIndex}[{param.Index / 16}].{chars[param.Index % 16 / 4]})");
                    }
                    declaredCBufs.Add(name);
                }

                if (!wroteCbufferHeaderYet && nonGlobalCbuffer)
                {
                    depth--;
                    sb.Append(new string(' ', depth * 4));
                    sb.AppendLine("// CBUFFER_END");
                    wroteCbufferHeaderYet = true;
                }
            }
        }
        return sb.ToString();
    }

    // todo: REPLACE


    private string WritePassTextures(
        ShaderParameters shaderParams, HashSet<string> declaredCBufs, int depth)
    {
        StringBuilder sb = new StringBuilder();
        foreach (TextureParameter param in shaderParams.TextureParameters)
        {
            string name = param.Name;
            if (!declaredCBufs.Contains(name) && !UnityShaderConstants.BUILTIN_TEXTURE_NAMES.Contains(name))
            {
                sb.Append(new string(' ', depth * 4));
                switch (param.Dim)
                {
                    case 2:
                        sb.AppendLine($"sampler2D {name}; // {param.Index}");
                        break;
                    case 3:
                        sb.AppendLine($"sampler3D {name}; // {param.Index}");
                        break;
                    case 4:
                        sb.AppendLine($"samplerCUBE {name}; // {param.Index}");
                        break;
                    case 5:
                        sb.AppendLine($"UNITY_DECLARE_TEX2DARRAY({name}); // {param.Index}");
                        break;
                    case 6:
                        sb.AppendLine($"UNITY_DECLARE_TEXCUBEARRAY({name}); // {param.Index}");
                        break;
                    default:
                        sb.AppendLine($"sampler2D {name}; // {param.Index} // Unsure of real type ({param.Dim})");
                        break;
                }
                declaredCBufs.Add(name);
            }
        }
        return sb.ToString();
    }
}

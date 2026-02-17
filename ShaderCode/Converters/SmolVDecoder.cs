using System;
using System.IO;

namespace USCSandbox.ShaderCode.Converters;

public static class SmolVDecoder
{
    private const uint kSpirVHeaderMagic = 0x07230203;
    private const uint kSmolHeaderMagic = 0x534D4F4C;

    private const int SpvOpVectorShuffleCompact = 13;
    private const int SpvOpVectorShuffle = 79;
    private const int SpvOpDecorate = 71;
    private const int SpvOpMemberDecorate = 72;

    // OpData: hasResult, hasType, deltaFromResult, varrest
    private static readonly (byte hasResult, byte hasType, byte deltaFromResult, byte varrest)[] OpData =
    {
        (0,0,0,0), // 0 Nop
        (1,1,0,0), // 1 Undef
        (0,0,0,0), // 2 SourceContinued
        (0,0,0,1), // 3 Source
        (0,0,0,0), // 4 SourceExtension
        (0,0,0,0), // 5 Name
        (0,0,0,0), // 6 MemberName
        (0,0,0,0), // 7 String
        (0,0,0,1), // 8 Line
        (1,1,0,0), // 9
        (0,0,0,0), // 10 Extension
        (1,0,0,0), // 11 ExtInstImport
        (1,1,0,1), // 12 ExtInst
        (1,1,2,1), // 13 VectorShuffleCompact
        (0,0,0,1), // 14 MemoryModel
        (0,0,0,1), // 15 EntryPoint
        (0,0,0,1), // 16 ExecutionMode
        (0,0,0,1), // 17 Capability
        (1,1,0,0), // 18
        (1,0,0,1), // 19 TypeVoid
        (1,0,0,1), // 20 TypeBool
        (1,0,0,1), // 21 TypeInt
        (1,0,0,1), // 22 TypeFloat
        (1,0,0,1), // 23 TypeVector
        (1,0,0,1), // 24 TypeMatrix
        (1,0,0,1), // 25 TypeImage
        (1,0,0,1), // 26 TypeSampler
        (1,0,0,1), // 27 TypeSampledImage
        (1,0,0,1), // 28 TypeArray
        (1,0,0,1), // 29 TypeRuntimeArray
        (1,0,0,1), // 30 TypeStruct
        (1,0,0,1), // 31 TypeOpaque
        (1,0,0,1), // 32 TypePointer
        (1,0,0,1), // 33 TypeFunction
        (1,0,0,1), // 34 TypeEvent
        (1,0,0,1), // 35 TypeDeviceEvent
        (1,0,0,1), // 36 TypeReserveId
        (1,0,0,1), // 37 TypeQueue
        (1,0,0,1), // 38 TypePipe
        (0,0,0,1), // 39 TypeForwardPointer
        (1,1,0,0), // 40
        (1,1,0,0), // 41 ConstantTrue
        (1,1,0,0), // 42 ConstantFalse
        (1,1,0,0), // 43 Constant
        (1,1,9,0), // 44 ConstantComposite
        (1,1,0,1), // 45 ConstantSampler
        (1,1,0,0), // 46 ConstantNull
        (1,1,0,0), // 47
        (1,1,0,0), // 48 SpecConstantTrue
        (1,1,0,0), // 49 SpecConstantFalse
        (1,1,0,0), // 50 SpecConstant
        (1,1,9,0), // 51 SpecConstantComposite
        (1,1,0,0), // 52 SpecConstantOp
        (1,1,0,0), // 53
        (1,1,0,1), // 54 Function
        (1,1,0,0), // 55 FunctionParameter
        (0,0,0,0), // 56 FunctionEnd
        (1,1,9,0), // 57 FunctionCall
        (1,1,0,0), // 58
        (1,1,0,1), // 59 Variable
        (1,1,0,0), // 60 ImageTexelPointer
        (1,1,1,1), // 61 Load
        (0,0,2,1), // 62 Store
        (0,0,0,0), // 63 CopyMemory
        (0,0,0,0), // 64 CopyMemorySized
        (1,1,0,1), // 65 AccessChain
        (1,1,0,0), // 66 InBoundsAccessChain
        (1,1,0,0), // 67 PtrAccessChain
        (1,1,0,0), // 68 ArrayLength
        (1,1,0,0), // 69 GenericPtrMemSemantics
        (1,1,0,0), // 70 InBoundsPtrAccessChain
        (0,0,0,1), // 71 Decorate
        (0,0,0,1), // 72 MemberDecorate
        (1,0,0,0), // 73 DecorationGroup
        (0,0,0,0), // 74 GroupDecorate
        (0,0,0,0), // 75 GroupMemberDecorate
        (1,1,0,0), // 76
        (1,1,1,1), // 77 VectorExtractDynamic
        (1,1,2,1), // 78 VectorInsertDynamic
        (1,1,2,1), // 79 VectorShuffle
        (1,1,9,0), // 80 CompositeConstruct
        (1,1,1,1), // 81 CompositeExtract
        (1,1,2,1), // 82 CompositeInsert
        (1,1,1,0), // 83 CopyObject
        (1,1,0,0), // 84 Transpose
        (1,1,0,0), // 85
        (1,1,0,0), // 86 SampledImage
        (1,1,2,1), // 87 ImageSampleImplicitLod
        (1,1,2,1), // 88 ImageSampleExplicitLod
        (1,1,3,1), // 89 ImageSampleDrefImplicitLod
        (1,1,3,1), // 90 ImageSampleDrefExplicitLod
        (1,1,2,1), // 91 ImageSampleProjImplicitLod
        (1,1,2,1), // 92 ImageSampleProjExplicitLod
        (1,1,3,1), // 93 ImageSampleProjDrefImplicitLod
        (1,1,3,1), // 94 ImageSampleProjDrefExplicitLod
        (1,1,2,1), // 95 ImageFetch
        (1,1,3,1), // 96 ImageGather
        (1,1,3,1), // 97 ImageDrefGather
        (1,1,2,1), // 98 ImageRead
        (0,0,3,1), // 99 ImageWrite
        (1,1,1,0), // 100 Image
        (1,1,1,0), // 101 ImageQueryFormat
        (1,1,1,0), // 102 ImageQueryOrder
        (1,1,2,0), // 103 ImageQuerySizeLod
        (1,1,1,0), // 104 ImageQuerySize
        (1,1,2,0), // 105 ImageQueryLod
        (1,1,1,0), // 106 ImageQueryLevels
        (1,1,1,0), // 107 ImageQuerySamples
        (1,1,0,0), // 108
        (1,1,1,0), // 109 ConvertFToU
        (1,1,1,0), // 110 ConvertFToS
        (1,1,1,0), // 111 ConvertSToF
        (1,1,1,0), // 112 ConvertUToF
        (1,1,1,0), // 113 UConvert
        (1,1,1,0), // 114 SConvert
        (1,1,1,0), // 115 FConvert
        (1,1,1,0), // 116 QuantizeToF16
        (1,1,1,0), // 117 ConvertPtrToU
        (1,1,1,0), // 118 SatConvertSToU
        (1,1,1,0), // 119 SatConvertUToS
        (1,1,1,0), // 120 ConvertUToPtr
        (1,1,1,0), // 121 PtrCastToGeneric
        (1,1,1,0), // 122 GenericCastToPtr
        (1,1,1,1), // 123 GenericCastToPtrExplicit
        (1,1,1,0), // 124 Bitcast
        (1,1,0,0), // 125
        (1,1,1,0), // 126 SNegate
        (1,1,1,0), // 127 FNegate
        (1,1,2,0), // 128 IAdd
        (1,1,2,0), // 129 FAdd
        (1,1,2,0), // 130 ISub
        (1,1,2,0), // 131 FSub
        (1,1,2,0), // 132 IMul
        (1,1,2,0), // 133 FMul
        (1,1,2,0), // 134 UDiv
        (1,1,2,0), // 135 SDiv
        (1,1,2,0), // 136 FDiv
        (1,1,2,0), // 137 UMod
        (1,1,2,0), // 138 SRem
        (1,1,2,0), // 139 SMod
        (1,1,2,0), // 140 FRem
        (1,1,2,0), // 141 FMod
        (1,1,2,0), // 142 VectorTimesScalar
        (1,1,2,0), // 143 MatrixTimesScalar
        (1,1,2,0), // 144 VectorTimesMatrix
        (1,1,2,0), // 145 MatrixTimesVector
        (1,1,2,0), // 146 MatrixTimesMatrix
        (1,1,2,0), // 147 OuterProduct
        (1,1,2,0), // 148 Dot
        (1,1,2,0), // 149 IAddCarry
        (1,1,2,0), // 150 ISubBorrow
        (1,1,2,0), // 151 UMulExtended
        (1,1,2,0), // 152 SMulExtended
        (1,1,0,0), // 153
        (1,1,1,0), // 154 Any
        (1,1,1,0), // 155 All
        (1,1,1,0), // 156 IsNan
        (1,1,1,0), // 157 IsInf
        (1,1,1,0), // 158 IsFinite
        (1,1,1,0), // 159 IsNormal
        (1,1,1,0), // 160 SignBitSet
        (1,1,2,0), // 161 LessOrGreater
        (1,1,2,0), // 162 Ordered
        (1,1,2,0), // 163 Unordered
        (1,1,2,0), // 164 LogicalEqual
        (1,1,2,0), // 165 LogicalNotEqual
        (1,1,2,0), // 166 LogicalOr
        (1,1,2,0), // 167 LogicalAnd
        (1,1,1,0), // 168 LogicalNot
        (1,1,3,0), // 169 Select
        (1,1,2,0), // 170 IEqual
        (1,1,2,0), // 171 INotEqual
        (1,1,2,0), // 172 UGreaterThan
        (1,1,2,0), // 173 SGreaterThan
        (1,1,2,0), // 174 UGreaterThanEqual
        (1,1,2,0), // 175 SGreaterThanEqual
        (1,1,2,0), // 176 ULessThan
        (1,1,2,0), // 177 SLessThan
        (1,1,2,0), // 178 ULessThanEqual
        (1,1,2,0), // 179 SLessThanEqual
        (1,1,2,0), // 180 FOrdEqual
        (1,1,2,0), // 181 FUnordEqual
        (1,1,2,0), // 182 FOrdNotEqual
        (1,1,2,0), // 183 FUnordNotEqual
        (1,1,2,0), // 184 FOrdLessThan
        (1,1,2,0), // 185 FUnordLessThan
        (1,1,2,0), // 186 FOrdGreaterThan
        (1,1,2,0), // 187 FUnordGreaterThan
        (1,1,2,0), // 188 FOrdLessThanEqual
        (1,1,2,0), // 189 FUnordLessThanEqual
        (1,1,2,0), // 190 FOrdGreaterThanEqual
        (1,1,2,0), // 191 FUnordGreaterThanEqual
        (1,1,0,0), // 192
        (1,1,0,0), // 193
        (1,1,2,0), // 194 ShiftRightLogical
        (1,1,2,0), // 195 ShiftRightArithmetic
        (1,1,2,0), // 196 ShiftLeftLogical
        (1,1,2,0), // 197 BitwiseOr
        (1,1,2,0), // 198 BitwiseXor
        (1,1,2,0), // 199 BitwiseAnd
        (1,1,1,0), // 200 Not
        (1,1,4,0), // 201 BitFieldInsert
        (1,1,3,0), // 202 BitFieldSExtract
        (1,1,3,0), // 203 BitFieldUExtract
        (1,1,1,0), // 204 BitReverse
        (1,1,1,0), // 205 BitCount
        (1,1,0,0), // 206
        (1,1,0,0), // 207 DPdx
        (1,1,0,0), // 208 DPdy
        (1,1,0,0), // 209 Fwidth
        (1,1,0,0), // 210 DPdxFine
        (1,1,0,0), // 211 DPdyFine
        (1,1,0,0), // 212 FwidthFine
        (1,1,0,0), // 213 DPdxCoarse
        (1,1,0,0), // 214 DPdyCoarse
        (1,1,0,0), // 215 FwidthCoarse
        (1,1,0,0), // 216
        (1,1,0,0), // 217
        (0,0,0,0), // 218 EmitVertex
        (0,0,0,0), // 219 EndPrimitive
        (0,0,0,0), // 220 EmitStreamVertex
        (0,0,0,0), // 221 EndStreamPrimitive
        (1,1,0,0), // 222
        (1,1,0,0), // 223
        (0,0,3,0), // 224 ControlBarrier
        (0,0,2,0), // 225 MemoryBarrier
        (1,1,0,0), // 226
        (1,1,0,0), // 227 AtomicLoad
        (0,0,0,0), // 228 AtomicStore
        (1,1,0,0), // 229 AtomicExchange
        (1,1,0,0), // 230 AtomicCompareExchange
        (1,1,0,0), // 231 AtomicCompareExchangeWeak
        (1,1,0,0), // 232 AtomicIIncrement
        (1,1,0,0), // 233 AtomicIDecrement
        (1,1,0,0), // 234 AtomicIAdd
        (1,1,0,0), // 235 AtomicISub
        (1,1,0,0), // 236 AtomicSMin
        (1,1,0,0), // 237 AtomicUMin
        (1,1,0,0), // 238 AtomicSMax
        (1,1,0,0), // 239 AtomicUMax
        (1,1,0,0), // 240 AtomicAnd
        (1,1,0,0), // 241 AtomicOr
        (1,1,0,0), // 242 AtomicXor
        (1,1,0,0), // 243
        (1,1,0,0), // 244
        (1,1,0,0), // 245 Phi
        (0,0,2,1), // 246 LoopMerge
        (0,0,1,1), // 247 SelectionMerge
        (1,0,0,0), // 248 Label
        (0,0,1,0), // 249 Branch
        (0,0,3,1), // 250 BranchConditional
        (0,0,0,0), // 251 Switch
        (0,0,0,0), // 252 Kill
        (0,0,0,0), // 253 Return
        (0,0,0,0), // 254 ReturnValue
        (0,0,0,0), // 255 Unreachable
        (0,0,0,0), // 256 LifetimeStart
        (0,0,0,0), // 257 LifetimeStop
        (1,1,0,0), // 258
        (1,1,0,0), // 259 GroupAsyncCopy
        (0,0,0,0), // 260 GroupWaitEvents
        (1,1,0,0), // 261 GroupAll
        (1,1,0,0), // 262 GroupAny
        (1,1,0,0), // 263 GroupBroadcast
        (1,1,0,0), // 264 GroupIAdd
        (1,1,0,0), // 265 GroupFAdd
        (1,1,0,0), // 266 GroupFMin
        (1,1,0,0), // 267 GroupUMin
        (1,1,0,0), // 268 GroupSMin
        (1,1,0,0), // 269 GroupFMax
        (1,1,0,0), // 270 GroupUMax
        (1,1,0,0), // 271 GroupSMax
        (1,1,0,0), // 272
        (1,1,0,0), // 273
        (1,1,0,0), // 274 ReadPipe
        (1,1,0,0), // 275 WritePipe
        (1,1,0,0), // 276 ReservedReadPipe
        (1,1,0,0), // 277 ReservedWritePipe
        (1,1,0,0), // 278 ReserveReadPipePackets
        (1,1,0,0), // 279 ReserveWritePipePackets
        (0,0,0,0), // 280 CommitReadPipe
        (0,0,0,0), // 281 CommitWritePipe
        (1,1,0,0), // 282 IsValidReserveId
        (1,1,0,0), // 283 GetNumPipePackets
        (1,1,0,0), // 284 GetMaxPipePackets
        (1,1,0,0), // 285 GroupReserveReadPipePackets
        (1,1,0,0), // 286 GroupReserveWritePipePackets
        (0,0,0,0), // 287 GroupCommitReadPipe
        (0,0,0,0), // 288 GroupCommitWritePipe
        (1,1,0,0), // 289
        (1,1,0,0), // 290
        (1,1,0,0), // 291 EnqueueMarker
        (1,1,0,0), // 292 EnqueueKernel
        (1,1,0,0), // 293 GetKernelNDrangeSubGroupCount
        (1,1,0,0), // 294 GetKernelNDrangeMaxSubGroupSize
        (1,1,0,0), // 295 GetKernelWorkGroupSize
        (1,1,0,0), // 296 GetKernelPreferredWorkGroupSizeMultiple
        (0,0,0,0), // 297 RetainEvent
        (0,0,0,0), // 298 ReleaseEvent
        (1,1,0,0), // 299 CreateUserEvent
        (1,1,0,0), // 300 IsValidEvent
        (0,0,0,0), // 301 SetUserEventStatus
        (0,0,0,0), // 302 CaptureEventProfilingInfo
        (1,1,0,0), // 303 GetDefaultQueue
        (1,1,0,0), // 304 BuildNDRange
        (1,1,2,1), // 305 ImageSparseSampleImplicitLod
        (1,1,2,1), // 306 ImageSparseSampleExplicitLod
        (1,1,3,1), // 307 ImageSparseSampleDrefImplicitLod
        (1,1,3,1), // 308 ImageSparseSampleDrefExplicitLod
        (1,1,2,1), // 309 ImageSparseSampleProjImplicitLod
        (1,1,2,1), // 310 ImageSparseSampleProjExplicitLod
        (1,1,3,1), // 311 ImageSparseSampleProjDrefImplicitLod
        (1,1,3,1), // 312 ImageSparseSampleProjDrefExplicitLod
        (1,1,2,1), // 313 ImageSparseFetch
        (1,1,3,1), // 314 ImageSparseGather
        (1,1,3,1), // 315 ImageSparseDrefGather
        (1,1,1,0), // 316 ImageSparseTexelsResident
        (0,0,0,0), // 317 NoLine
        (1,1,0,0), // 318 AtomicFlagTestAndSet
        (0,0,0,0), // 319 AtomicFlagClear
        (1,1,0,0), // 320 ImageSparseRead
        (1,1,0,0), // 321 SizeOf
        (1,1,0,0), // 322 TypePipeStorage
        (1,1,0,0), // 323 ConstantPipeStorage
        (1,1,0,0), // 324 CreatePipeFromPipeStorage
        (1,1,0,0), // 325 GetKernelLocalSizeForSubgroupCount
        (1,1,0,0), // 326 GetKernelMaxNumSubgroups
        (1,1,0,0), // 327 TypeNamedBarrier
        (1,1,0,1), // 328 NamedBarrierInitialize
        (0,0,2,1), // 329 MemoryNamedBarrier
        (1,1,0,0), // 330 ModuleProcessed
        (0,0,0,1), // 331 ExecutionModeId
        (0,0,0,1), // 332 DecorateId
        (1,1,1,1), // 333 GroupNonUniformElect
        (1,1,1,1), // 334
        (1,1,1,1), // 335
        (1,1,1,1), // 336
        (1,1,1,1), // 337
        (1,1,1,1), // 338
        (1,1,1,1), // 339
        (1,1,1,1), // 340
        (1,1,1,1), // 341
        (1,1,1,1), // 342
        (1,1,1,1), // 343
        (1,1,1,1), // 344
        (1,1,1,1), // 345
        (1,1,1,1), // 346
        (1,1,1,1), // 347
        (1,1,1,1), // 348
        (1,1,1,1), // 349
        (1,1,1,1), // 350
        (1,1,1,1), // 351
        (1,1,1,1), // 352
        (1,1,1,1), // 353
        (1,1,1,1), // 354
        (1,1,1,1), // 355
        (1,1,1,1), // 356
        (1,1,1,1), // 357
        (1,1,1,1), // 358
        (1,1,1,1), // 359
        (1,1,1,1), // 360
        (1,1,1,1), // 361
        (1,1,1,1), // 362
        (1,1,1,1), // 363
        (1,1,1,1), // 364
        (1,1,1,1), // 365
        (1,1,1,1), // 366
    };

    // Bidirectional op remapping table: maps encoded op <-> decoded op
    private static readonly (int from, int to)[] OpRemapPairs =
    {
        (71, 0),    // Decorate <-> Nop
        (61, 1),    // Load <-> Undef
        (62, 2),    // Store <-> SourceContinued
        (65, 3),    // AccessChain <-> Source
        (79, 4),    // VectorShuffle <-> SourceExtension
        (72, 7),    // MemberDecorate <-> String
        (248, 8),   // Label <-> Line
        (59, 9),    // Variable <-> 9
        (133, 10),  // FMul <-> Extension
        (129, 11),  // FAdd <-> ExtInstImport
        (32, 14),   // TypePointer <-> MemoryModel
        (127, 15),  // FNegate <-> EntryPoint
    };

    private static readonly int[] OpRemapTable;

    static SmolVDecoder()
    {
        // Build a bidirectional remap table
        OpRemapTable = new int[256 * 2]; // support up to 512 opcodes
        for (int i = 0; i < OpRemapTable.Length; i++)
            OpRemapTable[i] = i;

        foreach (var (from, to) in OpRemapPairs)
        {
            OpRemapTable[from] = to;
            OpRemapTable[to] = from;
        }
    }

    private static int RemapOp(int op)
    {
        if (op >= 0 && op < OpRemapTable.Length)
            return OpRemapTable[op];
        return op;
    }

    private static int DecorationExtraOps(int dec)
    {
        if (dec == 0 || (dec >= 2 && dec <= 5))
            return 0;
        if (dec >= 29 && dec <= 37)
            return 1;
        return -1;
    }

    private static uint ZigZagDecode(uint val)
    {
        if ((val & 1) != 0)
            return ~(val >> 1);
        return val >> 1;
    }

    /// <summary>
    /// Returns the decoded SPIR-V buffer size from SMOL-V encoded data, or -1 on failure.
    /// </summary>
    public static int GetDecodedBufferSize(byte[] smolData)
    {
        if (smolData.Length < 24)
            return -1;
        return (int)BitConverter.ToUInt32(smolData, 20);
    }

    /// <summary>
    /// Decodes SMOL-V encoded data back into SPIR-V bytes.
    /// Ported from https://github.com/aras-p/smol-v
    /// </summary>
    public static byte[]? Decode(byte[] smolData)
    {
        if (smolData == null || smolData.Length < 24)
            return null;

        int decodedSize = GetDecodedBufferSize(smolData);
        if (decodedSize < 20 || (decodedSize & 3) != 0)
            return null;

        var reader = new SmolReader(smolData);

        // Read and validate header
        uint magic = reader.ReadRawUInt32();
        if (magic != kSmolHeaderMagic)
            return null;

        uint versionWord = reader.ReadRawUInt32();
        int smolVersion = (int)(versionWord >> 24);
        if (smolVersion > 1)
            return null;

        uint generator = reader.ReadRawUInt32();
        uint bound = reader.ReadRawUInt32();
        uint schema = reader.ReadRawUInt32();
        reader.ReadRawUInt32(); // decoded size (already read above)

        var writer = new SpvWriter(decodedSize);

        // Write SPIR-V header
        writer.WriteWord(kSpirVHeaderMagic);
        writer.WriteWord(versionWord & 0x00FFFFFF);
        writer.WriteWord(generator);
        writer.WriteWord(bound);
        writer.WriteWord(schema);

        bool beforeZeroVersion = smolVersion == 0;
        int knownOpsCount = smolVersion == 0 ? 331 : 367; // ModuleProcessed+1 or GroupNonUniformQuadSwap+1

        uint prevResult = 0;
        uint prevDecorate = 0;

        while (reader.Position < smolData.Length && writer.Position < decodedSize)
        {
            // Read length + op varint
            if (!reader.TryReadVarint(out uint lengthOpValue))
                return null;

            int encodedLen = (int)(((lengthOpValue >> 20) << 4) | ((lengthOpValue >> 4) & 0xF));
            int op = (int)(((lengthOpValue >> 4) & 0xFFF0) | (lengthOpValue & 0xF));

            op = RemapOp(op);

            // Decode length
            int instrLen = encodedLen + 1;
            if (op == SpvOpVectorShuffle || op == SpvOpVectorShuffleCompact) instrLen += 4;
            if (op == SpvOpDecorate) instrLen += 2;
            if (op == 61) instrLen += 3; // Load
            if (op == 65) instrLen += 3; // AccessChain

            bool wasSwizzle = op == SpvOpVectorShuffleCompact;
            if (wasSwizzle) op = SpvOpVectorShuffle;

            // Write instruction header
            writer.WriteWord((uint)((instrLen << 16) | op));
            int ioffs = 1;

            // Op info lookup
            bool hasType = op >= 0 && op < knownOpsCount && op < OpData.Length && OpData[op].hasType != 0;
            bool hasResult = op >= 0 && op < knownOpsCount && op < OpData.Length && OpData[op].hasResult != 0;
            int deltaFromResult = op >= 0 && op < knownOpsCount && op < OpData.Length ? OpData[op].deltaFromResult : 0;
            bool varrest = op >= 0 && op < knownOpsCount && op < OpData.Length && OpData[op].varrest != 0;

            if (hasType)
            {
                if (!reader.TryReadVarint(out uint v)) return null;
                writer.WriteWord(v);
                ioffs++;
            }
            if (hasResult)
            {
                if (!reader.TryReadVarint(out uint v)) return null;
                prevResult += ZigZagDecode(v);
                writer.WriteWord(prevResult);
                ioffs++;
            }

            // Decorate/MemberDecorate target ID
            if (op == SpvOpDecorate || op == SpvOpMemberDecorate)
            {
                if (!reader.TryReadVarint(out uint v)) return null;
                prevDecorate += (beforeZeroVersion ? v : ZigZagDecode(v));
                writer.WriteWord(prevDecorate);
                ioffs++;
            }

            // MemberDecorate batching (version >= 1)
            if (op == SpvOpMemberDecorate && !beforeZeroVersion)
            {
                if (!reader.TryReadByte(out byte countByte)) return null;
                int count = countByte;
                int prevIndex = 0;
                uint prevOffset = 0;

                for (int m = 0; m < count; m++)
                {
                    if (!reader.TryReadVarint(out uint memberIndex)) return null;
                    memberIndex += (uint)prevIndex;
                    prevIndex = (int)memberIndex;

                    if (!reader.TryReadVarint(out uint memberDec)) return null;
                    int knownExtra = DecorationExtraOps((int)memberDec);
                    int memberLen;
                    if (knownExtra == -1)
                    {
                        if (!reader.TryReadVarint(out uint mLen)) return null;
                        memberLen = (int)mLen + 4;
                    }
                    else
                    {
                        memberLen = 4 + knownExtra;
                    }

                    if (m != 0)
                    {
                        writer.WriteWord((uint)((memberLen << 16) | op));
                        writer.WriteWord(prevDecorate);
                    }
                    writer.WriteWord(memberIndex);
                    writer.WriteWord(memberDec);

                    if (memberDec == 35) // Offset decoration
                    {
                        if (memberLen != 5) return null;
                        if (!reader.TryReadVarint(out uint offsetVal)) return null;
                        offsetVal += prevOffset;
                        writer.WriteWord(offsetVal);
                        prevOffset = offsetVal;
                    }
                    else
                    {
                        for (int i = 4; i < memberLen; i++)
                        {
                            if (!reader.TryReadVarint(out uint extraVal)) return null;
                            writer.WriteWord(extraVal);
                        }
                    }
                }
                continue; // Skip rest of main loop
            }

            // Relative operands (deltaFromResult)
            int relativeCount = deltaFromResult;
            bool zigDecodeVals = true;
            if (beforeZeroVersion)
            {
                zigDecodeVals = op == 249 || op == 250 || op == 224 || op == 225
                    || op == 246 || op == 247 || op == 329;
            }
            for (int i = 0; i < relativeCount && ioffs < instrLen; i++, ioffs++)
            {
                if (!reader.TryReadVarint(out uint v)) return null;
                if (zigDecodeVals)
                    v = ZigZagDecode(v);
                writer.WriteWord(prevResult - v);
            }

            // VectorShuffleCompact swizzle byte
            if (wasSwizzle && instrLen <= 9)
            {
                if (!reader.TryReadByte(out byte swizzle)) return null;
                if (instrLen > 5) { writer.WriteWord((uint)((swizzle >> 6) & 3)); ioffs++; }
                if (instrLen > 6) { writer.WriteWord((uint)((swizzle >> 4) & 3)); ioffs++; }
                if (instrLen > 7) { writer.WriteWord((uint)((swizzle >> 2) & 3)); ioffs++; }
                if (instrLen > 8) { writer.WriteWord((uint)(swizzle & 3)); ioffs++; }
            }
            else if (varrest)
            {
                for (; ioffs < instrLen; ioffs++)
                {
                    if (!reader.TryReadVarint(out uint v)) return null;
                    writer.WriteWord(v);
                }
            }
            else
            {
                for (; ioffs < instrLen; ioffs++)
                {
                    if (!reader.TryReadRawUInt32(out uint v)) return null;
                    writer.WriteWord(v);
                }
            }
        }

        if (writer.Position != decodedSize)
            return null;

        return writer.GetBytes();
    }

    private class SmolReader
    {
        private readonly byte[] _data;
        private int _pos;

        public SmolReader(byte[] data)
        {
            _data = data;
            _pos = 0;
        }

        public int Position => _pos;

        public uint ReadRawUInt32()
        {
            uint val = BitConverter.ToUInt32(_data, _pos);
            _pos += 4;
            return val;
        }

        public bool TryReadRawUInt32(out uint val)
        {
            if (_pos + 4 > _data.Length)
            {
                val = 0;
                return false;
            }
            val = BitConverter.ToUInt32(_data, _pos);
            _pos += 4;
            return true;
        }

        public bool TryReadByte(out byte val)
        {
            if (_pos >= _data.Length)
            {
                val = 0;
                return false;
            }
            val = _data[_pos];
            _pos++;
            return true;
        }

        public bool TryReadVarint(out uint result)
        {
            result = 0;
            int shift = 0;
            while (true)
            {
                if (_pos >= _data.Length)
                    return false;
                byte b = _data[_pos++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return true;
                shift += 7;
                if (shift >= 35)
                    return false;
            }
        }
    }

    private class SpvWriter
    {
        private readonly byte[] _buffer;
        private int _pos;

        public SpvWriter(int size)
        {
            _buffer = new byte[size];
            _pos = 0;
        }

        public int Position => _pos;

        public void WriteWord(uint val)
        {
            if (_pos + 4 <= _buffer.Length)
            {
                _buffer[_pos + 0] = (byte)(val);
                _buffer[_pos + 1] = (byte)(val >> 8);
                _buffer[_pos + 2] = (byte)(val >> 16);
                _buffer[_pos + 3] = (byte)(val >> 24);
            }
            _pos += 4;
        }

        public uint ReadWordAt(int byteOffset)
        {
            if (byteOffset >= 0 && byteOffset + 4 <= _buffer.Length)
                return BitConverter.ToUInt32(_buffer, byteOffset);
            return 0;
        }

        public byte[] GetBytes()
        {
            return _buffer;
        }
    }
}

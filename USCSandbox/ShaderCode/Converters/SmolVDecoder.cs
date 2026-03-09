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

    
    private static readonly (byte hasResult, byte hasType, byte deltaFromResult, byte varrest)[] OpData =
    {
        (0,0,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,1), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,1), 
        (1,1,0,0), 
        (0,0,0,0), 
        (1,0,0,0), 
        (1,1,0,1), 
        (1,1,2,1), 
        (0,0,0,1), 
        (0,0,0,1), 
        (0,0,0,1), 
        (0,0,0,1), 
        (1,1,0,0), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (1,0,0,1), 
        (0,0,0,1), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,9,0), 
        (1,1,0,1), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,9,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,1), 
        (1,1,0,0), 
        (0,0,0,0), 
        (1,1,9,0), 
        (1,1,0,0), 
        (1,1,0,1), 
        (1,1,0,0), 
        (1,1,1,1), 
        (0,0,2,1), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,1), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,1), 
        (0,0,0,1), 
        (1,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,1,1), 
        (1,1,2,1), 
        (1,1,2,1), 
        (1,1,9,0), 
        (1,1,1,1), 
        (1,1,2,1), 
        (1,1,1,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,2,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,2,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,2,1), 
        (0,0,3,1), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,2,0), 
        (1,1,1,0), 
        (1,1,2,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,0,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,1), 
        (1,1,1,0), 
        (1,1,0,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,0,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,1,0), 
        (1,1,3,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,2,0), 
        (1,1,1,0), 
        (1,1,4,0), 
        (1,1,3,0), 
        (1,1,3,0), 
        (1,1,1,0), 
        (1,1,1,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,3,0), 
        (0,0,2,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,2,1), 
        (0,0,1,1), 
        (1,0,0,0), 
        (0,0,1,0), 
        (0,0,3,1), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,2,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,2,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,2,1), 
        (1,1,3,1), 
        (1,1,3,1), 
        (1,1,1,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (0,0,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,0), 
        (1,1,0,1), 
        (0,0,2,1), 
        (1,1,0,0), 
        (0,0,0,1), 
        (0,0,0,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
        (1,1,1,1), 
    };

    
    private static readonly (int from, int to)[] OpRemapPairs =
    {
        (71, 0),    
        (61, 1),    
        (62, 2),    
        (65, 3),    
        (79, 4),    
        (72, 7),    
        (248, 8),   
        (59, 9),    
        (133, 10),  
        (129, 11),  
        (32, 14),   
        (127, 15),  
    };

    private static readonly int[] OpRemapTable;

    static SmolVDecoder()
    {
        
        OpRemapTable = new int[256 * 2]; 
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

    
    
    
    public static int GetDecodedBufferSize(byte[] smolData)
    {
        if (smolData.Length < 24)
            return -1;
        return (int)BitConverter.ToUInt32(smolData, 20);
    }

    
    
    
    
    public static byte[]? Decode(byte[] smolData)
    {
        if (smolData == null || smolData.Length < 24)
            return null;

        int decodedSize = GetDecodedBufferSize(smolData);
        if (decodedSize < 20 || (decodedSize & 3) != 0)
            return null;

        var reader = new SmolReader(smolData);

        
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
        reader.ReadRawUInt32(); 

        var writer = new SpvWriter(decodedSize);

        
        writer.WriteWord(kSpirVHeaderMagic);
        writer.WriteWord(versionWord & 0x00FFFFFF);
        writer.WriteWord(generator);
        writer.WriteWord(bound);
        writer.WriteWord(schema);

        bool beforeZeroVersion = smolVersion == 0;
        int knownOpsCount = smolVersion == 0 ? 331 : 367; 

        uint prevResult = 0;
        uint prevDecorate = 0;

        while (reader.Position < smolData.Length && writer.Position < decodedSize)
        {
            
            if (!reader.TryReadVarint(out uint lengthOpValue))
                return null;

            int encodedLen = (int)(((lengthOpValue >> 20) << 4) | ((lengthOpValue >> 4) & 0xF));
            int op = (int)(((lengthOpValue >> 4) & 0xFFF0) | (lengthOpValue & 0xF));

            op = RemapOp(op);

            
            int instrLen = encodedLen + 1;
            if (op == SpvOpVectorShuffle || op == SpvOpVectorShuffleCompact) instrLen += 4;
            if (op == SpvOpDecorate) instrLen += 2;
            if (op == 61) instrLen += 3; 
            if (op == 65) instrLen += 3; 

            bool wasSwizzle = op == SpvOpVectorShuffleCompact;
            if (wasSwizzle) op = SpvOpVectorShuffle;

            
            writer.WriteWord((uint)((instrLen << 16) | op));
            int ioffs = 1;

            
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

            
            if (op == SpvOpDecorate || op == SpvOpMemberDecorate)
            {
                if (!reader.TryReadVarint(out uint v)) return null;
                prevDecorate += (beforeZeroVersion ? v : ZigZagDecode(v));
                writer.WriteWord(prevDecorate);
                ioffs++;
            }

            
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

                    if (memberDec == 35) 
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
                continue; 
            }

            
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

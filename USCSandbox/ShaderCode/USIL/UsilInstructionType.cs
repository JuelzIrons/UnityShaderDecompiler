namespace USCSandbox.ShaderCode.USIL;
public enum UsilInstructionType
{
    
    Move, 
    MoveConditional, 
    Add, 
    Subtract, 
    Multiply, 
    Divide, 
    MultiplyAdd, 

    And, 
    Or, 
    Xor, 
    Not, 

    ShiftLeft, 
    ShiftRight, 

    Floor, 
    Ceiling, 
    Round, 
    Truncate, 
    IntToFloat, 
    UIntToFloat, 
    FloatToInt, 
    FloatToUInt, 
    Negate, 
    Clamp, 
    ClampUInt, 

    Minimum, 
    Maximum, 

    SquareRoot, 
    SquareRootReciprocal, 

    ToThePower, 
    Logarithm2, 

    Sine, 
    Cosine, 

    DotProduct2, 
    DotProduct3, 
    DotProduct4, 

    Reciprocal, 
    Fractional, 

    ResourceDimensionInfo, 
    SampleCountInfo, 

    
    DerivativeRenderTargetX, 
    DerivativeRenderTargetY, 
    DerivativeRenderTargetXCoarse, 
    DerivativeRenderTargetYCoarse, 
    DerivativeRenderTargetXFine, 
    DerivativeRenderTargetYFine, 

    
    Equal, 
    NotEqual, 
    GreaterThan, 
    GreaterThanOrEqual, 
    LessThan, 
    LessThanOrEqual, 

    
    IfTrue, 
    IfFalse, 
    Else, 
    EndIf, 
    Return, 
    Loop, 
    ForLoop, 
    EndLoop, 
    Break, 
    Continue, 
    Switch, 
    Case, 
    Default, 
    EndSwitch, 

    
    Discard, 
    Sample, 
    SampleLODBias, 
    SampleComparison, 
    SampleComparisonLODZero, 
    SampleLOD, 
    SampleDerivative, 
    LoadResource, 
    LoadResourceMultisampled, 
    LoadResourceStructured, 

    
    GetDimensions, 

    
    MultiplyMatrixByVector,

    
    UnityObjectToClipPos,
    UnityObjectToWorldNormal,
    WorldSpaceViewDir,

    
    Comment
}

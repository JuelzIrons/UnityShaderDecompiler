using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL.Optimizers;



public class UsilAndOptimizer : IUsilOptimizer
{
    public bool Run(UShaderProgram shader, ShaderParameters shaderParams)
    {
        bool changes = false;

        HashSet<string> _comparisonResultRegisters = new();

        List<UsilInstruction> instructions = shader.Instructions;
        foreach (UsilInstruction instruction in instructions)
        {
            
            UsilOperand? destOperand = instruction.DestOperand;

            if (IsComparisonOrBooleanOp(instruction))
            {
                if (destOperand != null)
                {
                    SetRegisterIsComparison(destOperand, true, _comparisonResultRegisters);
                }
            }
            else if (instruction.InstructionType == UsilInstructionType.And)
            {
                UsilOperand leftOperand = instruction.SrcOperands[0];
                UsilOperand rightOperand = instruction.SrcOperands[1];
                if (rightOperand.OperandType == UsilOperandType.ImmediateFloat &&
                    rightOperand.ImmFloat[0] == 1)
                {
                    instruction.InstructionType = UsilInstructionType.MoveConditional;
                    instruction.SrcOperands = new List<UsilOperand>
                    {
                        leftOperand,
                        new UsilOperand()
                        {
                            OperandType = UsilOperandType.ImmediateFloat,
                            ImmFloat = new float[1] { 1f }
                        },
                        new UsilOperand()
                        {
                            OperandType = UsilOperandType.ImmediateFloat,
                            ImmFloat = new float[1] { 0f }
                        }
                    };

                    if (destOperand != null)
                    {
                        SetRegisterIsComparison(destOperand, false, _comparisonResultRegisters);
                    }
                }
                else
                {
                    bool leftIsComparison = IsRegisterComparison(leftOperand, _comparisonResultRegisters);
                    bool rightIsComparison = IsRegisterComparison(rightOperand, _comparisonResultRegisters);
                    if (leftIsComparison || rightIsComparison)
                    {
                        UsilOperand cmpOperand = leftIsComparison ? leftOperand : rightOperand;
                        UsilOperand resOperand = leftIsComparison ? rightOperand : leftOperand;

                        instruction.InstructionType = UsilInstructionType.MoveConditional;
                        instruction.SrcOperands = new List<UsilOperand>
                        {
                            cmpOperand,
                            resOperand,
                            new UsilOperand()
                            {
                                OperandType = UsilOperandType.ImmediateFloat,
                                ImmFloat = new float[1] { 0f }
                            }
                        };
                    }

                    
                    if (leftIsComparison && rightIsComparison)
                    {
                        SetRegisterIsComparison(destOperand, true, _comparisonResultRegisters);
                    }
                    else
                    {
                        SetRegisterIsComparison(destOperand, false, _comparisonResultRegisters);
                    }
                }
            }
        }
        return changes; 
    }

    private static void SetRegisterIsComparison(UsilOperand operand, bool isComparison, HashSet<string> _comparisonResultRegisters)
    {
        foreach (int maskIdx in operand.Mask)
        {
            
            if (isComparison)
            {
                _comparisonResultRegisters.Add($"{operand.ToString(true)}.{UsilConstants.MASK_CHARS[maskIdx]}");
            }
            else
            {
                _comparisonResultRegisters.Remove($"{operand.ToString(true)}.{UsilConstants.MASK_CHARS[maskIdx]}");
            }
        }
    }

    
    private static bool IsRegisterComparison(UsilOperand operand, HashSet<string> _comparisonResultRegisters)
    {
        foreach (int maskIdx in operand.Mask)
        {
            if (!_comparisonResultRegisters.Contains($"{operand.ToString(true)}.{UsilConstants.MASK_CHARS[maskIdx]}"))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsComparisonOrBooleanOp(UsilInstruction instruction)
    {
        
        return instruction.IsComparisonType() || instruction.InstructionType == UsilInstructionType.Not;
    }
}

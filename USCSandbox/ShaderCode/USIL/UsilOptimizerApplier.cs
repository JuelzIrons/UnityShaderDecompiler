using USCSandbox.ShaderCode.UShader;
using USCSandbox.ShaderCode.USIL.Fixers;
using USCSandbox.ShaderCode.USIL.Metadders;
using USCSandbox.ShaderCode.USIL.Optimizers;
using USCSandbox.ShaderMetadata;

namespace USCSandbox.ShaderCode.USIL;
public static class UsilOptimizerApplier
{
    
    
    
    
    
    
    
    private static readonly IUsilOptimizer[] OPTIMIZER_TYPES = new IUsilOptimizer[]
    {
        
        new UsilCBufferMetadder(),
        new UsilSamplerMetadder(),
        new UsilInputOutputMetadder(),
        
        
        new USILSamplerTypeFixer(),
        new UsilGetDimensionsFixer(),
        
        
        

        
        new UsilCompareOrderOptimizer(),
        new UsilAddNegativeOptimizer(),
        
        
        new USILObviousUIntFixer(),
        
        new UsilForLoopOptimizer(),
    };

    public static void Apply(UShaderProgram shader, ShaderParameters shaderData)
    {
        for (int i = 0; i < OPTIMIZER_TYPES.Length; i++)
        {
            OPTIMIZER_TYPES[i].Run(shader, shaderData);
        }
    }
}

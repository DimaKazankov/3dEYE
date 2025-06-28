using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class FileGeneratorFactory(ILogger logger)
{
    private static readonly string[] SampleStrings = ["Apple", "Banana is yellow", "Cherry is the best", "Something something something"];
    
    public IFileGenerator GetFileGenerator()
    {
        logger.LogDebug("Creating new FileGenerator instance with {SampleCount} sample strings", SampleStrings.Length);
        logger.LogTrace("Sample strings: {SampleStrings}", string.Join(", ", SampleStrings));
        
        var generator = new FileGenerator(logger, SampleStrings);
        logger.LogInformation("FileGenerator instance created successfully");
        
        return generator;
    }
}
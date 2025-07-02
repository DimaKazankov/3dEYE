using Microsoft.Extensions.Logging;

namespace _3dEYE.Benchmark;

public class TestFileManager<T> : IDisposable
{
    private readonly string _testDirectory;
    private readonly List<string> _tempDirectories = new();

    public string TestDirectory => _testDirectory;
    public string InputFile { get; }
    public string OutputFile { get; }
    public ILogger<T> Logger { get; private set; }

    public TestFileManager(string testName)
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"{testName}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        InputFile = Path.Combine(_testDirectory, "input.txt");
        OutputFile = Path.Combine(_testDirectory, "output.txt");
        Logger = CreateLogger();
    }
    
    private ILogger<T> CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        return loggerFactory.CreateLogger<T>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, recursive: true);
    }
}
namespace _3dEYE.Generator;

public interface IFileGenerator
{
    Task GenerateFileAsync(string filePath, long fileSizeInBytes);
}
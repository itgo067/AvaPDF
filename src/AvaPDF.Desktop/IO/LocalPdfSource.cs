using AvaPDF.Application.Abstractions;

namespace AvaPDF.Desktop.IO;

public sealed class LocalPdfSource(string path) : IPdfSource
{
    public string Path { get; } = path;

    public string Name => System.IO.Path.GetFileName(Path);

    public ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(
            Path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return ValueTask.FromResult(stream);
    }
}

public sealed class LocalPdfOutputTarget(string directory) : IPdfOutputTarget
{
    private readonly List<string> _createdPaths = [];

    public IReadOnlyList<string> CreatedPaths
    {
        get
        {
            lock (_createdPaths)
            {
                return _createdPaths.ToArray();
            }
        }
    }

    public ValueTask<Stream> OpenWriteAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directory);
        var safeName = System.IO.Path.GetFileName(fileName);
        var stem = System.IO.Path.GetFileNameWithoutExtension(safeName);
        var extension = System.IO.Path.GetExtension(safeName);
        var path = System.IO.Path.Combine(directory, safeName);

        for (var suffix = 2; File.Exists(path); suffix++)
        {
            path = System.IO.Path.Combine(directory, $"{stem}-{suffix}{extension}");
        }

        Stream stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        lock (_createdPaths)
        {
            _createdPaths.Add(path);
        }

        return ValueTask.FromResult(stream);
    }
}

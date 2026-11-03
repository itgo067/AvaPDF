namespace AvaPDF.Application.Abstractions;

/// <summary>A readable input independent of local files, HTTP uploads or cloud storage.</summary>
public interface IPdfSource
{
    string Name { get; }

    ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken);
}

/// <summary>Creates output streams. The host owns naming, overwrite and atomic-write policy.</summary>
public interface IPdfOutputTarget
{
    ValueTask<Stream> OpenWriteAsync(string fileName, CancellationToken cancellationToken);
}

public sealed class DelegatePdfSource(
    string name,
    Func<CancellationToken, ValueTask<Stream>> openReadAsync) : IPdfSource
{
    public string Name { get; } = !string.IsNullOrWhiteSpace(name)
        ? name
        : throw new ArgumentException("输入文件名不能为空。", nameof(name));

    public ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken) =>
        openReadAsync(cancellationToken);
}

public sealed class DelegatePdfOutputTarget(
    Func<string, CancellationToken, ValueTask<Stream>> openWriteAsync) : IPdfOutputTarget
{
    public ValueTask<Stream> OpenWriteAsync(string fileName, CancellationToken cancellationToken) =>
        openWriteAsync(fileName, cancellationToken);
}

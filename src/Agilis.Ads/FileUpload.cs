using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Agilis.Ads;

/// <summary>
/// An image to upload: a file path, a byte array or a stream, with a file name. The content type
/// is detected from the file name's extension unless you give one.
/// </summary>
public sealed class FileUpload
{
    private readonly string? _path;
    private readonly Stream? _stream;
    private byte[]? _bytes;

    private FileUpload(string fileName, string? contentType, string? path, byte[]? bytes, Stream? stream)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name is required.", nameof(fileName));
        }

        FileName = fileName;
        ContentType = string.IsNullOrEmpty(contentType) ? DetectContentType(fileName) : contentType!;
        _path = path;
        _bytes = bytes;
        _stream = stream;
    }

    /// <summary>The file name sent with the part.</summary>
    public string FileName { get; }

    /// <summary>The part's content type, for example <c>image/png</c>.</summary>
    public string ContentType { get; }

    /// <summary>An image read from a file. The file is read when the request is sent.</summary>
    /// <param name="path">The file's path.</param>
    /// <param name="contentType">Optional content type; detected from the extension when omitted.</param>
    public static FileUpload FromPath(string path, string? contentType = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("A path is required.", nameof(path));
        }

        return new FileUpload(Path.GetFileName(path), contentType, path, null, null);
    }

    /// <summary>An image held in memory.</summary>
    /// <param name="data">The file's bytes.</param>
    /// <param name="fileName">The file name, for example <c>offer.png</c>.</param>
    /// <param name="contentType">Optional content type; detected from the extension when omitted.</param>
    public static FileUpload FromBytes(byte[] data, string fileName, string? contentType = null)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return new FileUpload(fileName, contentType, null, data, null);
    }

    /// <summary>
    /// An image read from a stream. The stream is read once, from its current position, and kept
    /// in memory so that a retried request sends the same bytes. It is not disposed.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="fileName">The file name, for example <c>offer.jpg</c>.</param>
    /// <param name="contentType">Optional content type; detected from the extension when omitted.</param>
    public static FileUpload FromStream(Stream stream, string fileName, string? contentType = null)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        return new FileUpload(fileName, contentType, null, null, stream);
    }

    /// <summary>The content type for a file name: JPEG, PNG, GIF or WebP, otherwise <c>application/octet-stream</c>.</summary>
    /// <param name="fileName">The file name.</param>
    public static string DetectContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            case ".gif":
                return "image/gif";
            case ".webp":
                return "image/webp";
            default:
                return "application/octet-stream";
        }
    }

    internal async Task<byte[]> ReadAsync(CancellationToken cancellationToken)
    {
        if (_bytes != null)
        {
            return _bytes;
        }

        if (_path != null)
        {
            using var file = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            _bytes = await ReadAllAsync(file, cancellationToken).ConfigureAwait(false);
            return _bytes;
        }

        _bytes = await ReadAllAsync(_stream!, cancellationToken).ConfigureAwait(false);
        return _bytes;
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, 81920, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }
}

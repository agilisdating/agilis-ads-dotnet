using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Agilis.Ads.Internal;

/// <summary>Collects multipart fields once, then builds fresh content for every attempt.</summary>
internal sealed class MultipartForm
{
    private readonly List<KeyValuePair<string, string>> _fields = new List<KeyValuePair<string, string>>();
    private string? _fileField;
    private FileUpload? _file;
    private byte[]? _fileBytes;

    public IReadOnlyList<KeyValuePair<string, string>> Fields => _fields;

    public MultipartForm Add(string name, string? value)
    {
        if (value != null)
        {
            _fields.Add(new KeyValuePair<string, string>(name, value));
        }

        return this;
    }

    public MultipartForm Add(string name, int? value) =>
        value.HasValue ? Add(name, Json.FormatNumber(value.Value)) : this;

    public MultipartForm Add(string name, decimal? value) =>
        value.HasValue ? Add(name, Json.FormatNumber(value.Value)) : this;

    public MultipartForm Add(string name, DateTimeOffset? value) =>
        value.HasValue ? Add(name, Json.FormatDate(value.Value)) : this;

    public MultipartForm Add(string name, GeoLocation? value)
    {
        if (value.HasValue)
        {
            Add(name + "[]", Json.FormatNumber(value.Value.Latitude));
            Add(name + "[]", Json.FormatNumber(value.Value.Longitude));
        }

        return this;
    }

    public MultipartForm AddArray(string name, IEnumerable<string>? values)
    {
        if (values != null)
        {
            foreach (var v in values)
            {
                Add(name + "[]", v);
            }
        }

        return this;
    }

    public MultipartForm AddArray(string name, IEnumerable<int>? values)
    {
        if (values != null)
        {
            foreach (var v in values)
            {
                Add(name + "[]", Json.FormatNumber(v));
            }
        }

        return this;
    }

    public MultipartForm AddCrop(ImageCrop? crop)
    {
        if (crop != null)
        {
            Add("crop[x]", crop.X);
            Add("crop[y]", crop.Y);
            Add("crop[w]", crop.W);
        }

        return this;
    }

    public MultipartForm AddFile(string name, FileUpload? file)
    {
        if (file != null)
        {
            _fileField = name;
            _file = file;
        }

        return this;
    }

    public async Task PrepareAsync(CancellationToken cancellationToken)
    {
        if (_file != null && _fileBytes == null)
        {
            _fileBytes = await _file.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public HttpContent Build()
    {
        var content = new MultipartFormDataContent("agilis-" + Guid.NewGuid().ToString("N"));
        foreach (var field in _fields)
        {
            var part = new StringContent(field.Value, Encoding.UTF8);
            part.Headers.ContentType = null;
            content.Add(part, Quote(field.Key));
        }

        if (_file != null && _fileBytes != null)
        {
            var filePart = new ByteArrayContent(_fileBytes);
            filePart.Headers.ContentType = MediaTypeHeaderValue.Parse(_file.ContentType);
            content.Add(filePart, Quote(_fileField!), Quote(_file.FileName));
        }

        return content;
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}

/// <summary>A JSON object body, built from the API's own field names.</summary>
internal sealed class JsonBody
{
    private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();

    public IReadOnlyDictionary<string, object> Fields => _fields;

    public JsonBody Add(string name, object? value)
    {
        if (value != null)
        {
            _fields[name] = value;
        }

        return this;
    }

    public JsonBody Add(string name, DateTimeOffset? value) =>
        value.HasValue ? Add(name, (object)Json.FormatDate(value.Value)) : this;

    public JsonBody Add(string name, GeoLocation? value) =>
        value.HasValue ? Add(name, (object)new[] { value.Value.Latitude, value.Value.Longitude }) : this;

    public HttpContent Build()
    {
        var json = JsonSerializer.Serialize(_fields, Json.Options);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}

/// <summary>Builds a query string with the API's own parameter names.</summary>
internal sealed class Query
{
    private readonly List<KeyValuePair<string, string>> _items = new List<KeyValuePair<string, string>>();

    public Query Add(string name, string? value)
    {
        if (value != null)
        {
            _items.Add(new KeyValuePair<string, string>(name, value));
        }

        return this;
    }

    public Query Add(string name, int? value) => value.HasValue ? Add(name, Json.FormatNumber(value.Value)) : this;

    public Query Add(string name, bool? value) => value.HasValue ? Add(name, value.Value ? "true" : "false") : this;

    public Query Add(string name, DateTimeOffset? value) => value.HasValue ? Add(name, Json.FormatDate(value.Value)) : this;

    public override string ToString()
    {
        if (_items.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder("?");
        for (var i = 0; i < _items.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('&');
            }

            sb.Append(Uri.EscapeDataString(_items[i].Key)).Append('=').Append(Uri.EscapeDataString(_items[i].Value));
        }

        return sb.ToString();
    }
}

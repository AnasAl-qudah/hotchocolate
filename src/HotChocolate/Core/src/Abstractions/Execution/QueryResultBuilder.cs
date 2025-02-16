using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution;

public class QueryResultBuilder : IQueryResultBuilder
{
    private IReadOnlyDictionary<string, object?>? _data;
    private List<IError>? _errors;
    private ExtensionData? _extensionData;
    private ExtensionData? _contextData;
    private string? _label;
    private Path? _path;
    private bool? _hasNext;
    private Func<ValueTask>[] _cleanupTasks = Array.Empty<Func<ValueTask>>();

    public IQueryResultBuilder SetData(IReadOnlyDictionary<string, object?>? data)
    {
        _data = data;
        return this;
    }

    public IQueryResultBuilder AddError(IError error)
    {
        if (error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        _errors ??= new List<IError>();
        _errors.Add(error);
        return this;
    }

    public IQueryResultBuilder AddErrors(IEnumerable<IError> errors)
    {
        if (errors is null)
        {
            throw new ArgumentNullException(nameof(errors));
        }

        _errors ??= new List<IError>();
        _errors.AddRange(errors);
        return this;
    }

    public IQueryResultBuilder AddExtension(string key, object? data)
    {
        _extensionData ??= new ExtensionData();
        _extensionData.Add(key, data);
        return this;
    }

    public IQueryResultBuilder SetExtension(string key, object? data)
    {
        _extensionData ??= new ExtensionData();
        _extensionData[key] = data;
        return this;
    }

    public IQueryResultBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions)
    {
        if (extensions is ExtensionData extensionData)
        {
            _extensionData = extensionData;
        }
        else if (extensions is not null)
        {
            _extensionData = new ExtensionData(extensions);
        }
        else
        {
            _extensionData = null;
        }
        return this;
    }

    public IQueryResultBuilder AddContextData(string key, object? data)
    {
        _contextData ??= new ExtensionData();
        _contextData.Add(key, data);
        return this;
    }

    public IQueryResultBuilder SetContextData(string key, object? data)
    {
        _contextData ??= new ExtensionData();
        _contextData[key] = data;
        return this;
    }

    public IQueryResultBuilder SetContextData(IReadOnlyDictionary<string, object?>? contextData)
    {
        if (contextData is ExtensionData extensionData)
        {
            _contextData = extensionData;
        }
        else if (contextData is not null)
        {
            _contextData = new ExtensionData(contextData);
        }
        else
        {
            _contextData = null;
        }
        return this;
    }

    public IQueryResultBuilder SetLabel(string? label)
    {
        _label = label;
        return this;
    }

    public IQueryResultBuilder SetPath(Path? path)
    {
        _path = path;
        return this;
    }

    public IQueryResultBuilder SetHasNext(bool? hasNext)
    {
        _hasNext = hasNext;
        return this;
    }

    public IQueryResultBuilder RegisterForCleanup(Func<ValueTask> clean)
    {
        if (clean is null)
        {
            throw new ArgumentNullException(nameof(clean));
        }

        var index = _cleanupTasks.Length;
        Array.Resize(ref _cleanupTasks, index + 1);
        _cleanupTasks[index] = clean;
        return this;
    }

    public IQueryResult Create()
        => new QueryResult(
            _data,
            _errors?.Count > 0 ? _errors : null,
            _extensionData?.Count > 0 ? _extensionData : null,
            _contextData?.Count > 0 ? _contextData : null,
            _label,
            _path,
            _hasNext,
            _cleanupTasks);

    public static QueryResultBuilder New() => new();

    public static QueryResultBuilder FromResult(IQueryResult result)
    {
        var builder = new QueryResultBuilder { _data = result.Data };

        if (result.Errors is not null)
        {
            builder._errors = new List<IError>(result.Errors);
        }

        if (result.Extensions is ExtensionData d)
        {
            builder._extensionData = new ExtensionData(d);
        }
        else if (result.Extensions is not null)
        {
            builder._extensionData = new ExtensionData(result.Extensions);
        }

        if (result.ContextData is ExtensionData cd)
        {
            builder._contextData = new ExtensionData(cd);
        }
        else if (result.ContextData is not null)
        {
            builder._contextData = new ExtensionData(result.ContextData);
        }

        return builder;
    }

    public static IQueryResult CreateError(
        IError error,
        IReadOnlyDictionary<string, object?>? contextData = null)
        => error is AggregateError aggregateError
            ? CreateError(aggregateError.Errors, contextData)
            : new QueryResult(null, new List<IError> { error }, contextData: contextData);

    public static IQueryResult CreateError(
        IReadOnlyList<IError> errors,
        IReadOnlyDictionary<string, object?>? contextData = null)
        => new QueryResult(null, errors, contextData: contextData);
}

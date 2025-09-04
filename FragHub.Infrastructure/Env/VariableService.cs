using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Env;
using Microsoft.Extensions.Logging;

namespace FragHub.Infrastructure.Env;

/// <summary>
/// Provides functionality for managing and retrieving variables identified by unique keys. Variables do not persist between instances.
/// </summary>
/// <remarks>This service allows storing, updating, and retrieving variables using string keys. Keys are
/// case-insensitive. Variables can be initialized from multiple sources during construction, and additional variables
/// can be added or updated at runtime.</remarks>
public class VariableService : IVariableService
{
    private readonly Dictionary<string, Variable> _variables = new(StringComparer.OrdinalIgnoreCase);

    public VariableService(ILogger<IVariableService> logger, params IEnvConfig?[]? keySources)
    {
        ArgumentNullException.ThrowIfNull(keySources, nameof(keySources));

        foreach (var source in keySources)
        {
            if (source is null) { continue; }
            foreach (var variable in VariableLoader.LoadAll(source))
            {
                _variables[variable.Key] = variable;

                if (string.IsNullOrWhiteSpace(variable.Value))
                {
                    // If the variable's value is empty, remove it from the collection
                    _variables.Remove(variable.Key);
                    logger.LogWarning("Variable '{Key}' has an empty value and will not be stored.", variable.Key);
                }
            }
        }
    }

    /// <summary>
    /// Sets a variable with the specified key and value.
    /// </summary>
    /// <remarks>If a variable with the specified key already exists, its value will be overwritten.</remarks>
    /// <param name="key">The unique identifier for the variable. Cannot be null, empty, or consist solely of whitespace.</param>
    /// <param name="value">The value to associate with the specified key. Cannot be null.</param>
    public void SetVariable(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));
        ArgumentNullException.ThrowIfNull(value, nameof(key));

        _variables[key] = new Variable(key, value);
    }

    /// <summary>
    /// Retrieves the value of a variable associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to identify the variable. Cannot be null, empty, or consist solely of whitespace.</param>
    /// <returns>The value of the variable if the key exists; otherwise, <see langword="null"/>.</returns>
    public string? GetVariable(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        return _variables.TryGetValue(key, out var variable) ? variable.Value : null;
    }

    /// <summary>
    /// Retrieves all variables currently stored in the collection.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> containing all <see cref="Variable"/> instances in the collection. The
    /// collection will be empty if no variables are stored.</returns>
    public IEnumerable<Variable> GetAllVariables() => _variables.Values;
}

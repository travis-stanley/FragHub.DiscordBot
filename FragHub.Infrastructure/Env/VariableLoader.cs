using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FragHub.Application.Abstractions;
using FragHub.Domain.Env;

namespace FragHub.Infrastructure.Env;

public static class VariableLoader
{
    public static Variable Load(string key) => new(key, Environment.GetEnvironmentVariable(key) ?? string.Empty);

    public static IEnumerable<Variable> LoadAll(object instance)
    {
        foreach (var key in GetKeys(instance))
        {
            yield return Load(key);
        }
    }

    public static IEnumerable<string> GetKeys(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return instance
            .GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);
    }
}

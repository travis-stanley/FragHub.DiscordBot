using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FragHub.Domain.Env;

public class Variable(string key, string value)
{
    public string Key { get; } = key ?? throw new ArgumentNullException(nameof(key));
    public string Value { get; set; } = value ?? string.Empty;
}




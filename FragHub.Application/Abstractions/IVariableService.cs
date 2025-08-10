using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FragHub.Domain.Env;

namespace FragHub.Application.Abstractions;

public interface IVariableService
{
    string? GetVariable(string key);
    void SetVariable(string key, string value);
    IEnumerable<Variable> GetAllVariables();
}
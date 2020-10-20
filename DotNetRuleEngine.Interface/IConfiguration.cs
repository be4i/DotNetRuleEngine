using System;

namespace DotNetRuleEngine.Interface
{
    public interface IConfiguration
    {
        Func<bool> Constraint { get; set; }

        int? ExecutionOrder { get; set; }

        bool Skip { get; set; }

        bool? Terminate { get; set; }

        bool InvokeNestedRulesFirst { get; set; }

        bool NestedRulesInheritConstraint { get; set; }
    }
}
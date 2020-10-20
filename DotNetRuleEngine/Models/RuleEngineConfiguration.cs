﻿using System;
using DotNetRuleEngine.Interface;

namespace DotNetRuleEngine.Models
{
    internal class RuleEngineConfiguration : IRuleEngineConfiguration
    {
        public RuleEngineConfiguration(IConfiguration configuration)
        {
            Constraint = configuration.Constraint;
            ExecutionOrder = configuration.ExecutionOrder;
            Skip = configuration.Skip;
            Terminate = configuration.Terminate;
            InvokeNestedRulesFirst = configuration.InvokeNestedRulesFirst;
            NestedRulesInheritConstraint = configuration.NestedRulesInheritConstraint;
        }

        public Guid RuleEngineId { get; set; }

        public Func<bool> Constraint { get; set; }

        public int? ExecutionOrder { get; set; }

        public bool Skip { get; set; }

        public bool? Terminate { get; set; }

        public bool InvokeNestedRulesFirst { get; set; }

        public bool NestedRulesInheritConstraint { get; set; }
    }
}
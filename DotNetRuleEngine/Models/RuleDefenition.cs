using DotNetRuleEngine.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRuleEngine.Models
{
    public class RuleDefenition : IRuleDefenition
    {
        public object Rule { get; set; }
        public object Model { get; set; }

        public RuleDefenition(object rule, object model)
        {
            Rule = rule;
            Model = model;
        }
    }
}

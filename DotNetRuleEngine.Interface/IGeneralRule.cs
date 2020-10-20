using System;
using System.Collections.Generic;

namespace DotNetRuleEngine.Interface
{
    public interface IGeneralRule
    {
        object Model { get; set; }

        bool IsNested { get; }

        bool IsReactive { get; set; }

        bool IsProactive { get; set; }

        bool IsExceptionHandler { get; set; }

        bool IsGlobalExceptionHandler { get; set; }

        Type ObservedRule { get; }

        Exception UnhandledException { get; set; }

        IDependencyResolver Resolve { get; set; }

        IList<IRuleDefenition> GetRules();

        IConfiguration Configuration { get; set; }

        void AddRule(IGeneralRule rule, object model);

        void AddRule<TK>(object model) where TK : IGeneralRule;
    }
}
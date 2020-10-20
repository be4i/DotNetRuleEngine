using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleGeneral : IGeneralRule
    {
        void ObserveRule<TK>() where TK : IRuleGeneral;

        void Initialize();

        void BeforeInvoke();

        void AfterInvoke();

        IRuleResult Invoke();

        object TryGetValue(string key, int timeoutInMs);

        void TryAdd(string key, object value);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleAsyncGeneral : IGeneralRule
    {
        bool IsParallel { get; set; }

        IParallelConfiguration ParallelConfiguration { get; set; }

        void ObserveRule<TK>() where TK : IRuleAsyncGeneral;

        Task InitializeAsync();

        Task BeforeInvokeAsync();

        Task AfterInvokeAsync();

        Task<IRuleResult> InvokeAsync();

        Task<object> TryGetValueAsync(string key, int timeoutInMs);

        Task TryAddAsync(string key, Task<object> value);
    }
}

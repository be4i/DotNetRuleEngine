using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleAsyncGeneral : IGeneralRule
    {
        bool IsParallel { get; set; }

        IParallelConfiguration ParallelConfiguration { get; set; }

        void ObserveRule<TK>() where TK : IRuleAsyncGeneral;

        Task InitializeAsync(CancellationToken cancellationToken);

        Task BeforeInvokeAsync(CancellationToken cancellationToken);

        Task AfterInvokeAsync(CancellationToken cancellationToken);

        Task<IRuleResult> InvokeAsync(CancellationToken cancellationToken);

        Task<object> TryGetValueAsync(string key, int timeoutInMs);

        Task TryAddAsync(string key, Task<object> value);
    }
}

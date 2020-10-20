using System.Threading.Tasks;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleAsync<T> : IRuleConcrateModel<T>, IRuleAsyncGeneral
    {
        void AddRule(IRuleAsync<T> rule);

        void AddRule<TK>() where TK : IRuleAsync<T>;
    }
}
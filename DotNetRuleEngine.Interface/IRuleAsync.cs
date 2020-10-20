using System.Threading.Tasks;

namespace DotNetRuleEngine.Interface
{
    public interface IRuleAsync<T> : IRuleAsyncGeneral
    {
        new T Model { get; set; }

        void AddRule(IRuleAsync<T> rule);

        void AddRule<TK>() where TK : IRuleAsync<T>;
    }
}
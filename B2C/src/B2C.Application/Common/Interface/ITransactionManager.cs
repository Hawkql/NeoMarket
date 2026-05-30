using System.Threading;
using System.Threading.Tasks;

namespace B2C.Application.Common.Interface
{
    public interface ITransactionManager
    {
        Task BeginAsync(CancellationToken ct);
        Task CommitAsync(CancellationToken ct);
        Task RollbackAsync(CancellationToken ct);
    }
}

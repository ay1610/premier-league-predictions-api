using EplApi.Contracts;

namespace EplApi.Services;

public interface ITableService
{
    Task<IReadOnlyList<TableRowResponse>> GetTableAsync(CancellationToken cancellationToken);
}

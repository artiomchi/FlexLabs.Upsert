using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public interface IUpsertCommandRunner
    {
        bool Supports(string name);
        string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions);
        void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, IList<IProperty> joinColumns, IList<(IProperty Property, KnownExpressions Value)> updateExpressions, IList<(IProperty Property, object Value)> updateValues) where TEntity : class;
        Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, IList<IProperty> joinColumns, IList<(IProperty Property, KnownExpressions Value)> updateExpressions, IList<(IProperty Property, object Value)> updateValues, CancellationToken cancellationToken) where TEntity : class;
    }
}

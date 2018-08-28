using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    public interface IUpsertCommandRunner
    {
        bool Supports(string name);
        void Run<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression, Expression<Func<TEntity, TEntity>> updateExpression) where TEntity : class;
        Task RunAsync<TEntity>(DbContext dbContext, IEntityType entityType, TEntity[] entities, Expression<Func<TEntity, object>> matchExpression, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken) where TEntity : class;
    }
}

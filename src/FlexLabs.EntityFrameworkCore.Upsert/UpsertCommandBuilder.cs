using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FlexLabs.EntityFrameworkCore.Upsert.Runners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    public class UpsertCommandBuilder<TEntity> where TEntity : class
    {
        private readonly DbContext _dbContext;
        private readonly IEntityType _entityType;
        private readonly IEnumerable<TEntity> _entities;
        private Expression<Func<TEntity, object>> _matchExpression = null;
        private Expression<Func<TEntity, TEntity>> _updateExpression = null;

        internal UpsertCommandBuilder(DbContext dbContext, IEnumerable<TEntity> entities)
        {
            _dbContext = dbContext;
            _entities = entities ?? throw new ArgumentNullException(nameof(entities));

            _entityType = dbContext.GetService<IModel>().FindEntityType(typeof(TEntity));
        }

        public UpsertCommandBuilder<TEntity> On(Expression<Func<TEntity, object>> match)
        {
            if (_matchExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(On)} twice!");
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            _matchExpression = match;
            return this;
        }

        public UpsertCommandBuilder<TEntity> UpdateColumns(Expression<Func<TEntity, TEntity>> updater)
        {
            if (_updateExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(UpdateColumns)} twice!");
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));

            _updateExpression = updater;
            return this;
        }

        private IUpsertCommandRunner GetCommandRunner()
        {
            var dbProvider = _dbContext.GetService<IDatabaseProvider>();
            var commandRunner = _dbContext.GetInfrastructure().GetServices<IUpsertCommandRunner>()
                .Concat(DefaultRunners.Runners)
                .FirstOrDefault(r => r.Supports(dbProvider.Name));
            if (commandRunner == null)
                throw new NotSupportedException("Database provider not supported yet!");

            return commandRunner;
        }

        public void Run()
        {
            var commandRunner = GetCommandRunner();
            commandRunner.Run(_dbContext, _entityType, _entities, _matchExpression, _updateExpression);
        }

        public Task RunAsync(CancellationToken token = default)
        {
            var commandRunner = GetCommandRunner();
            return commandRunner.RunAsync(_dbContext, _entityType, _entities, _matchExpression, _updateExpression, token);
        }
    }
}

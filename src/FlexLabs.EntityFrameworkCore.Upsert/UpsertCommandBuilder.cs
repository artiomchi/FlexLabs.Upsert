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
    /// <summary>
    /// Used to configure an upsert command before running it
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to be upserted</typeparam>
    public class UpsertCommandBuilder<TEntity> where TEntity : class
    {
        private readonly DbContext _dbContext;
        private readonly IEntityType _entityType;
        private readonly ICollection<TEntity> _entities;
        private Expression<Func<TEntity, object>> _matchExpression = null;
        private Expression<Func<TEntity, TEntity, TEntity>> _updateExpression = null;
        private Expression<Func<TEntity, TEntity, bool>> _updateCondition = null;
        private bool _noUpdate = false, _useExpressionCompiler = false;

        /// <summary>
        /// Initialise an instance of the UpsertCommandBuilder
        /// </summary>
        /// <param name="dbContext">The data context that will be used to upsert entities</param>
        /// <param name="entities">The collection of entities to be upserted</param>
        internal UpsertCommandBuilder(DbContext dbContext, ICollection<TEntity> entities)
        {
            _dbContext = dbContext;
            _entities = entities;

            _entityType = dbContext.GetService<IModel>().FindEntityType(typeof(TEntity));
        }

        /// <summary>
        /// Specifies which columns will be used to find matching entities between the collection passed and the ones stored in the database
        /// </summary>
        /// <param name="match">The expression that will identity one or several columns to be used in the match clause</param>
        /// <returns>The current instance of the UpsertCommandBuilder</returns>
        public UpsertCommandBuilder<TEntity> On(Expression<Func<TEntity, object>> match)
        {
            if (_matchExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(On)} twice!");

            _matchExpression = match ?? throw new ArgumentNullException(nameof(match));
            return this;
        }

        /// <summary>
        /// Specifies which columns should be updated when a matched entity is found
        /// </summary>
        /// <param name="updater">The expression that returns a new instance of TEntity, with the columns that have to be updated being initialised with new values</param>
        /// <returns>The current instance of the UpsertCommandBuilder</returns>
        public UpsertCommandBuilder<TEntity> WhenMatched(Expression<Func<TEntity, TEntity>> updater)
        {
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));
            if (_updateExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} twice!");
            if (_noUpdate)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} when {nameof(NoUpdate)} has been called, as they are mutually exclusive");

            _updateExpression =
                Expression.Lambda<Func<TEntity, TEntity, TEntity>>(
                    updater.Body,
                    updater.Parameters[0],
                    Expression.Parameter(typeof(TEntity)));
            return this;
        }

        /// <summary>
        /// Specifies which columns should be updated when a matched entity is found.
        /// The second type parameter points to the entity that was originally passed to be inserted
        /// </summary>
        /// <param name="updater">The expression that returns a new instance of TEntity, with the columns that have to be updated being initialised with new values</param>
        /// <returns>The current instance of the UpsertCommandBuilder</returns>
        public UpsertCommandBuilder<TEntity> WhenMatched(Expression<Func<TEntity, TEntity, TEntity>> updater)
        {
            if (_updateExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} twice!");
            if (_noUpdate)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} when {nameof(NoUpdate)} has been called, as they are mutually exclusive");

            _updateExpression = updater ?? throw new ArgumentNullException(nameof(updater));
            return this;
        }

        /// <summary>
        /// Specifies a condition that has to be validated before updating existing entries
        /// </summary>
        /// <param name="condition">The condition that checks if a database entry should be updated</param>
        /// <returns>The current instance of the UpsertCommandBuilder</returns>
        public UpsertCommandBuilder<TEntity> UpdateIf(Expression<Func<TEntity, bool>> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (_updateCondition != null)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} twice!");
            if (_noUpdate)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} when {nameof(NoUpdate)} has been called, as they are mutually exclusive");

            _updateCondition =
                Expression.Lambda<Func<TEntity, TEntity, bool>>(
                    condition.Body,
                    condition.Parameters[0],
                    Expression.Parameter(typeof(TEntity)));
            return this;
        }

        /// <summary>
        /// Specifies a condition that has to be validated before updating existing entries
        /// The second type parameter points to the entity that was originally passed to be inserted
        /// </summary>
        /// <param name="condition">The condition that checks if a database entry should be updated</param>
        /// <returns>The current instance of the UpsertCommandBuilder</returns>
        public UpsertCommandBuilder<TEntity> UpdateIf(Expression<Func<TEntity, TEntity, bool>> condition)
        {
            if (_updateCondition != null)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} twice!");
            if (_noUpdate)
                throw new InvalidOperationException($"Can't call {nameof(WhenMatched)} when {nameof(NoUpdate)} has been called, as they are mutually exclusive");

            _updateCondition = condition ?? throw new ArgumentNullException(nameof(condition));
            return this;
        }

        /// <summary>
        /// Enables the use of the fallback expression compiler. This can be enabled to add support for more expression types in the Update statement
        /// at the cost of slower evaluation.
        /// If you have an expression type that isn't supported out of the box, please see https://go.flexlabs.org/upsert.expressions
        /// </summary>
        /// <returns></returns>
        public UpsertCommandBuilder<TEntity> WithFallbackExpressionCompiler()
        {
            _useExpressionCompiler = true;
            return this;
        }

        /// <summary>
        /// Specifies that if a match is found, no action will be taken on the entity
        /// </summary>
        /// <returns></returns>
        public UpsertCommandBuilder<TEntity> NoUpdate()
        {
            if (_updateExpression != null)
                throw new InvalidOperationException($"Can't call {nameof(NoUpdate)} when {nameof(WhenMatched)} has been called, as they are mutually exclusive");

            _noUpdate = true;
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

        /// <summary>
        /// Execute the upsert command against the database
        /// </summary>
        public int Run()
        {
            if (_entities.Count == 0)
                return 0;

            var commandRunner = GetCommandRunner();
            return commandRunner.Run(_dbContext, _entityType, _entities, _matchExpression, _updateExpression, _updateCondition, _noUpdate, _useExpressionCompiler);
        }

        /// <summary>
        /// Execute the upsert command against the database asynchronously
        /// </summary>
        /// <param name="token">The cancellation token for this transaction</param>
        /// <returns>The asynchronous task for this transaction</returns>
        public Task<int> RunAsync(CancellationToken token = default)
        {
            if (_entities.Count == 0)
                return Task.FromResult(0);

            var commandRunner = GetCommandRunner();
            return commandRunner.RunAsync(_dbContext, _entityType, _entities, _matchExpression, _updateExpression, _updateCondition, _noUpdate, _useExpressionCompiler, token);
        }
    }
}

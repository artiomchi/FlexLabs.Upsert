using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlexLabs.EntityFrameworkCore.Upsert.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Runners
{
    /// <summary>
    /// Upsert command runner for the MySql.Data.EntityFrameworkCore or the Pomelo.EntityFrameworkCore.MySql providers
    /// </summary>
    public class MySqlUpsertCommandRunner : RelationalUpsertCommandRunner
    {
        public override bool Supports(string name) => name == "MySql.Data.EntityFrameworkCore" || name == "Pomelo.EntityFrameworkCore.MySql";
        protected override string Column(string name) => "`" + name + "`";
        protected override string SourcePrefix => "VALUES(";
        protected override string SourceSuffix => ")";
        protected override string TargetPrefix => null;

        protected override string GenerateCommand(IEntityType entityType, ICollection<ICollection<(string ColumnName, ConstantValue Value)>> entities, ICollection<string> joinColumns,
            List<(string ColumnName, KnownExpression Value)> updateExpressions)
        {
            var schema = entityType.Relational().Schema;
            if (schema != null)
                schema = $"`{schema}`.";

            var result = new StringBuilder("INSERT ");
            if (updateExpressions == null)
                result.Append("IGNORE ");
            result.Append($"INTO {schema}`{entityType.Relational().TableName}` (");
            result.Append(string.Join(", ", entities.First().Select(e => Column(e.ColumnName))));
            result.Append(") VALUES (");
            result.Append(string.Join("), (", entities.Select(ec => string.Join(", ", ec.Select(e => Parameter(e.Value.ArgumentIndex))))));
            result.Append(")");
            if (updateExpressions != null)
            {
                result.Append(" ON DUPLICATE KEY UPDATE ");
                result.Append(string.Join(", ", updateExpressions.Select((e, i) => $"{Column(e.ColumnName)} = {ExpandExpression(e.Value)}")));
            }
            return result.ToString();
        }
    }
}

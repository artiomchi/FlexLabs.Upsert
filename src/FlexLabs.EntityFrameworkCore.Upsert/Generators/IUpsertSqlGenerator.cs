using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FlexLabs.EntityFrameworkCore.Upsert.Generators
{
    public interface IUpsertSqlGenerator
    {
        string GenerateCommand(IEntityType entityType, int entityCount, ICollection<string> insertColumns, ICollection<string> joinColumns, ICollection<string> updateColumns, List<(string ColumnName, KnownExpressions Value)> updateExpressions);
        bool Supports(string name);
    }
}

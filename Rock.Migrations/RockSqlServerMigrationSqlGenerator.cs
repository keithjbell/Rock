using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;

namespace Rock.Migrations
{
    /// <summary>
    /// 
    /// </summary>
    public class RockSqlServerMigrationSqlGenerator : SqlServerMigrationSqlGenerator 
    {
        /// <summary>
        /// Generates SQL for a <see cref="T:System.Data.Entity.Migrations.Model.SqlOperation" />.
        /// Generated SQL should be added using the Statement or StatementBatch methods.
        /// </summary>
        /// <param name="sqlOperation">The operation to produce SQL for.</param>
        protected override void Generate( SqlOperation sqlOperation )
        {
            // override Generator for Sql to use base.Statement instead of the 6.1.2 base.StatementBatch to avoid issue with 'go' when used in a string
            base.Statement( sqlOperation.Sql, sqlOperation.SuppressTransaction );
        }
    }
}

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using estacionamientos.Data;

namespace estacionamientos.Controllers
{
    public class SchemaController : Controller
    {
        private readonly AppDbContext _ctx;
        public SchemaController(AppDbContext ctx) => _ctx = ctx;

        public IActionResult Index()
        {
            var model = _ctx.Model.GetEntityTypes()
                .OrderBy(et => et.GetSchema())
                .ThenBy(et => et.GetTableName())
                .Select(et =>
                {
                    var tableName = et.GetTableName();
                    var schema = et.GetSchema();
                    if (tableName is null) return null!; // por seguridad, debería no ocurrir si está mapeada

                    var storeObject = StoreObjectIdentifier.Table(tableName, schema);
                    var pk = et.FindPrimaryKey();

                    var cols = et.GetProperties()
                        // ❌ Quitamos GetColumnOrder()
                        .OrderBy(p => p.GetColumnName(storeObject) ?? p.Name)
                        .Select(p => new ColumnInfo
                        {
                            ColumnName = p.GetColumnName(storeObject) ?? p.Name,
                            ClrType = p.ClrType.Name,
                            ColumnType = p.GetColumnType(),   // tipo SQL si está configurado
                            IsNullable = p.IsNullable,
                            IsKey = pk?.Properties.Contains(p) == true,
                            IsForeignKey = p.IsForeignKey()
                        })
                        .ToList();

                    return new TableInfo
                    {
                        Schema = schema ?? "public",
                        Table = tableName,
                        EntityName = et.ClrType?.Name ?? et.Name,
                        Columns = cols
                    };
                })
                .Where(t => t != null)
                .OrderBy(t => t!.Schema).ThenBy(t => t!.Table)
                .ToList()!;

            return View(model);
        }

        // ViewModels
        public sealed class TableInfo
        {
            public string Schema { get; set; } = "";
            public string Table { get; set; } = "";
            public string EntityName { get; set; } = "";
            public List<ColumnInfo> Columns { get; set; } = new();
        }

        public sealed class ColumnInfo
        {
            public string ColumnName { get; set; } = "";
            public string ClrType { get; set; } = "";
            public string? ColumnType { get; set; }
            public bool IsNullable { get; set; }
            public bool IsKey { get; set; }
            public bool IsForeignKey { get; set; }
        }
    }

    internal static class EfMetaExtensions
    {
        public static bool IsForeignKey(this IProperty p)
            => p.GetContainingForeignKeys()?.Any() == true;
    }
}

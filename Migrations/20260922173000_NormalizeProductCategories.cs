using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceApp.Migrations
{
    public partial class NormalizeProductCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "Category" = CASE
                    WHEN "Category" ILIKE 'bebida%' THEN 'Bebida'
                    WHEN "Category" ILIKE 'postre%' THEN 'Postre'
                    WHEN "Category" ILIKE 'snack%' THEN 'Snack'
                    ELSE 'Snack'
                END
                WHERE "Category" IS NULL
                   OR BTRIM("Category") = ''
                   OR "Category" NOT IN ('Bebida', 'Postre', 'Snack');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La normalización de texto no puede revertirse con seguridad.
        }
    }
}

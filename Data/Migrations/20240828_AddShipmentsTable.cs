using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubApi.Data.Migrations;

/// <inheritdoc />
public partial class AddShipmentsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "shipments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                tracking_number = table.Column<string>(type: "text", nullable: false),
                carrier = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                tracking_url = table.Column<string>(type: "text", nullable: true),
                shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                estimated_delivery = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_shipments", x => x.id);
                table.ForeignKey(
                    name: "fk_shipments_orders_order_id",
                    column: x => x.order_id,
                    principalTable: "orders_v2",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_shipments_carrier",
            table: "shipments",
            column: "carrier");

        migrationBuilder.CreateIndex(
            name: "ix_shipments_order_id",
            table: "shipments",
            column: "order_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_shipments_status",
            table: "shipments",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_shipments_tracking_number",
            table: "shipments",
            column: "tracking_number");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "shipments");
    }
}

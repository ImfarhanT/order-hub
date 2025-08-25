using Microsoft.EntityFrameworkCore.Migrations;

namespace HubApi.Data.Migrations
{
    public partial class AddRawOrderDataTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawOrderData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawOrderData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RawOrderData_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawOrderData_SiteId",
                table: "RawOrderData",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_RawOrderData_ReceivedAt",
                table: "RawOrderData",
                column: "ReceivedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawOrderData");
        }
    }
}


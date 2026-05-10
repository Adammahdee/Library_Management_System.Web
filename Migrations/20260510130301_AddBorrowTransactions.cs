using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Library_Management_System.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "ActionTime",
                table: "AuditLogs",
                newName: "LogDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogDate",
                table: "AuditLogs",
                newName: "ActionTime");

            migrationBuilder.AddColumn<int>(
                name: "RecordId",
                table: "AuditLogs",
                type: "int",
                nullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace traobang.be.infrastructure.data.Migrations
{
    /// <inheritdoc />
    public partial class moveqrtenkhoatosubplan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrTenKhoa",
                schema: "tb",
                table: "SubPlan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "QrTenKhoa",
                schema: "tb",
                table: "DanhSachSinhVienNhanBang");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrTenKhoa",
                schema: "tb",
                table: "DanhSachSinhVienNhanBang",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(
                name: "QrTenKhoa",
                schema: "tb",
                table: "SubPlan");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace traobang.be.infrastructure.data.Migrations
{
    /// <inheritdoc />
    public partial class addlinkqronly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkQrOnly",
                schema: "tb",
                table: "DanhSachSinhVienNhanBang",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkQrOnly",
                schema: "tb",
                table: "DanhSachSinhVienNhanBang");
        }
    }
}

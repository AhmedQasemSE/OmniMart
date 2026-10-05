using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniMart.Infrastructure.Migrations
{
    public partial class AddRowVersionToEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. نحذف العمود القديم لأنه يرفض التعديل المباشر
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffProfiles");

            // 2. نضيفه من جديد بالنوع الصحيح (rowversion)
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffProfiles",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // في حالة التراجع، نعكس العملية
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StaffProfiles");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StaffProfiles",
                type: "varbinary(max)",
                nullable: false);
        }
    }
}
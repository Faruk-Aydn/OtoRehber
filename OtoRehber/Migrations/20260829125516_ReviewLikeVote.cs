using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class ReviewLikeVote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLike_AspNetUsers_UserId",
                table: "ReviewLike");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLike_CarReviews_ReviewId",
                table: "ReviewLike");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLike",
                table: "ReviewLike");

            migrationBuilder.DropIndex(
                name: "IX_ReviewLike_UserId",
                table: "ReviewLike");

            migrationBuilder.RenameTable(
                name: "ReviewLike",
                newName: "ReviewLikes");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLike_ReviewId",
                table: "ReviewLikes",
                newName: "IX_ReviewLikes_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLikes",
                table: "ReviewLikes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLikes_UserId_ReviewId",
                table: "ReviewLikes",
                columns: new[] { "UserId", "ReviewId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLikes_AspNetUsers_UserId",
                table: "ReviewLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLikes_CarReviews_ReviewId",
                table: "ReviewLikes",
                column: "ReviewId",
                principalTable: "CarReviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLikes_AspNetUsers_UserId",
                table: "ReviewLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLikes_CarReviews_ReviewId",
                table: "ReviewLikes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLikes",
                table: "ReviewLikes");

            migrationBuilder.DropIndex(
                name: "IX_ReviewLikes_UserId_ReviewId",
                table: "ReviewLikes");

            migrationBuilder.RenameTable(
                name: "ReviewLikes",
                newName: "ReviewLike");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLikes_ReviewId",
                table: "ReviewLike",
                newName: "IX_ReviewLike_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLike",
                table: "ReviewLike",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewLike_UserId",
                table: "ReviewLike",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLike_AspNetUsers_UserId",
                table: "ReviewLike",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLike_CarReviews_ReviewId",
                table: "ReviewLike",
                column: "ReviewId",
                principalTable: "CarReviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

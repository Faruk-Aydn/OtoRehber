import sys

with open('OtoRehber/Migrations/20260806083410_UXFeatures.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace CreateTable for AspNetRoles and AspNetUsers
replacement = '''            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "XP",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);'''

start_idx = content.find('            migrationBuilder.CreateTable(\n                name: "AspNetRoles"')
end_idx = content.find('            migrationBuilder.CreateTable(\n                name: "CarPriceHistories"')

if start_idx != -1 and end_idx != -1:
    content = content[:start_idx] + replacement + '\n\n' + content[end_idx:]

# Also we must remove CreateTable for AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens
start_idx_2 = content.find('            migrationBuilder.CreateTable(\n                name: "AspNetRoleClaims"')
end_idx_2 = content.find('            migrationBuilder.CreateTable(\n                name: "ReviewLikes"')

if start_idx_2 != -1 and end_idx_2 != -1:
    content = content[:start_idx_2] + content[end_idx_2:]

# Now remove indexes related to AspNet
idx_start = content.find('            migrationBuilder.CreateIndex(\n                name: "IX_AspNetRoleClaims_RoleId"')
idx_end = content.find('            migrationBuilder.CreateIndex(\n                name: "IX_CarPriceHistories_CarId"')
if idx_start != -1 and idx_end != -1:
    content = content[:idx_start] + content[idx_end:]


# Down method fixes
down_start = content.find('            migrationBuilder.DropTable(\n                name: "AspNetRoleClaims"')
down_end = content.find('            migrationBuilder.DropTable(\n                name: "CarPriceHistories"')
if down_start != -1 and down_end != -1:
    content = content[:down_start] + content[down_end:]

down_start_2 = content.find('            migrationBuilder.DropTable(\n                name: "AspNetRoles"')
down_end_2 = content.find('            migrationBuilder.DropTable(\n                name: "CarReviews"')
if down_start_2 != -1 and down_end_2 != -1:
    down_replacement = '''            migrationBuilder.DropColumn(name: "AvatarUrl", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "Level", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "XP", table: "AspNetUsers");\n\n'''
    content = content[:down_start_2] + down_replacement + content[down_end_2:]

with open('OtoRehber/Migrations/20260806083410_UXFeatures.cs', 'w', encoding='utf-8') as f:
    f.write(content)

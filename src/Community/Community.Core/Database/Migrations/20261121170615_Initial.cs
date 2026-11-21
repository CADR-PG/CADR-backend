using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Community");

            migrationBuilder.CreateTable(
                name: "Chats",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DirectChatLowerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DirectChatHigherUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                schema: "Community",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsNameCustomized = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                schema: "Community",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: true),
                    LastMessageReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => new { x.ChatId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChatParticipants_Chats_ChatId",
                        column: x => x.ChatId,
                        principalSchema: "Community",
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<long>(type: "bigint", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendRequests_UserProfiles_ReceiverId",
                        column: x => x.ReceiverId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FriendRequests_UserProfiles_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                schema: "Community",
                columns: table => new
                {
                    LowerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    HigherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => new { x.LowerUserId, x.HigherUserId });
                    table.ForeignKey(
                        name: "FK_Friendships_UserProfiles_HigherUserId",
                        column: x => x.HigherUserId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friendships_UserProfiles_LowerUserId",
                        column: x => x.LowerUserId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBlocks",
                schema: "Community",
                columns: table => new
                {
                    BlockerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => new { x.BlockerId, x.BlockedId });
                    table.ForeignKey(
                        name: "FK_UserBlocks_UserProfiles_BlockedId",
                        column: x => x.BlockedId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBlocks_UserProfiles_BlockerId",
                        column: x => x.BlockerId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfileEdits",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfileEdits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfileEdits_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                schema: "Community",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupInvitePolicy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserSettings_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "Community",
                        principalTable: "UserProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "Community",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ClientMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferencedMessageId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatMessages_ReferencedMessageId",
                        column: x => x.ReferencedMessageId,
                        principalSchema: "Community",
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatParticipants_ChatId_SenderId",
                        columns: x => new { x.ChatId, x.SenderId },
                        principalSchema: "Community",
                        principalTable: "ChatParticipants",
                        principalColumns: new[] { "ChatId", "UserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Chats_ChatId",
                        column: x => x.ChatId,
                        principalSchema: "Community",
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessagePins",
                schema: "Community",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    PinnedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PinnedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessagePins", x => new { x.ChatId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_ChatMessagePins_ChatMessages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "Community",
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessagePins_MessageId",
                schema: "Community",
                table: "ChatMessagePins",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatId_Id",
                schema: "Community",
                table: "ChatMessages",
                columns: new[] { "ChatId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChatId_SenderId_ClientMessageId",
                schema: "Community",
                table: "ChatMessages",
                columns: new[] { "ChatId", "SenderId", "ClientMessageId" },
                unique: true,
                filter: "\"ClientMessageId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ReferencedMessageId",
                schema: "Community",
                table: "ChatMessages",
                column: "ReferencedMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_UserId",
                schema: "Community",
                table: "ChatParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_DirectChatLowerUserId_DirectChatHigherUserId",
                schema: "Community",
                table: "Chats",
                columns: new[] { "DirectChatLowerUserId", "DirectChatHigherUserId" },
                unique: true,
                filter: "\"Type\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_ReceiverId",
                schema: "Community",
                table: "FriendRequests",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_SenderId_ReceiverId",
                schema: "Community",
                table: "FriendRequests",
                columns: new[] { "SenderId", "ReceiverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_HigherUserId",
                schema: "Community",
                table: "Friendships",
                column: "HigherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedId",
                schema: "Community",
                table: "UserBlocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfileEdits_UserId_EditedAt",
                schema: "Community",
                table: "UserProfileEdits",
                columns: new[] { "UserId", "EditedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessagePins",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "FriendRequests",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "Friendships",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "UserBlocks",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "UserProfileEdits",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "UserSettings",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "ChatParticipants",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "Chats",
                schema: "Community");

            migrationBuilder.DropTable(
                name: "UserProfiles",
                schema: "Community");
        }
    }
}

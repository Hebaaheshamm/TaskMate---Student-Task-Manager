CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;
CREATE TABLE "Users" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Email" TEXT NOT NULL,
    "NormalizedEmail" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL
);

CREATE TABLE "Tasks" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NULL,
    "DueDate" TEXT NULL,
    "Priority" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "CreatedAtUtc" TEXT NOT NULL,
    "UpdatedAtUtc" TEXT NOT NULL,
    CONSTRAINT "FK_Tasks_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Tasks_UserId_DueDate" ON "Tasks" ("UserId", "DueDate");

CREATE INDEX "IX_Tasks_UserId_Status" ON "Tasks" ("UserId", "Status");

CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261022193024_InitialCreate', '10.0.13');

COMMIT;

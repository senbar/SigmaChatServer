namespace SigmaChatServer

module Migrations =
    let GetVersion =
        """
        SELECT "Version" FROM "Migrations" 
        ORDER BY "Version" DESC
        LIMIT 1;
        """

    let Migrations =
        [| """
            CREATE TABLE "Migrations"(
                "Version" INT PRIMARY KEY
            );
            """
           """
            CREATE TABLE "Chats"(
                "ChatId" serial PRIMARY KEY
            );

            INSERT INTO "Chats" DEFAULT VALUES ;

            CREATE TABLE "Messages"(
                "ChatId" INT NOT NULL,
                "MessageId" serial PRIMARY KEY,
                "Sender" VARCHAR(100) NOT NULL,
                "Text" VARCHAR(500) NOT NULL,
                "DateCreated" TIMESTAMP NOT NULL,
                FOREIGN KEY ("ChatId")
                    REFERENCES "Chats" ("ChatId")
            );
            """
           """
            CREATE TABLE "Users"(
                "Id" VARCHAR(50) PRIMARY KEY,
                "Email" VARCHAR(500),
                "Nickname" VARCHAR(500)
            );

            ALTER TABLE "Messages"
            DROP COLUMN "Sender",
            ADD COLUMN "UserId" VARCHAR(50) NOT NULL REFERENCES "Users"("Id");
            """
           """
            CREATE TABLE "PushSubscriptions"(
                "Id" serial PRIMARY KEY,
                "UserId" VARCHAR(50),
                "Json" VARCHAR(4000),
                "DateCreated" TIMESTAMP NOT NULL,
                FOREIGN KEY ("UserId")
                    REFERENCES "Users" ("Id")
            );
            """
           """
            DROP TABLE "PushSubscriptions";

            CREATE TABLE "PushSubscriptions"(
                "UserId" VARCHAR(50) PRIMARY KEY,
                "Json" VARCHAR(4000),
                "DateCreated" TIMESTAMP NOT NULL,
                FOREIGN KEY ("UserId")
                    REFERENCES "Users" ("Id")
            );
             """
           """
            CREATE TABLE "UserProfilePictures"(
                "UserId" VARCHAR(50) PRIMARY KEY,
                "BlobName" VARCHAR(50) NOT NULL,
                "DateCreated" TIMESTAMP NOT NULL,
                "OriginalFilename" VARCHAR(255) NOT NULL,
                FOREIGN KEY ("UserId")
                    REFERENCES "Users" ("Id")
            );
            
            CREATE UNIQUE INDEX idx_UserProfilePictures_BlobName ON "UserProfilePictures" ("BlobName");
            """ |]

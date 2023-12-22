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
         """ |]

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
        """ |]

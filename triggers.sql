CREATE OR REPLACE FUNCTION InsertFifteen() RETURNS trigger AS $InsertFifteen$
    BEGIN
       INSERT INTO "Fifteens"(
        "UserId")
    values (
        NEW."Id"
        );

    RETURN NEW;
END;
   $InsertFifteen$ LANGUAGE plpgsql;

   CREATE OR REPLACE TRIGGER insertFifteen AFTER INSERT ON "AspNetUsers"
FOR EACH ROW EXECUTE PROCEDURE InsertFifteen();
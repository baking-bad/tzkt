using Microsoft.EntityFrameworkCore.Migrations;

namespace Tzkt.Data.Migrations
{
    public partial class Triggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION public.state_changedf()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                IF TG_OP = 'INSERT' then
                PERFORM pg_notify('notifystatechanged', 'INSERT');
                ELSIF TG_OP = 'UPDATE' then
                PERFORM pg_notify('notifystatechanged', 'UPDATE');
                ELSIF TG_OP = 'DELETE' then
                PERFORM pg_notify('notifystatechanged', 'DELETE');
                END IF;
                RETURN NULL;
                END;
                $function$;");
            migrationBuilder.Sql(@"create trigger state_changed after
            insert
                or
            delete
                or
            update
                on
        public.""AppState"" for each row execute procedure state_changedf();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS state_changed ON public.""AppState"" CASCADE");
            migrationBuilder.Sql(@"DROP FUNCTION  IF EXISTS  state_changedf CASCADE");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandilyCommerce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Creates <c>Courses</c> and seeds the 4 initial BNCC Computação courses.
    /// Seed lives here as idempotent SQL (<c>ON CONFLICT DO NOTHING</c>), not in the model via
    /// <c>HasData</c> (removed on purpose in #21): later edits to catalog rows in Supabase are never
    /// overwritten or deleted by future migrations. <c>Axis</c> stores the enum name as text.
    /// </remarks>
    public partial class AddCourses : Migration
    {
        private const string SeedCoursesSql = """
            INSERT INTO "Courses" ("Id", "Title", "Axis", "Summary", "Audience", "WorkloadHours", "SortOrder")
            VALUES
              ('pensamento-computacional-na-pratica', 'Pensamento Computacional na prática', 'PensamentoComputacional',
               'Decomposição, padrões, abstração e algoritmos com atividades plugadas e desplugadas para a sala de aula.',
               'Professores dos anos iniciais do Ensino Fundamental', 40, 10),
              ('programacao-criativa-com-blocos', 'Programação criativa com blocos', 'PensamentoComputacional',
               'Introdução à programação visual para criar jogos, animações e histórias interativas com os estudantes.',
               'Professores do Ensino Fundamental', NULL, 20),
              ('mundo-digital-dados-e-redes', 'Mundo Digital: dados, redes e internet', 'MundoDigital',
               'Como a informação é representada, armazenada e transmitida, com exemplos do cotidiano escolar.',
               'Professores dos anos finais do Ensino Fundamental', 32, 30),
              ('cultura-digital-e-cidadania', 'Cultura Digital e cidadania', 'CulturaDigital',
               'Ética, segurança, privacidade e uso crítico das tecnologias na comunidade escolar.',
               'Professores e coordenação pedagógica', 24, 40)
            ON CONFLICT ("Id") DO NOTHING;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Axis = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Audience = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkloadHours = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SortOrder",
                table: "Courses",
                column: "SortOrder");

            migrationBuilder.Sql(SeedCoursesSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}

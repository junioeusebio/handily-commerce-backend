using HandilyCommerce.Domain.Courses;

namespace HandilyCommerce.Infrastructure.Courses;

/// <summary>
/// Static course catalog (example content) until the official list is published.
/// Swap for a persisted adapter (e.g. EF) behind <see cref="ICourseRepository"/> without changing Api/Application.
/// </summary>
public sealed class InMemoryCourseRepository : ICourseRepository
{
    private static readonly IReadOnlyList<Course> Courses =
    [
        new(
            "pensamento-computacional-na-pratica",
            "Pensamento Computacional na prática",
            BnccAxis.PensamentoComputacional,
            "Decomposição, padrões, abstração e algoritmos com atividades plugadas e desplugadas para a sala de aula.",
            "Professores dos anos iniciais do Ensino Fundamental",
            40),
        new(
            "programacao-criativa-com-blocos",
            "Programação criativa com blocos",
            BnccAxis.PensamentoComputacional,
            "Introdução à programação visual para criar jogos, animações e histórias interativas com os estudantes.",
            "Professores do Ensino Fundamental"),
        new(
            "mundo-digital-dados-e-redes",
            "Mundo Digital: dados, redes e internet",
            BnccAxis.MundoDigital,
            "Como a informação é representada, armazenada e transmitida, com exemplos do cotidiano escolar.",
            "Professores dos anos finais do Ensino Fundamental",
            32),
        new(
            "cultura-digital-e-cidadania",
            "Cultura Digital e cidadania",
            BnccAxis.CulturaDigital,
            "Ética, segurança, privacidade e uso crítico das tecnologias na comunidade escolar.",
            "Professores e coordenação pedagógica",
            24)
    ];

    public IReadOnlyList<Course> ListAll() => Courses;
}

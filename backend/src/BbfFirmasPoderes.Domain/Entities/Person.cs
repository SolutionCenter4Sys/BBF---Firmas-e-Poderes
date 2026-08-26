using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Entities;

public class Person
{
    public string PersonId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Qualificacao { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public PersonStatus Status { get; set; }

    public Document Document { get; set; } = null!;
}

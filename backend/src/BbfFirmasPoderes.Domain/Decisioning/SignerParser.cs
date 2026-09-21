using System.Text.RegularExpressions;
using BbfFirmasPoderes.Domain.Entities;

namespace BbfFirmasPoderes.Domain.Decisioning;

internal static partial class SignerParser
{
    public sealed record ResolvedSigner(string Input, Person Person, string Cargo);

    public static IReadOnlyList<ResolvedSigner> Resolve(
        IReadOnlyList<string> requested,
        IReadOnlyList<Person> people,
        out IReadOnlyList<string> unresolved)
    {
        var resolved = new List<ResolvedSigner>(requested.Count);
        var missing = new List<string>();

        foreach (var raw in requested)
        {
            var input = (raw ?? string.Empty).Trim();
            if (input.Length == 0)
            {
                missing.Add(raw ?? string.Empty);
                continue;
            }

            var parsed = Parse(input);
            var person = Match(parsed, people);
            if (person is null)
            {
                missing.Add(input);
                continue;
            }

            var cargo = !string.IsNullOrWhiteSpace(parsed.Cargo)
                ? parsed.Cargo
                : person.Cargo;
            resolved.Add(new ResolvedSigner(input, person, cargo));
        }

        unresolved = missing;
        return resolved;
    }

    private static (string? Name, string? Cargo) Parse(string input)
    {
        var match = NameAndCargo().Match(input);
        if (match.Success)
            return (match.Groups["name"].Value.Trim(), match.Groups["cargo"].Value.Trim());

        return (input, null);
    }

    private static Person? Match((string? Name, string? Cargo) parsed, IReadOnlyList<Person> people)
    {
        if (!string.IsNullOrWhiteSpace(parsed.Name) && string.IsNullOrWhiteSpace(parsed.Cargo))
        {
            var byId = people.FirstOrDefault(p =>
                string.Equals(p.PersonId, parsed.Name, StringComparison.OrdinalIgnoreCase));
            if (byId is not null)
                return byId;
        }

        if (!string.IsNullOrWhiteSpace(parsed.Name))
        {
            var byName = people.Where(p => DecisionText.NameEquals(p.Nome, parsed.Name)).ToList();
            if (byName.Count == 1)
                return byName[0];
            if (byName.Count > 1 && !string.IsNullOrWhiteSpace(parsed.Cargo))
            {
                var byBoth = byName.FirstOrDefault(p =>
                    DecisionText.RoleEquals(p.Cargo, parsed.Cargo)
                    || DecisionText.RoleEquals(p.Qualificacao, parsed.Cargo));
                if (byBoth is not null)
                    return byBoth;
            }
        }

        if (!string.IsNullOrWhiteSpace(parsed.Cargo) && string.IsNullOrWhiteSpace(parsed.Name))
        {
            var byCargo = people.Where(p =>
                DecisionText.RoleEquals(p.Cargo, parsed.Cargo)
                || DecisionText.RoleEquals(p.Qualificacao, parsed.Cargo)).ToList();
            if (byCargo.Count == 1)
                return byCargo[0];
        }

        if (!string.IsNullOrWhiteSpace(parsed.Name) && string.IsNullOrWhiteSpace(parsed.Cargo))
        {
            var byCargoOnly = people.Where(p =>
                DecisionText.RoleEquals(p.Cargo, parsed.Name)
                || DecisionText.RoleEquals(p.Qualificacao, parsed.Name)).ToList();
            if (byCargoOnly.Count == 1)
                return byCargoOnly[0];
        }

        return null;
    }

    [GeneratedRegex(@"^(?<name>.+?)\s*\((?<cargo>[^)]+)\)\s*$")]
    private static partial Regex NameAndCargo();
}

using System.Globalization;

namespace Infrastructure.Services.PrescriptionAudit;

internal static class DrugTextNormalizer
{
    private static readonly HashSet<string> DosageNoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "tab", "tabs", "tablet", "tablets",
        "cap", "caps", "capsule", "capsules",
        "sach", "sachet", "sachets",
        "drop", "drops",
        "amp", "ampoule", "ampoules", "vial",
        "syr", "syrup", "susp", "suspension",
        "cream", "ointment", "gel",
        "mg", "mcg", "g", "ml", "iu"
    };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static bool SameStrength(string? left, string? right)
    {
        var a = NormalizeStrength(left);
        var b = NormalizeStrength(right);
        return string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b;
    }

    public static bool ContainsStrength(string? extractedStrength, params string?[] fields)
    {
        if (string.IsNullOrWhiteSpace(extractedStrength))
            return true;

        var target = NormalizeStrength(extractedStrength);
        if (string.IsNullOrEmpty(target))
            return true;

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
                continue;

            var normalizedField = NormalizeStrength(field);

            if (normalizedField == target)
                return true;

            int idx = normalizedField.IndexOf(target, StringComparison.Ordinal);
            while (idx != -1)
            {
                bool startValid = idx == 0 || !char.IsDigit(normalizedField[idx - 1]);
                bool endValid = (idx + target.Length == normalizedField.Length) || !char.IsDigit(normalizedField[idx + target.Length]);

                if (startValid && endValid)
                    return true;

                idx = normalizedField.IndexOf(target, idx + 1, StringComparison.Ordinal);
            }
        }

        return false;
    }

    public static bool SameDosageForm(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        return string.IsNullOrEmpty(a)
               || string.IsNullOrEmpty(b)
               || a == b
               || a.Contains(b, StringComparison.OrdinalIgnoreCase)
               || b.Contains(a, StringComparison.OrdinalIgnoreCase)
               || AreEquivalentDosageForms(a, b);
    }

    public static bool ContainsDosageForm(string? extractedForm, params string?[] fields)
    {
        if (string.IsNullOrWhiteSpace(extractedForm))
            return true;

        var target = Normalize(extractedForm);
        if (string.IsNullOrEmpty(target))
            return true;

        var groups = new[]
        {
            new[] { "tab", "tabs", "tablet", "tablets" },
            new[] { "cap", "caps", "capsule", "capsules" },
            new[] { "sach", "sachet", "sachets" },
            new[] { "drop", "drops" },
            new[] { "amp", "ampoule", "ampoules", "vial", "vials", "injection", "injectable" },
            new[] { "syr", "syrup" },
            new[] { "susp", "suspension", "solution", "spray", "lotion", "cream", "gel", "ointment" }
        };

        var targetGroup = groups.FirstOrDefault(g => g.Contains(target)) ?? new[] { target };

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field))
                continue;

            var normalizedField = Normalize(field);

            foreach (var term in targetGroup)
            {
                if (normalizedField.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public static double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);

        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        if (a == b)
        {
            return 1;
        }

        var distance = LevenshteinDistance(a, b);
        return 1.0 - (double)distance / Math.Max(a.Length, b.Length);
    }

    public static IReadOnlyCollection<string> MeaningfulTokens(params string?[] values)
    {
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .SelectMany(v => v!.Split([' ', '-', '_', '/', '\\', '.', ',', '(', ')', '+'], StringSplitOptions.RemoveEmptyEntries))
            .Select(Normalize)
            .Where(t => t.Length >= 3 && !DosageNoiseTokens.Contains(t) && !IsMostlyNumeric(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static double TokenOverlapScore(string? extractedName, params string?[] catalogNames)
    {
        var extractedTokens = MeaningfulTokens(extractedName);
        if (extractedTokens.Count == 0)
        {
            return 0;
        }

        var catalogTokens = MeaningfulTokens(catalogNames).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (catalogTokens.Count == 0)
        {
            return 0;
        }

        var matches = extractedTokens.Count(catalogTokens.Contains);
        return (double)matches / extractedTokens.Count;
    }

    public static bool ContainsMeaningfulName(string? extractedName, string? catalogName)
    {
        var extracted = NormalizeWithoutNoise(extractedName);
        var catalog = NormalizeWithoutNoise(catalogName);

        return extracted.Length >= 4
               && catalog.Length >= 4
               && (catalog.Contains(extracted, StringComparison.OrdinalIgnoreCase)
                   || extracted.Contains(catalog, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeWithoutNoise(string? value)
    {
        return string.Concat(MeaningfulTokens(value));
    }

    private static bool AreEquivalentDosageForms(string left, string right)
    {
        var groups = new[]
        {
            new[] { "tab", "tabs", "tablet", "tablets" },
            new[] { "cap", "caps", "capsule", "capsules" },
            new[] { "sach", "sachet", "sachets" },
            new[] { "drop", "drops" },
            new[] { "amp", "ampoule", "ampoules", "vial", "injection", "injectable" },
            new[] { "syr", "syrup" },
            new[] { "susp", "suspension" }
        };

        return groups.Any(g => g.Contains(left) && g.Contains(right));
    }

    private static string NormalizeStrength(string? value)
    {
        var normalized = Normalize(value)
            .Replace("grams", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("gram", "g", StringComparison.OrdinalIgnoreCase)
            .Replace("gm", "g", StringComparison.OrdinalIgnoreCase);

        return normalized;
    }

    private static bool IsMostlyNumeric(string value)
    {
        var digitCount = value.Count(char.IsDigit);
        return digitCount > 0 && digitCount >= value.Length / 2;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        var costs = new int[target.Length + 1];
        for (var j = 0; j <= target.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= source.Length; i++)
        {
            costs[0] = i;
            var corner = i - 1;

            for (var j = 1; j <= target.Length; j++)
            {
                var upper = costs[j];
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    corner + cost);
                corner = upper;
            }
        }

        return costs[target.Length];
    }
}

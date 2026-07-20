namespace Application.Common
{
    public static class DrugCategoryMapper
    {
        private static readonly (DrugCategory Category, string[] Keywords)[] Rules =
        [
            (DrugCategory.Antibiotics, [
            "ANTIBIOTIC", "ANTIBACTERIAL", "PENICILLIN", "CEPHALOSPORIN", "MACROLIDE",
            "QUINOLONE", "AMOXICILLIN", "AMINOGLYCOSIDE", "TETRACYCLINE", "SULFONAMIDE"
        ]),
        (DrugCategory.Diabetes, [
            "DIABET", "INSULIN", "HYPOGLYCEMIC", "METFORMIN", "SULFONYLUREA"
        ]),
        (DrugCategory.BloodPressure, [
            "ANTIHYPERTENSIVE", "HYPERTENSION", "ACE INHIBITOR", "ANGIOTENSIN",
            "BETA BLOCKER", "BETA-BLOCKER", "CALCIUM CHANNEL BLOCKER", "DIURETIC"
        ]),
        (DrugCategory.Cardiovascular, [
            "CARDIAC", "CARDIOVASCULAR", "ANTIARRHYTHMIC", "ANTICOAGULANT",
            "ANTIPLATELET", "STATIN", "CHOLESTEROL", "VASODILATOR"
        ]),
        (DrugCategory.DigestiveSystem, [
            "ANTACID", "GASTRO", "LAXATIVE", "ANTIEMETIC", "ANTI-EMETIC", "PROTON PUMP",
            "DIGESTIVE", "IRRITABLE BOWEL", "CONSTIPATION", "DIARRHEA", "ULCER", "H2 ANTAGONIST"
        ]),
        (DrugCategory.AntiInflammatory, [
            "ANTI-INFLAMMATORY", "ANTIINFLAMMATORY", "NSAID", "CORTICOSTEROID", "STEROID"
        ]),
        (DrugCategory.PainRelievers, [
            "ANALGESIC", "PAIN RELIEVER", "PARACETAMOL", "ACETAMINOPHEN", "IBUPROFEN", "OPIOID"
        ]),
    ];

        public static DrugCategory Map(string? drugClass, string? genericName)
        {
            var haystack = $"{drugClass} {genericName}".ToUpperInvariant();

            foreach (var (category, keywords) in Rules)
            {
                if (keywords.Any(haystack.Contains))
                    return category;
            }

            return DrugCategory.Other;
        }
    }
}

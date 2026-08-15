-- =========================================================================================
-- PHARMALINK PRESCRIPTION REVIEWS TEST SEED SCRIPT (WITH REPEATING DRUGS)
-- Includes 10 Prescription Reviews with 5 drugs each (50 medicines in total).
-- High-frequency repeating drugs to test analytics, popularity, and pattern detection!
-- =========================================================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Target User / Pharmacist IDs
    DECLARE @PatientUser1 UNIQUEIDENTIFIER = 'FE25125F-4F29-437E-73B1-08DEF190B22C';
    DECLARE @PatientUser2 UNIQUEIDENTIFIER = 'EB5C5BA8-0910-46DB-528C-08DEF24E848E';
    DECLARE @PharmacistId UNIQUEIDENTIFIER = 'ADF9651F-5BC8-4643-68FB-08DEF22022C9';

    -- Target Drug GUID Constants
    DECLARE @Drug_Cidophage UNIQUEIDENTIFIER = '447D4D2E-03D3-46FC-2CE5-08DEF23AE66C'; -- Cidophage 500mg (Repeated 6 times)
    DECLARE @Drug_Enalapril UNIQUEIDENTIFIER = '5ECB5E07-F9AE-4B30-2C4E-08DEF23AE66C'; -- Enalapril 20mg (Repeated 5 times)
    DECLARE @Drug_Actifast UNIQUEIDENTIFIER = '0A05722F-9303-468E-2C5F-08DEF23AE66C';  -- Actifast 50mg (Repeated 5 times)
    DECLARE @Drug_Declophen UNIQUEIDENTIFIER = '175F3BF3-8C3E-4C32-2C48-08DEF23AE66C'; -- Declophen 25mg Supp (Repeated 4 times)
    DECLARE @Drug_Bisolock UNIQUEIDENTIFIER = '01A5F23C-FBF2-4985-2CD6-08DEF23AE66C';  -- Bisolock 2.5mg (Repeated 4 times)
    DECLARE @Drug_Cardioguard UNIQUEIDENTIFIER = '9E8A7822-7BA7-44DD-2C7C-08DEF23AE66C'; -- Cardioguard M SR 40mg (Repeated 3 times)
    DECLARE @Drug_NoFlu UNIQUEIDENTIFIER = 'BCDCA76A-687B-4C1B-2C56-08DEF23AE66C';     -- NoFlu Syrup 100ml (Repeated 3 times)
    DECLARE @Drug_Ringer UNIQUEIDENTIFIER = '4448911F-4159-43D2-2CD3-08DEF23AE66C';    -- Ringer's Lactate 500ml (Repeated 3 times)
    DECLARE @Drug_Dexagel UNIQUEIDENTIFIER = '919893BC-AB3F-4F05-2C49-08DEF23AE66C';   -- Dexagel Eye Gel 5gm (Repeated 3 times)
    DECLARE @Drug_EpiDinitra UNIQUEIDENTIFIER = '4938A3D6-02F1-445B-2C54-08DEF23AE66C';-- Epi-Dinitra 10mg (Repeated 2 times)
    DECLARE @Drug_Mepacure UNIQUEIDENTIFIER = '4CF0D7C6-6A98-4C14-2C57-08DEF23AE66C';  -- Mepacure 30 Tabs (Repeated 2 times)
    DECLARE @Drug_Primperan UNIQUEIDENTIFIER = 'B1AE3358-3C70-4C61-2CC7-08DEF23AE66C'; -- Primperan 10mg (Repeated 2 times)
    DECLARE @Drug_Curisafe UNIQUEIDENTIFIER = '38FC71A9-A3D2-45AE-2C5B-08DEF23AE66C';  -- Curisafe Drops (Repeated 2 times)
    DECLARE @Drug_Diabenor UNIQUEIDENTIFIER = '6E4058FB-D9AC-47E9-2C4A-08DEF23AE66C';  -- Diabenor 1mg (Repeated 2 times)

    -- Single Occurrence Drugs
    DECLARE @Drug_Trivastal UNIQUEIDENTIFIER = 'CB607742-FE28-4B8B-2C63-08DEF23AE66C';
    DECLARE @Drug_Acyclostad UNIQUEIDENTIFIER = 'C8C70B99-1B33-4F40-2C64-08DEF23AE66C';
    DECLARE @Drug_Neostigmine UNIQUEIDENTIFIER = 'FCDC59C7-E744-4978-2C73-08DEF23AE66C';
    DECLARE @Drug_ZoraC UNIQUEIDENTIFIER = '6815083B-0E77-4D38-2C9D-08DEF23AE66C';

    -- Prescription Review Unique IDs
    DECLARE @Rev1 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev2 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev3 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev4 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev5 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev6 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev7 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev8 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev9 UNIQUEIDENTIFIER = NEWID();
    DECLARE @Rev10 UNIQUEIDENTIFIER = NEWID();

    -- =========================================================================================
    -- 1. INSERT PRESCRIPTION REVIEWS (10 Reviews)
    -- =========================================================================================
    INSERT INTO [PrescriptionReviews]
    (
        [PrescriptionReviewId],
        [PatientUserId],
        [PrescriptionImagePath],
        [OriginalFileName],
        [AIModel],
        [ExtractedText],
        [AISummary],
        [ExtractionConfidence],
        [ProcessingStatus],
        [ReviewStatus],
        [PharmacistUserId],
        [ReviewNotes],
        [ReviewedAt],
        [CreatedOrderId],
        [CreatedAt],
        [UpdatedAt]
    )
    VALUES
    (
        @Rev1, @PatientUser1, N'/uploads/prescriptions/repeating_rx_01.jpg', N'rx_diabetes_hypertension_01.jpg', N'gemini-2.5-flash',
        N'روشتة ضغط وسكر: سيدوفاج 500مجم + إينالابريل 20 مجم + اكتيفاست مسكن + كارديو جارد + ديكساجيل جل للعين.',
        N'روشتة مزيج أدوية سكر وضغط ومسكن ومطهر عين.', 0.96, N'Completed', 2, @PharmacistId,
        N'تمت المراجعة بنجاح.', '2026-08-10T10:30:00.0000000', NULL, '2026-08-10T10:00:00.0000000', '2026-08-10T10:30:00.0000000'
    ),
    (
        @Rev2, @PatientUser2, N'/uploads/prescriptions/repeating_rx_02.jpg', N'rx_cardiac_pain_02.jpg', N'gemini-2.5-flash',
        N'روشتة: إينالابريل 20 مجم + ديكلوفين 25مج + بيسولوك 2.5مج + رينجر لاكتات + بريمبران 10مجم.',
        N'روشتة علاج ضغط ومسكن ومحاليل وريدية.', 0.95, N'Completed', 2, @PharmacistId,
        N'تم اعتماد الجرعات.', '2026-08-09T16:15:00.0000000', NULL, '2026-08-09T15:45:00.0000000', '2026-08-09T16:15:00.0000000'
    ),
    (
        @Rev3, @PatientUser1, N'/uploads/prescriptions/repeating_rx_03.jpg', N'rx_cold_diabetes_03.jpg', N'gemini-2.5-flash',
        N'روشتة: سيدوفاج 500مجم + اكتيفاست مسكن + نوفلو شراب + ابي داينيترا 10مجم + كيورسيف قطرة.',
        N'روشتة سكر ونزلة برد.', 0.94, N'Completed', 4, @PharmacistId,
        N'تم إنشاء الطلب.', '2026-08-08T19:00:00.0000000', NULL, '2026-08-08T18:30:00.0000000', '2026-08-08T19:00:00.0000000'
    ),
    (
        @Rev4, @PatientUser2, N'/uploads/prescriptions/repeating_rx_04.jpg', N'rx_hypertension_digestive_04.jpg', N'gemini-2.5-flash',
        N'روشتة: إينالابريل 20 مجم + ديكلوفين 25مج + كارديو جارد + ميباكيور + ديابينور 1مجم.',
        N'روشتة ضغط وسكر وهضم.', 0.91, N'PendingPharmacistReview', 1, NULL,
        NULL, NULL, NULL, '2026-08-07T14:10:00.0000000', '2026-08-07T14:10:00.0000000'
    ),
    (
        @Rev5, @PatientUser1, N'/uploads/prescriptions/repeating_rx_05.jpg', N'rx_diabetes_fluid_05.jpg', N'gemini-2.5-flash',
        N'روشتة: سيدوفاج 500مجم + اكتيفاست مسكن + بيسولوك 2.5مج + رينجر لاكتات + تريفاستال ريتارد.',
        N'روشتة كبار سن ومزمن.', 0.97, N'Completed', 2, @PharmacistId,
        N'تم الصرف بنجاح.', '2026-08-06T12:00:00.0000000', NULL, '2026-08-06T11:30:00.0000000', '2026-08-06T12:00:00.0000000'
    ),
    (
        @Rev6, @PatientUser2, N'/uploads/prescriptions/repeating_rx_06.jpg', N'rx_analgesic_heart_06.jpg', N'gemini-2.5-flash',
        N'روشتة: إينالابريل 20 مجم + اكتيفاست مسكن + نوفلو شراب + ابي داينيترا 10مجم + اسيكلو ستاد كريم.',
        N'روشتة علاجية متكاملة.', 0.93, N'NeedsPatientApproval', 1, @PharmacistId,
        N'بانتظار موافقة المريض على البديل.', '2026-08-05T17:45:00.0000000', NULL, '2026-08-05T17:15:00.0000000', '2026-08-05T17:45:00.0000000'
    ),
    (
        @Rev7, @PatientUser1, N'/uploads/prescriptions/repeating_rx_07.jpg', N'rx_chronic_care_07.jpg', N'gemini-2.5-flash',
        N'روشتة: سيدوفاج 500مجم + ديكلوفين 25مج + كارديو جارد + ميباكيور + نيوستجمين امبول.',
        N'روشتة مسكنات وأدوية قلب وعضلات.', 0.96, N'Completed', 2, @PharmacistId,
        N'تمت الموافقة وصرف الدواء.', '2026-08-04T11:20:00.0000000', NULL, '2026-08-04T10:50:00.0000000', '2026-08-04T11:20:00.0000000'
    ),
    (
        @Rev8, @PatientUser2, N'/uploads/prescriptions/repeating_rx_08.jpg', N'rx_diabetes_eye_08.jpg', N'gemini-2.5-flash',
        N'روشتة: سيدوفاج 500مجم + اكتيفاست مسكن + بيسولوك 2.5مج + ديكساجيل جل للعين + كيورسيف قطرة.',
        N'روشتة سكر وضغط وقطرات.', 0.92, N'PendingPharmacistReview', 1, NULL,
        NULL, NULL, NULL, '2026-08-03T16:00:00.0000000', '2026-08-03T16:00:00.0000000'
    ),
    (
        @Rev9, @PatientUser1, N'/uploads/prescriptions/repeating_rx_09.jpg', N'rx_hypertension_nausea_09.jpg', N'gemini-2.5-flash',
        N'روشتة: إينالابريل 20 مجم + بيسولوك 2.5مج + رينجر لاكتات + بريمبران 10مجم + ديابينور 1مجم.',
        N'روشتة ضغط وغثيان وسكر.', 0.98, N'Completed', 2, @PharmacistId,
        N'تم التدقيق والموافقة.', '2026-08-02T14:30:00.0000000', NULL, '2026-08-02T14:00:00.0000000', '2026-08-02T14:30:00.0000000'
    ),
    (
        @Rev10, @PatientUser2, N'/uploads/prescriptions/repeating_rx_10.jpg', N'rx_diabetes_fever_10.jpg', N'gemini-2.5-flash',
        N'روشتة: سيدوفاج 500مجم + ديكلوفين 25مج + نوفلو شراب + ديكساجيل جل للعين + زورا سي.',
        N'روشتة سكر وسخونية ونزلة برد.', 0.90, N'Processing', 1, @PharmacistId,
        N'جاري المعالجة.', NULL, NULL, '2026-08-01T09:00:00.0000000', '2026-08-01T09:00:00.0000000'
    );

    -- =========================================================================================
    -- 2. INSERT PRESCRIPTION REVIEW MEDICINES (50 Medicines total - 5 per Prescription)
    -- =========================================================================================
    INSERT INTO [PrescriptionReviewMedicines]
    (
        [PrescriptionReviewMedicineId], [PrescriptionReviewId], [MedicineName], [OriginalMedicineName], [GenericName],
        [Strength], [DosageForm], [Dose], [Frequency], [Duration], [Quantity], [Route], [Confidence],
        [MatchedDrugId], [SuggestedAlternativeDrugId], [MatchStatus], [MatchReason], [MatchScore], [RequiresPatientApproval], [PatientApprovedAt], [IsEdited]
    )
    VALUES
    -- Prescription 1
    (NEWID(), @Rev1, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً مع الأكل', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev1, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.97, @Drug_Enalapril, NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev1, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.95, @Drug_Actifast, NULL, N'ExactMatch', N'تطابق ممتاز', 0.96, 0, NULL, 0),
    (NEWID(), @Rev1, N'Cardioguard M SR 40mg | 10 Caps', N'كارديو جارد ام اس ار 40مجم', N'Isosorbide Mononitrate', N'40mg', N'Capsule', N'كبسولة واحدة', N'صباحاً', N'10 أيام', 1, N'Oral', 0.94, @Drug_Cardioguard, NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev1, N'Dexagel | Eye Gel | 5gm', N'ديكساجيل | جل للعين | 5جم', N'Dexamethasone', N'5gm', N'Eye Gel', N'نقطة بالعين', N'مرتين يومياً', N'7 أيام', 1, N'Ophthalmic', 0.98, @Drug_Dexagel, NULL, N'ExactMatch', N'تطابق تام', 0.98, 0, NULL, 0),

    -- Prescription 2
    (NEWID(), @Rev2, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.97, @Drug_Enalapril, NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev2, N'Declophen | Antipyretic 25mg | 5 Supp', N'ديكلوفين | خافض حرارة 25مج', N'Diclofenac Sodium', N'25mg', N'Suppository', N'لبوسة واحدة', N'عند اللزوم', N'3 أيام', 1, N'Rectal', 0.98, @Drug_Declophen, NULL, N'ExactMatch', N'تطابق كامل', 0.99, 0, NULL, 0),
    (NEWID(), @Rev2, N'Bisolock | High Blood Pressure 2.5mg | 20 Tabs', N'بيسولوك 2.5مج | 20 قرص', N'Bisoprolol', N'2.5mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.98, @Drug_Bisolock, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev2, N'Ringer''s Lactate | 500ml', N'رينجر لاكتات | 500مل', N'Ringer Lactate', N'500ml', N'IV Solution', N'عبوة محاليل', N'حقن وريدي', N'يوم واحد', 1, N'Intravenous', 0.98, @Drug_Ringer, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev2, N'Primperan Antiemetic 10mg | 10 Tabs', N'بريمبران 10مجم | 10 اقراص', N'Metoclopramide', N'10mg', N'Tablet', N'قرص واحد', N'قبل الأكل بـ 15 دقيقة', N'5 أيام', 1, N'Oral', 0.96, @Drug_Primperan, NULL, N'ExactMatch', N'تطابق سليم', 0.97, 0, NULL, 0),

    -- Prescription 3
    (NEWID(), @Rev3, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev3, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.95, @Drug_Actifast, NULL, N'ExactMatch', N'تطابق ممتاز', 0.96, 0, NULL, 0),
    (NEWID(), @Rev3, N'NoFlu Syrup | 100ml', N'نوفلو شراب | 100مل', N'Paracetamol / Pseudoephedrine', N'100ml', N'Syrup', N'10ml', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.93, @Drug_NoFlu, NULL, N'ExactMatch', N'تطابق دقيق', 0.94, 0, NULL, 0),
    (NEWID(), @Rev3, N'Epi-Dinitra 10mg | 60 Tabs', N'ابي-داينيترا 10مجم | 60 قرص', N'Isosorbide Dinitrate', N'10mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 1, N'Oral', 0.92, @Drug_EpiDinitra, NULL, N'ExactMatch', N'تطابق ناجح', 0.93, 0, NULL, 0),
    (NEWID(), @Rev3, N'Curisafe | 100Mg/1Ml | Drops | 10Ml', N'كيورسيف | قطرة | 10مل', N'Cefadroxil', N'100mg/ml', N'Drops', N'0.5ml', N'كل 12 ساعة', N'7 أيام', 1, N'Oral Drops', 0.91, @Drug_Curisafe, NULL, N'ExactMatch', N'تطابق سليم', 0.92, 0, NULL, 0),

    -- Prescription 4
    (NEWID(), @Rev4, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.97, @Drug_Enalapril, NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev4, N'Declophen | Antipyretic 25mg | 5 Supp', N'ديكلوفين | خافض حرارة 25مج', N'Diclofenac Sodium', N'25mg', N'Suppository', N'لبوسة واحدة', N'عند اللزوم', N'3 أيام', 1, N'Rectal', 0.98, @Drug_Declophen, NULL, N'ExactMatch', N'تطابق كامل', 0.99, 0, NULL, 0),
    (NEWID(), @Rev4, N'Cardioguard M SR 40mg | 10 Caps', N'كارديو جارد ام اس ار 40مجم', N'Isosorbide Mononitrate', N'40mg', N'Capsule', N'كبسولة واحدة', N'صباحاً', N'10 أيام', 1, N'Oral', 0.94, @Drug_Cardioguard, NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev4, N'Mepacure | 30 Tabs', N'ميباكيور | 30 كبسولة', N'Essential Phospholipids', N'30 Tabs', N'Capsule', N'كبسولة واحدة', N'3 مرات يومياً', N'15 يوم', 1, N'Oral', 0.90, @Drug_Mepacure, NULL, N'ExactMatch', N'تطابق ممتاز', 0.91, 0, NULL, 0),
    (NEWID(), @Rev4, N'Diabenor 1mg | 10 Tabs', N'ديابينور 1مجم | 10 اقراص', N'Glimepiride', N'1mg', N'Tablet', N'قرص واحد', N'قبل الإفطار', N'30 يوم', 2, N'Oral', 0.95, @Drug_Diabenor, NULL, N'ExactMatch', N'تطابق دقيق', 0.96, 0, NULL, 0),

    -- Prescription 5
    (NEWID(), @Rev5, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev5, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.95, @Drug_Actifast, NULL, N'ExactMatch', N'تطابق ممتاز', 0.96, 0, NULL, 0),
    (NEWID(), @Rev5, N'Bisolock | High Blood Pressure 2.5mg | 20 Tabs', N'بيسولوك 2.5مج | 20 قرص', N'Bisoprolol', N'2.5mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.98, @Drug_Bisolock, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev5, N'Ringer''s Lactate | 500ml', N'رينجر لاكتات | 500مل', N'Ringer Lactate', N'500ml', N'IV Solution', N'عبوة محاليل', N'حقن وريدي', N'يوم واحد', 1, N'Intravenous', 0.98, @Drug_Ringer, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev5, N'Trivastal Retard 20mg | 30 Tabs', N'تريفاستال ريتارد 20مجم', N'Piribedil', N'20mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'15 يوم', 1, N'Oral', 0.96, @Drug_Trivastal, NULL, N'ExactMatch', N'تطابق كلي', 0.97, 0, NULL, 0),

    -- Prescription 6
    (NEWID(), @Rev6, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.97, @Drug_Enalapril, NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev6, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.95, @Drug_Actifast, NULL, N'ExactMatch', N'تطابق ممتاز', 0.96, 0, NULL, 0),
    (NEWID(), @Rev6, N'NoFlu Syrup | 100ml', N'نوفلو شراب | 100مل', N'Paracetamol / Pseudoephedrine', N'100ml', N'Syrup', N'10ml', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.93, @Drug_NoFlu, NULL, N'ExactMatch', N'تطابق دقيق', 0.94, 0, NULL, 0),
    (NEWID(), @Rev6, N'Epi-Dinitra 10mg | 60 Tabs', N'ابي-داينيترا 10مجم | 60 قرص', N'Isosorbide Dinitrate', N'10mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 1, N'Oral', 0.92, @Drug_EpiDinitra, NULL, N'ExactMatch', N'تطابق ناجح', 0.93, 0, NULL, 0),
    (NEWID(), @Rev6, N'Acyclostad | Cream for Local Treatment 50mg | 10gm', N'اسيكلو ستاد | كريم 50مج', N'Acyclovir', N'50mg/g', N'Cream', N'دهان موضعى', N'4 مرات يومياً', N'5 أيام', 1, N'Topical', 0.95, @Drug_Acyclostad, NULL, N'ExactMatch', N'تطابق ممتاز', 0.95, 0, NULL, 0),

    -- Prescription 7
    (NEWID(), @Rev7, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev7, N'Declophen | Antipyretic 25mg | 5 Supp', N'ديكلوفين | خافض حرارة 25مج', N'Diclofenac Sodium', N'25mg', N'Suppository', N'لبوسة واحدة', N'عند اللزوم', N'3 أيام', 1, N'Rectal', 0.98, @Drug_Declophen, NULL, N'ExactMatch', N'تطابق كامل', 0.99, 0, NULL, 0),
    (NEWID(), @Rev7, N'Cardioguard M SR 40mg | 10 Caps', N'كارديو جارد ام اس ار 40مجم', N'Isosorbide Mononitrate', N'40mg', N'Capsule', N'كبسولة واحدة', N'صباحاً', N'10 أيام', 1, N'Oral', 0.94, @Drug_Cardioguard, NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev7, N'Mepacure | 30 Tabs', N'ميباكيور | 30 كبسولة', N'Essential Phospholipids', N'30 Tabs', N'Capsule', N'كبسولة واحدة', N'3 مرات يومياً', N'15 يوم', 1, N'Oral', 0.90, @Drug_Mepacure, NULL, N'ExactMatch', N'تطابق ممتاز', 0.91, 0, NULL, 0),
    (NEWID(), @Rev7, N'Neostigmine | 0.5 Mg/1Ml | 5Amp', N'نيوستجمين| 0.5 مجم/1مل', N'Neostigmine', N'0.5mg/ml', N'Ampoule', N'أمبول واحد', N'عند الحاجة', N'3 أيام', 1, N'Injection', 0.99, @Drug_Neostigmine, NULL, N'ExactMatch', N'تطابق كامل', 0.99, 0, NULL, 0),

    -- Prescription 8
    (NEWID(), @Rev8, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev8, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.95, @Drug_Actifast, NULL, N'ExactMatch', N'تطابق ممتاز', 0.96, 0, NULL, 0),
    (NEWID(), @Rev8, N'Bisolock | High Blood Pressure 2.5mg | 20 Tabs', N'بيسولوك 2.5مج | 20 قرص', N'Bisoprolol', N'2.5mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.98, @Drug_Bisolock, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev8, N'Dexagel | Eye Gel | 5gm', N'ديكساجيل | جل للعين | 5جم', N'Dexamethasone', N'5gm', N'Eye Gel', N'نقطة بالعين', N'مرتين يومياً', N'7 أيام', 1, N'Ophthalmic', 0.98, @Drug_Dexagel, NULL, N'ExactMatch', N'تطابق تام', 0.98, 0, NULL, 0),
    (NEWID(), @Rev8, N'Curisafe | 100Mg/1Ml | Drops | 10Ml', N'كيورسيف | قطرة | 10مل', N'Cefadroxil', N'100mg/ml', N'Drops', N'0.5ml', N'كل 12 ساعة', N'7 أيام', 1, N'Oral Drops', 0.91, @Drug_Curisafe, NULL, N'ExactMatch', N'تطابق سليم', 0.92, 0, NULL, 0),

    -- Prescription 9
    (NEWID(), @Rev9, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.97, @Drug_Enalapril, NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev9, N'Bisolock | High Blood Pressure 2.5mg | 20 Tabs', N'بيسولوك 2.5مج | 20 قرص', N'Bisoprolol', N'2.5mg', N'Tablet', N'قرص واحد', N'صباحاً', N'30 يوم', 1, N'Oral', 0.98, @Drug_Bisolock, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev9, N'Ringer''s Lactate | 500ml', N'رينجر لاكتات | 500مل', N'Ringer Lactate', N'500ml', N'IV Solution', N'عبوة محاليل', N'حقن وريدي', N'يوم واحد', 1, N'Intravenous', 0.98, @Drug_Ringer, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev9, N'Primperan Antiemetic 10mg | 10 Tabs', N'بريمبران 10مجم | 10 اقراص', N'Metoclopramide', N'10mg', N'Tablet', N'قرص واحد', N'قبل الأكل بـ 15 دقيقة', N'5 أيام', 1, N'Oral', 0.96, @Drug_Primperan, NULL, N'ExactMatch', N'تطابق سليم', 0.97, 0, NULL, 0),
    (NEWID(), @Rev9, N'Diabenor 1mg | 10 Tabs', N'ديابينور 1مجم | 10 اقراص', N'Glimepiride', N'1mg', N'Tablet', N'قرص واحد', N'قبل الإفطار', N'30 يوم', 2, N'Oral', 0.95, @Drug_Diabenor, NULL, N'ExactMatch', N'تطابق دقيق', 0.96, 0, NULL, 0),

    -- Prescription 10
    (NEWID(), @Rev10, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 2, N'Oral', 0.99, @Drug_Cidophage, NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev10, N'Declophen | Antipyretic 25mg | 5 Supp', N'ديكلوفين | خافض حرارة 25مج', N'Diclofenac Sodium', N'25mg', N'Suppository', N'لبوسة واحدة', N'عند اللزوم', N'3 أيام', 1, N'Rectal', 0.98, @Drug_Declophen, NULL, N'ExactMatch', N'تطابق كامل', 0.99, 0, NULL, 0),
    (NEWID(), @Rev10, N'NoFlu Syrup | 100ml', N'نوفلو شراب | 100مل', N'Paracetamol / Pseudoephedrine', N'100ml', N'Syrup', N'10ml', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.93, @Drug_NoFlu, NULL, N'ExactMatch', N'تطابق دقيق', 0.94, 0, NULL, 0),
    (NEWID(), @Rev10, N'Dexagel | Eye Gel | 5gm', N'ديكساجيل | جل للعين | 5جم', N'Dexamethasone', N'5gm', N'Eye Gel', N'نقطة بالعين', N'مرتين يومياً', N'7 أيام', 1, N'Ophthalmic', 0.98, @Drug_Dexagel, NULL, N'ExactMatch', N'تطابق تام', 0.98, 0, NULL, 0),
    (NEWID(), @Rev10, N'Zora C | 20 Lozenges | 20 Tabs', N'زورا سي | 20 قرص', N'Cetylpyridinium / Vit C', N'20 Tabs', N'Lozenge', N'قرص استحلاب', N'كل 4 ساعات', N'4 أيام', 1, N'Oral', 0.94, @Drug_ZoraC, NULL, N'ExactMatch', N'مطابقة ناجحة', 0.95, 0, NULL, 0);

    COMMIT TRANSACTION;
    PRINT N'SUCCESS: 10 Prescription Reviews with REPEATING drugs inserted successfully!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'ERROR occurred during execution:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

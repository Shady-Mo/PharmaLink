-- =========================================================================================
-- PHARMALINK PRESCRIPTION REVIEWS TEST SEED SCRIPT
-- Inserts 10 realistic Prescription Reviews with 5 distinct drugs in each (50 medicines total)
-- Testable across different statuses, times, patients, and drug combinations.
-- =========================================================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Target User / Pharmacist IDs provided
    DECLARE @PatientUser1 UNIQUEIDENTIFIER = 'F29ED51D-4928-46E4-9622-08DEF1616F56';
    DECLARE @PatientUser2 UNIQUEIDENTIFIER = 'B407AE18-4080-4ACB-68FA-08DEF22022C9';
    DECLARE @PharmacistId UNIQUEIDENTIFIER = '175F3BF3-8C3E-4C32-2C48-08DEF23AE66C';

    -- Prescription Review IDs
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
    -- Prescription 1: Approved by Pharmacist (Completed) - Patient 1
    (
        @Rev1,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_01.jpg',
        N'prescription_chronic_01.jpg',
        N'gemini-2.5-flash',
        N'روشتة عيون وضغط وسكر: ديكساجيل جل للعين + ديابينور 1مجم + إينالابريل 20مجم + انتيسيبتول غسول فم + اكتيفاست مسكن.',
        N'تتضمن الروشتة أدوية للضغط والعين والسكر ومسكن للألم. تم التأكد من الجرعات ومطابقة الأدوية بنسبة عالية.',
        0.96,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تمت المراجعة والموافقة على جميع الأصناف الخمسة بنجاح.',
        '2026-08-01T09:15:00.0000000',
        NULL,
        '2026-08-01T08:30:00.0000000',
        '2026-08-01T09:15:00.0000000'
    ),

    -- Prescription 2: Pending Pharmacist Review - Patient 2
    (
        @Rev2,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_02.jpg',
        N'prescription_cardiac_02.jpg',
        N'gemini-2.5-flash',
        N'روشتة حديثة: ابي داينيترا 10مجم + نوفلو شراب + ميباكيور 30 كبسولة + كيورسيف قطرة + ديلاي تيازيم اس ار 90مجم.',
        N'تحتوي الروشتة على أدوية قلبية ومضاد نزلات برد وقطرة عين. تتطلب مراجعة الصيدلي للتحقق من التداخلات.',
        0.91,
        N'PendingPharmacistReview',
        1, -- PendingReview
        NULL,
        NULL,
        NULL,
        NULL,
        '2026-08-02T10:15:00.0000000',
        '2026-08-02T10:15:00.0000000'
    ),

    -- Prescription 3: Approved & Order Created - Patient 1
    (
        @Rev3,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_03.jpg',
        N'prescription_gyn_03.jpg',
        N'gemini-2.5-flash',
        N'روشتة علاجية: دوفاديلان 20مجم + مونيكور بلس تحاميل + تريفاستال ريتارد 20مجم + اسيكلو ستاد كريم + نيوستجمين امبول.',
        N'تم استخراج جميع الأدوية بنجاح وتحويل الروشتة إلى طلب فعالي بعد موافقة الصيدلي.',
        0.98,
        N'Completed',
        4, -- OrderCreated
        @PharmacistId,
        N'تم التأكيد وتم إنشاء الطلب بناءً على طلب المريض.',
        '2026-08-03T13:00:00.0000000',
        NULL,
        '2026-08-03T12:45:00.0000000',
        '2026-08-03T13:00:00.0000000'
    ),

    -- Prescription 4: Rejected by Pharmacist - Patient 2
    (
        @Rev4,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_04.jpg',
        N'prescription_unclear_04.jpg',
        N'gemini-2.5-flash',
        N'روشتة غير واضحة: كيتوتي شراب + كلوترايزون كريم + كارديو جارد + كابيلا شراب + زورا سي.',
        N'الصورة غير واضحة وبها خط يد غير مقروء في بعض الأدوية، ترفض لإعادة الرفع بصورة أوضح.',
        0.54,
        N'Rejected',
        3, -- Rejected
        @PharmacistId,
        N'يرجى إعادة إرسال صورة أكثر وضوحاً للروشتة لتأكيد أسماء الأدوية والجرعات.',
        '2026-08-04T15:45:00.0000000',
        NULL,
        '2026-08-04T15:20:00.0000000',
        '2026-08-04T15:45:00.0000000'
    ),

    -- Prescription 5: Needs Patient Approval (Alternative Suggested) - Patient 1
    (
        @Rev5,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_05.jpg',
        N'prescription_alt_05.jpg',
        N'gemini-2.5-flash',
        N'روشتة: رامبيكاردين 5مجم + نيرهامايسين كريم + كليروفيت مرهم + ادويفلام أقماع + بريمبران أقراص.',
        N'تم اقتراح بدائل لبعض الأدوية غير المتوفرة حالياً، وفي انتظار موافقة المريض.',
        0.89,
        N'NeedsPatientApproval',
        1, -- PendingReview
        @PharmacistId,
        N'تم اقتراح بديل مناسب لدواء ادويفلام، بانتظار تأكيد المريض.',
        '2026-08-05T17:30:00.0000000',
        NULL,
        '2026-08-05T17:00:00.0000000',
        '2026-08-05T17:30:00.0000000'
    ),

    -- Prescription 6: Approved by Pharmacist - Patient 2
    (
        @Rev6,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_06.jpg',
        N'prescription_digestive_06.jpg',
        N'gemini-2.5-flash',
        N'روشتة جهاز هضمي ومسكنات: نيتروفيكتازول 500مجم + ميوفلكس مسكن + مينوفيللين ان + لبوس اتش فورميولا + فوليكاب 0.5مجم.',
        N'روشتة مكتملة ومطابقة بنسبة ممتازة.',
        0.95,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تمت المراجعة والموافقة.',
        '2026-08-06T10:00:00.0000000',
        NULL,
        '2026-08-06T09:40:00.0000000',
        '2026-08-06T10:00:00.0000000'
    ),

    -- Prescription 7: Pending Review (Recent Submission) - Patient 1
    (
        @Rev7,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_07.jpg',
        N'prescription_recent_07.jpg',
        N'gemini-2.5-flash',
        N'روشتة: فلوفيرمال شراب + سيلديناكس 100مجم + رينجر لاكتات + رومارين أقماع + توبمود فورت.',
        N'تم استخراج الأدوية بواسطة الذكاء الاصطناعي وفي انتظار مراجعة الصيدلي.',
        0.93,
        N'PendingPharmacistReview',
        1, -- PendingReview
        NULL,
        NULL,
        NULL,
        NULL,
        '2026-08-07T11:10:00.0000000',
        '2026-08-07T11:10:00.0000000'
    ),

    -- Prescription 8: Approved Chronic Hypertension & Asthma - Patient 2
    (
        @Rev8,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_08.jpg',
        N'prescription_asthma_08.jpg',
        N'gemini-2.5-flash',
        N'روشتة: بيسولوك 2.5مجم + برونكوفنت شراب + برومهكسين شراب + إيبيجينت امبول + أموفاج 500مجم.',
        N'روشتة لعلاج حساسية الصدر والضغط والسكر. تم التأكد من ملائمة الجرعات.',
        0.97,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'الجرعات سليمة وتمت الموافقة.',
        '2026-08-08T14:30:00.0000000',
        NULL,
        '2026-08-08T14:05:00.0000000',
        '2026-08-08T14:30:00.0000000'
    ),

    -- Prescription 9: Approved with Quantity Adjustments - Patient 1
    (
        @Rev9,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_09.jpg',
        N'prescription_dermatology_09.jpg',
        N'gemini-2.5-flash',
        N'روشتة جلدية وباطنة: ايبيكومب مرهم + اوراستاتين نقط + اورازون 0.5مجم + انشلارين شراب + الجيكاب معلق.',
        N'تم التعديل على كمية بعض الكريمات والمحاليل حسب إرشادات الروشتة.',
        0.94,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تم تعديل الكميات وتأكيد الروشتة.',
        '2026-08-09T17:00:00.0000000',
        NULL,
        '2026-08-09T16:30:00.0000000',
        '2026-08-09T17:00:00.0000000'
    ),

    -- Prescription 10: Currently Processing (Active Evaluation) - Patient 2
    (
        @Rev10,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_10.jpg',
        N'prescription_active_10.jpg',
        N'gemini-2.5-flash',
        N'روشتة: ابيريزين 5مج/5مل + رينجر محاليل + سيدوفاج 500مجم + كلينداجرام جل + نورفلكس امبول.',
        N'قيد المعالجة حالياً بواسطة فريق مراجعة الصيدلية.',
        0.90,
        N'Processing',
        1, -- PendingReview
        @PharmacistId,
        N'جاري فحص وتدقيق الجرعات قبل الموافقة النهائية.',
        NULL,
        NULL,
        '2026-08-10T18:00:00.0000000',
        '2026-08-10T18:00:00.0000000'
    );


    -- =========================================================================================
    -- 2. INSERT PRESCRIPTION REVIEW MEDICINES (50 Medicines - 5 per Prescription)
    -- =========================================================================================
    INSERT INTO [PrescriptionReviewMedicines]
    (
        [PrescriptionReviewMedicineId],
        [PrescriptionReviewId],
        [MedicineName],
        [OriginalMedicineName],
        [GenericName],
        [Strength],
        [DosageForm],
        [Dose],
        [Frequency],
        [Duration],
        [Quantity],
        [Route],
        [Confidence],
        [MatchedDrugId],
        [SuggestedAlternativeDrugId],
        [MatchStatus],
        [MatchReason],
        [MatchScore],
        [RequiresPatientApproval],
        [PatientApprovedAt],
        [IsEdited]
    )
    VALUES
    -- -----------------------------------------------------------------------------------------
    -- Prescription 1 Medicines (Review 1)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev1, N'Dexagel | Eye Gel | 5gm', N'ديكساجيل | جل للعين | 5جم', N'Dexamethasone Eye Gel', N'5gm', N'Eye Gel', N'نقطة واحدة', N'مرتين يومياً', N'7 أيام', 1, N'Ophthalmic', 0.98, '919893BC-AB3F-4F05-2C49-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام مع الاسم والتصنيع', 0.99, 0, NULL, 0),
    (NEWID(), @Rev1, N'Diabenor 1mg | 10 Tabs', N'ديابينور 1مجم | 10 اقراص', N'Glimepiride', N'1mg', N'Tablet', N'قرص واحد', N'قبل الإفطار', N'30 يوم', 2, N'Oral', 0.96, '6E4058FB-D9AC-47E9-2C4A-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة البيانات', 0.97, 0, NULL, 0),
    (NEWID(), @Rev1, N'Enalapril 20mg | 28Tabs', N'إينالابريل 20 مجم | 28 قرص', N'Enalapril Maleate', N'20mg', N'Tablet', N'قرص واحد', N'مرة يومياً صباحاً', N'30 يوم', 1, N'Oral', 0.97, '5ECB5E07-F9AE-4B30-2C4E-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev1, N'Antiseptol 1mg/ml Mouth Wash | 120ml', N'انتيسيبتول 1مجم/مل غسول الفم | 120مل', N'Chlorhexidine', N'1mg/ml', N'Mouth Wash', N'15ml مضمضة', N'3 مرات يومياً', N'5 أيام', 1, N'Oral Rinse', 0.95, 'C1FBE766-F8AB-4EB4-2C51-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.96, 0, NULL, 0),
    (NEWID(), @Rev1, N'Actifast | Pain Reliever 50mg | 6 Sachets', N'اكتيفاست | مسكن للألم 50مج | 6 اكياس', N'Diclofenac Potassium', N'50mg', N'Sachet', N'كيس واحد على نصف كوب ماء', N'عند اللزوم', N'3 أيام', 1, N'Oral', 0.94, '0A05722F-9303-468E-2C5F-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.95, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 2 Medicines (Review 2)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev2, N'Epi-Dinitra 10mg | 60 Tabs', N'ابي-داينيترا 10مجم | 60 قرص', N'Isosorbide Dinitrate', N'10mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'30 يوم', 1, N'Oral', 0.92, '4938A3D6-02F1-445B-2C54-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة بيانات الأدوية', 0.94, 0, NULL, 0),
    (NEWID(), @Rev2, N'NoFlu Syrup | 100ml', N'نوفلو شراب | 100مل', N'Paracetamol / Pseudoephedrine', N'100ml', N'Syrup', N'10ml', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.91, 'BCDCA76A-687B-4C1B-2C56-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.93, 0, NULL, 0),
    (NEWID(), @Rev2, N'Mepacure | 30 Tabs', N'ميباكيور | 30 كبسولة', N'Essential Phospholipids', N'30 Tabs', N'Capsule', N'كبسولة واحدة', N'3 مرات يومياً بعد الأكل', N'15 يوم', 1, N'Oral', 0.89, '4CF0D7C6-6A98-4C14-2C57-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع الدواء المخزون', 0.91, 0, NULL, 0),
    (NEWID(), @Rev2, N'Curisafe | 100Mg/1Ml | Drops | 10Ml', N'كيورسيف | قطرة | 10مل', N'Cefadroxil', N'100mg/ml', N'Drops', N'0.5ml', N'كل 12 ساعة', N'7 أيام', 1, N'Oral Drops', 0.90, '38FC71A9-A3D2-45AE-2C5B-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مباشر', 0.92, 0, NULL, 0),
    (NEWID(), @Rev2, N'Delay Tiazem SR 90mg | 10 Caps', N'ديلاي تيازيم اس ار 90مجم | 10 كبسولة', N'Diltiazem HCl', N'90mg', N'Capsule', N'كبسولة واحدة', N'مرة يومياً', N'10 أيام', 1, N'Oral', 0.93, '6D661F57-42E1-4649-2C5C-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.94, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 3 Medicines (Review 3)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev3, N'Duvadilan | 20mg | 30Tab', N'دوفاديلان | 20مجم | 30قرص', N'Isoxsuprine HCl', N'20mg', N'Tablet', N'قرص واحد', N'3 مرات يومياً', N'10 أيام', 1, N'Oral', 0.98, '9C13ECD9-CF87-402B-2C5D-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام مع الاسم المقروء', 0.98, 0, NULL, 0),
    (NEWID(), @Rev3, N'Monicure Plus | 7 Vaginal suppositories', N'مونيكور بلس | 7 تحاميل مهبلية', N'Miconazole Nitrate', N'7 Supp', N'Suppository', N'تحميلة واحدة', N'مساءً قبل النوم', N'7 أيام', 1, N'Vaginal', 0.97, 'AA0FF0D9-A27F-4E80-2C60-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ناجحة', 0.97, 0, NULL, 0),
    (NEWID(), @Rev3, N'Trivastal Retard Paralysis and Blood Clots 20mg | 30 Tabs', N'تريفاستال ريتارد 20مجم | 30 قرص', N'Piribedil', N'20mg', N'Tablet', N'قرص واحد', N'مرتين يومياً بعد الأكل', N'15 يوم', 1, N'Oral', 0.96, 'CB607742-FE28-4B8B-2C63-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كلي', 0.96, 0, NULL, 0),
    (NEWID(), @Rev3, N'Acyclostad | Cream for Local Treatment 50mg | 10gm', N'اسيكلو ستاد | كريم 50مج | 10جم', N'Acyclovir', N'50mg/g', N'Cream', N'دهان موضعى', N'4 مرات يومياً', N'5 أيام', 1, N'Topical', 0.95, 'C8C70B99-1B33-4F40-2C64-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.95, 0, NULL, 0),
    (NEWID(), @Rev3, N'Neostigmine | 0.5 Mg/1Ml | 5Amp', N'نيوستجمين| 0.5 مجم/1مل | 5امبول', N'Neostigmine Methylsulfate', N'0.5mg/ml', N'Ampoule', N'أمبول واحد', N'حسب إرشادات الطبيب', N'عند الحاجة', 1, N'Injection', 0.99, 'FCDC59C7-E744-4978-2C73-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة البيانات', 0.99, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 4 Medicines (Review 4 - Rejected)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev4, N'Ketoti | Pharco 1mg/5ml Syrup | 120ml', N'كيتوتي | فاركو 1 مجم/5 مل شراب', N'Ketotifen', N'1mg/5ml', N'Syrup', N'5ml', N'مرتين يومياً', N'7 أيام', 1, N'Oral', 0.65, 'D687A0B1-2CE2-4560-2C79-08DEF23AE66C', NULL, N'FuzzyMatch', N'قراءة غير واضحة من الخط اليدوي', 0.68, 0, NULL, 0),
    (NEWID(), @Rev4, N'Clotrisone | Cream | 15gm', N'كلوترايزون | كريم | 15جم', N'Clotrimazole / Betamethasone', N'15gm', N'Cream', N'دهان خفيف', N'مرتين يومياً', N'5 أيام', 1, N'Topical', 0.58, '64FE57E3-51D8-47A2-2C7A-08DEF23AE66C', NULL, N'FuzzyMatch', N'اسم غير جلي بالكامل', 0.60, 0, NULL, 0),
    (NEWID(), @Rev4, N'Cardioguard M SR 40mg | 10 Caps', N'كارديو جارد ام اس ار 40مجم', N'Isosorbide Mononitrate', N'40mg', N'Capsule', N'كبسولة واحدة', N'صباحاً', N'10 أيام', 1, N'Oral', 0.52, '9E8A7822-7BA7-44DD-2C7C-08DEF23AE66C', NULL, N'NotFound', N'تعذر تأكيد الجرعة بدقة من الروشتة', 0.55, 0, NULL, 0),
    (NEWID(), @Rev4, N'Cabella | Syrup | 125ml', N'كابيلا | شراب | 125مل', N'Herbals Cough Formula', N'125ml', N'Syrup', N'10ml', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.60, 'EA226900-B937-46EA-2C7D-08DEF23AE66C', NULL, N'FuzzyMatch', N'خط مطمس جزئياً', 0.62, 0, NULL, 0),
    (NEWID(), @Rev4, N'Zora C | 20 Lozenges | 20 Tabs', N'زورا سي | 20 قرص', N'Cetylpyridinium / Vit C', N'20 Tabs', N'Lozenge', N'قرص استحلاب', N'كل 4 ساعات', N'4 أيام', 1, N'Oral', 0.70, '6815083B-0E77-4D38-2C9D-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة اسم الدواء التجاري', 0.72, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 5 Medicines (Review 5 - Needs Approval & Alternative)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev5, N'Rampecardin | 5mg | 7 tabs', N'رامبيكاردين | 5 مجم | 7 اقراص', N'Ramipril', N'5mg', N'Tablet', N'قرص واحد', N'صباحاً', N'7 أيام', 1, N'Oral', 0.94, '93A7B413-F0D8-4E2D-2C9F-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ناجحة', 0.95, 0, NULL, 0),
    (NEWID(), @Rev5, N'Nerhamicin 0.3% | Cream | 15gm', N'نيرهامايسين كريم | 15 جم', N'Gentamicin', N'0.3%', N'Cream', N'دهان موضعى', N'3 مرات يومياً', N'7 أيام', 1, N'Topical', 0.96, '67C9A18E-1AF6-4EAF-2CC4-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.96, 0, NULL, 0),
    (NEWID(), @Rev5, N'Clerovate | Oint - 0.05% | 25gm', N'كليروفيت | مرهم - 0.05٪ | 25جرام', N'Clobetasol Propionate', N'0.05%', N'Ointment', N'طبقة رقيقة', N'مرتين يومياً', N'5 أيام', 1, N'Topical', 0.95, '3B58A5EA-630A-4224-2CC5-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev5, N'Adwiflam | 75Mg | 5Suppositories', N'ادويفلام | 75مجم | 5اقماع', N'Diclofenac Potassium', N'75mg', N'Suppository', N'قمع واحد', N'عند الشدة', N'5 أيام', 1, N'Rectal', 0.88, 'DFB44C36-5987-4035-2CC6-08DEF23AE66C', '175F3BF3-8C3E-4C32-2C48-08DEF23AE66C', N'AlternativeSuggested', N'الصنف المطلوب غير متوفر بالصيدلية وتم اقتراح ديكلوفين كبديل بنفس المادة', 0.85, 1, NULL, 0),
    (NEWID(), @Rev5, N'Primperan Antiemetic 10mg | 10 Tabs', N'بريمبران 10مجم | 10 اقراص', N'Metoclopramide HCl', N'10mg', N'Tablet', N'قرص واحد', N'قبل الأكل بـ 15 دقيقة', N'5 أيام', 1, N'Oral', 0.97, 'B1AE3358-3C70-4C61-2CC7-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع الدواء المخزون', 0.97, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 6 Medicines (Review 6)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev6, N'NItrofectazole 500mg | 20 Tabs', N'نيتروفيكتازول 500مجم | 20 قرص', N'Nitazoxanide', N'500mg', N'Tablet', N'قرص واحد', N'كل 12 ساعة بعد الأكل', N'3 أيام', 1, N'Oral', 0.95, '408B4F2F-F3A7-4002-2CC8-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة تامة', 0.96, 0, NULL, 0),
    (NEWID(), @Rev6, N'Myoflex | Pain reliever | 10 tablets', N'ميوفلكس | مسكن للآلم | 10أقراص', N'Chlorzoxazone / Paracetamol', N'10 Tabs', N'Tablet', N'قرص واحد', N'3 مرات يومياً', N'5 أيام', 1, N'Oral', 0.96, 'BB406639-4ED8-4A87-2CCA-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.97, 0, NULL, 0),
    (NEWID(), @Rev6, N'Minophylline N | 500Mg | 5Amp', N'مينوفيللين ان | 500مجم | 5امبول', N'Aminophylline', N'500mg', N'Ampoule', N'أمبول في الوريد', N'حسب الحاجة', N'3 أيام', 1, N'Injection', 0.93, '658C7D1F-CAE6-45B5-2CCB-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.94, 0, NULL, 0),
    (NEWID(), @Rev6, N'H Formula | 6Supp', N'لبوس اتش فورميولا | 6 لبوس', N'Dibucaine / Phenylephrine', N'6 Supp', N'Suppository', N'لبوسة واحدة', N'صباحاً ومساءً', N'6 أيام', 1, N'Rectal', 0.94, 'C91014BF-FA42-4ED7-2CCC-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ممتازة', 0.95, 0, NULL, 0),
    (NEWID(), @Rev6, N'Folicap 0.5mg | 24 Caps', N'فوليكاب 0.5مج | 24 كبسولة', N'Folic Acid', N'0.5mg', N'Capsule', N'كبسولة واحدة', N'مرة يومياً', N'30 يوم', 1, N'Oral', 0.97, '81BC2B7A-6DBC-43AD-2CD0-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.98, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 7 Medicines (Review 7 - Pending)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev7, N'Fluvermal 20mg/ml Suspension | 30ml', N'فلوفيرمال 20مجم/مل شراب معلق', N'Flubendazole', N'20mg/ml', N'Suspension', N'5ml', N'مرة واحدة وتكرر بعد أسبوعين', N'يومان', 1, N'Oral', 0.93, '9A06FE1C-9DBE-4BE9-2CD1-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع الذكاء الاصطناعي', 0.94, 0, NULL, 0),
    (NEWID(), @Rev7, N'Sildinax 100mg | 3 Tabs', N'سيلديناكس 100مجم | 3 اقراص', N'Sildenafil', N'100mg', N'Tablet', N'قرص واحد', N'عند الحاجة قبل الجماع بساعة', N'عند اللزوم', 1, N'Oral', 0.91, '387CFA38-B90B-45DB-2CD2-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق الاسم التجاري', 0.92, 0, NULL, 0),
    (NEWID(), @Rev7, N'Ringer''s Lactate | 500ml', N'رينجر لاكتات | 500مل', N'Sodium Chloride / Potassium / Lactate', N'500ml', N'IV Solution', N'زجاجة محلول واحدة', N'بالتنقيط الوريدي', N'يوم واحد', 2, N'Intravenous', 0.98, '4448911F-4159-43D2-2CD3-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev7, N'Rheumarene 100mg | 6Supp', N'رومارين 100مجم | 6أقماع', N'Diclofenac Sodium', N'100mg', N'Suppository', N'قمع شرجي', N'مرة واحدة قبل النوم', N'5 أيام', 1, N'Rectal', 0.92, 'D45612FF-7D19-402B-2CD4-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ناجح', 0.93, 0, NULL, 0),
    (NEWID(), @Rev7, N'Topmode Forte | Stomach Ulcers | 200 mg | 10 Tab', N'توبمود فورت | 200 مج | 10 اقراص', N'Sulpiride', N'200mg', N'Tablet', N'قرص واحد', N'مرتين يومياً قبل الأكل', N'10 أيام', 1, N'Oral', 0.95, 'B0878B87-129F-45F6-2CD5-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة البيانات', 0.95, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 8 Medicines (Review 8)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev8, N'Bisolock | High Blood Pressure 2.5mg | 20 Tabs', N'بيسولوك 2.5مج | 20 قرص', N'Bisoprolol Fumarate', N'2.5mg', N'Tablet', N'قرص واحد', N'مرة واحدة صباحاً', N'30 يوم', 1, N'Oral', 0.98, '01A5F23C-FBF2-4985-2CD6-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev8, N'Bronchovent | Oral Syrup | 125ml', N'برونكوفنت | شراب | 125مل', N'Salbutamol', N'125ml', N'Syrup', N'5ml', N'3 مرات يومياً', N'7 أيام', 1, N'Oral', 0.96, 'AB755AB1-BC75-41D7-2CD7-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة سريعة', 0.97, 0, NULL, 0),
    (NEWID(), @Rev8, N'Bromhexine | Syrup | 120ml', N'برومهكسين | شراب | 120مل', N'Bromhexine HCl', N'120ml', N'Syrup', N'10ml', N'3 مرات يومياً بعد الأكل', N'7 أيام', 1, N'Oral', 0.95, 'CF3B249C-7052-4924-2CD8-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.96, 0, NULL, 0),
    (NEWID(), @Rev8, N'Epigent | 20Mg | 3Amp', N'إيبيجينت | 20مجم | 3امبول', N'Gentamicin Sulfate', N'20mg', N'Ampoule', N'حقنة بالفيال', N'كل 12 ساعة', N'3 أيام', 1, N'Injection', 0.94, 'E9C02B63-EC69-48DA-2CD9-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كلي', 0.95, 0, NULL, 0),
    (NEWID(), @Rev8, N'Amophage | 500mg | 30Tab', N'أموفاج | 500مجم | 30قرص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً وسط الأكل', N'30 يوم', 2, N'Oral', 0.97, '0470A1CC-839E-4894-2CDA-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.98, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 9 Medicines (Review 9 - Edited Quantities)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev9, N'Epicomb | Topical Ointment | 15gm', N'ايبيكومب | مرهم موضعي | 15جم', N'Triamcinolone / Nystatin', N'15gm', N'Ointment', N'طبقة رقيقة دهان', N'مرتين يومياً', N'7 أيام', 2, N'Topical', 0.95, 'CE358682-21E6-43A5-2CDB-08DEF23AE66C', NULL, N'ExactMatch', N'تم زيادة العبوات حسب مساحة الجلد المتأثرة', 0.96, 0, NULL, 1),
    (NEWID(), @Rev9, N'Orastatin | 100.000 i.u. / ml oral drops | 30Ml', N'اوراستاتين | نقط فموية | 30مل', N'Nystatin', N'100,000 IU/ml', N'Oral Drops', N'1ml قطارة', N'4 مرات يومياً بعد الأكل', N'7 أيام', 1, N'Oral Drops', 0.94, '64845573-633C-4C22-2CDC-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة تامة', 0.95, 0, NULL, 0),
    (NEWID(), @Rev9, N'Orazone | To Treat Allergies | 0.5 mg | 20 Tab', N'اورازون | 0.5 مج | 20 قرص', N'Dexamethasone', N'0.5mg', N'Tablet', N'قرص واحد', N'مرة يومياً صباحاً', N'5 أيام', 1, N'Oral', 0.96, 'FF0DA45F-F9BE-4FD7-2CDD-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.97, 0, NULL, 0),
    (NEWID(), @Rev9, N'Anschlarin | Syrup | 120ml', N'انشلارين | شرب | 120مل', N'Desloratadine', N'120ml', N'Syrup', N'5ml', N'مرة واحدة قبل النوم', N'10 أيام', 1, N'Oral', 0.92, '433B7FFA-8677-4601-2CDE-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة سريعة', 0.93, 0, NULL, 0),
    (NEWID(), @Rev9, N'Algicab Suspension | 120ml', N'الجيكاب معلق فموى | 120مل', N'Sodium Alginate / Bicarbonate', N'120ml', N'Suspension', N'10ml', N'بعد الوجبات وعند النوم', N'7 أيام', 1, N'Oral', 0.93, 'F066CA09-8AA6-4E99-2CDF-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.94, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 10 Medicines (Review 10 - Processing)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev10, N'Epirizine 5mg/5ml | 60ml', N'ابيريزين 5مج/5مل | 60مل', N'Cetirizine Dihydrochloride', N'5mg/5ml', N'Syrup', N'5ml', N'مرة واحدة يومياً مساءً', N'7 أيام', 1, N'Oral', 0.92, '3CCC0EBA-341D-4C3D-2CE1-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق قيد المراجعة', 0.93, 0, NULL, 0),
    (NEWID(), @Rev10, N'Ringer IV Infusion | 500 ml', N'Ringer IV Infusion | 500 ml', N'Ringer Solution', N'500ml', N'IV Solution', N'عبوة محاليل', N'حقن وريدي بطيء', N'يوم واحد', 1, N'Intravenous', 0.97, '1FCCDC62-C2FA-4BEF-2CE2-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تكتيكي', 0.98, 0, NULL, 0),
    (NEWID(), @Rev10, N'Cidophage | 500 mg | 10 Tabs', N'سيدوفاج | 500 مجم | 10 أقراص', N'Metformin HCl', N'500mg', N'Tablet', N'قرص واحد', N'مرتين يومياً مع الوجبات', N'15 يوم', 2, N'Oral', 0.99, '447D4D2E-03D3-46FC-2CE5-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كامل ومؤكد', 0.99, 0, NULL, 0),
    (NEWID(), @Rev10, N'Clindagram | Gel | 30GM', N'كلينداجرام | جل | 30 جم', N'Clindamycin Phosphate', N'30g', N'Gel', N'دهان موضعى للحبوب', N'مرتين يومياً', N'14 يوم', 1, N'Topical', 0.95, '2F2892D8-3A32-4C52-2CE6-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev10, N'Norflex | 30 mg/ml | 3 Ampoules', N'نورفلكس | 30مج/مل | 3 امبول', N'Orphenadrine Citrate', N'30mg/ml', N'Ampoule', N'أمبول عضل عند اللزوم', N'مرة يومياً', N'3 أيام', 1, N'Intramuscular', 0.96, '9747DFA4-D568-417C-2CE7-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة دقيقة', 0.96, 0, NULL, 0);

    COMMIT TRANSACTION;
    PRINT N'SUCCESS: 10 Prescription Reviews and 50 Prescription Review Medicines inserted successfully!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'ERROR occurred during execution:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

-- =========================================================================================
-- PHARMALINK PRESCRIPTION REVIEWS TEST SEED SCRIPT #2
-- Target Patient User IDs:
--   - FE25125F-4F29-437E-73B1-08DEF190B22C
--   - EB5C5BA8-0910-46DB-528C-08DEF24E848E
-- Target Pharmacist ID:
--   - ADF9651F-5BC8-4643-68FB-08DEF22022C9
--
-- Inserts 10 NEW Prescription Reviews with 5 distinct drugs in each (50 NEW medicines total)
-- =========================================================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- Target User / Pharmacist IDs provided
    DECLARE @PatientUser1 UNIQUEIDENTIFIER = 'FE25125F-4F29-437E-73B1-08DEF190B22C';
    DECLARE @PatientUser2 UNIQUEIDENTIFIER = 'EB5C5BA8-0910-46DB-528C-08DEF24E848E';
    DECLARE @PharmacistId UNIQUEIDENTIFIER = 'ADF9651F-5BC8-4643-68FB-08DEF22022C9';

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
    -- Prescription 1: Approved by Pharmacist (Completed) - Patient 1
    (
        @Rev1,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_2_01.jpg',
        N'prescription_fever_pain_01.jpg',
        N'gemini-2.5-flash',
        N'روشتة خافض حرارة ومسكنات وعيون: ديكلوفين لبوس 25مج + بيرال 0.5مل + بوكسينات نقط عيون + ابيفيناك 12.5مج + ابيفيناك 25مج أقماع.',
        N'تتضمن الروشتة أدوية خافضة للحرارة ومسكنات وأقماع للأطفال والبالغين ونقط للعين. تمت مراجعة الجرعات بدقة.',
        0.97,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تمت مراجعة الروشتة والموافقة على جميع الأصناف وصرفها للمريض.',
        '2026-08-10T14:30:00.0000000',
        NULL,
        '2026-08-10T14:00:00.0000000',
        '2026-08-10T14:30:00.0000000'
    ),

    -- Prescription 2: Pending Pharmacist Review - Patient 2
    (
        @Rev2,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_2_02.jpg',
        N'prescription_new_patient2_02.jpg',
        N'gemini-2.5-flash',
        N'روشتة حديثة: ابي داينيترا 10مجم + مايكلون 20 قرص + ماش بريمير غسول وجه + فاتيكا زيت شعر + ويف هاند جيل.',
        N'روشتة تحتوي على دواء للقلب وأصناف عناية شخصية. بانتظار فحص الصيدلي.',
        0.92,
        N'PendingPharmacistReview',
        1, -- PendingReview
        NULL,
        NULL,
        NULL,
        NULL,
        '2026-08-10T11:30:00.0000000',
        '2026-08-10T11:30:00.0000000'
    ),

    -- Prescription 3: Approved & Order Created - Patient 1
    (
        @Rev3,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_2_03.jpg',
        N'prescription_firstaid_03.jpg',
        N'gemini-2.5-flash',
        N'روشتة غيارات وإسعافات: كيس جمع بول للأطفال + كير ريدي مسحات قطنية + قطن طبي 50جم + قطن جراحي 50جم + فرست بي بي تشيك اختبار حمل.',
        N'مستلزمات إسعافية واختبارات تم تأكيدها وتحويلها لطلب.',
        0.99,
        N'Completed',
        4, -- OrderCreated
        @PharmacistId,
        N'تم تأكيد المستلزمات وإنشاء الطلب بنجاح.',
        '2026-08-09T19:15:00.0000000',
        NULL,
        '2026-08-09T18:45:00.0000000',
        '2026-08-09T19:15:00.0000000'
    ),

    -- Prescription 4: Rejected by Pharmacist - Patient 2
    (
        @Rev4,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_2_04.jpg',
        N'prescription_blur_04.jpg',
        N'gemini-2.5-flash',
        N'روشتة غير واضحة: ضمادة فارمابور + فاتيكا حمام كريم جنين القمح + فاتيكا كريم شعر + فاتيكا سبايك جل + فاتيكا حمام كريم باللوز.',
        N'الصورة غير واضحة وبها انعكاس ضوئي شديد يمنع قراءة التعليمات.',
        0.58,
        N'Rejected',
        3, -- Rejected
        @PharmacistId,
        N'مرفوضة لعدم وضوح الصورة. يرجى إعادة تصوير الروشتة في إضاءة جيدة.',
        '2026-08-08T16:45:00.0000000',
        NULL,
        '2026-08-08T16:15:00.0000000',
        '2026-08-08T16:45:00.0000000'
    ),

    -- Prescription 5: Needs Patient Approval (Alternative Suggested) - Patient 1
    (
        @Rev5,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_2_05.jpg',
        N'prescription_hair_care_05.jpg',
        N'gemini-2.5-flash',
        N'روشتة عناية وتغذية شعر: فاتيكا حمام كريم تساقط + فاتيكا حمام كريم بالثوم + فاتيكا حمام كريم بحبة البركة + فاتيكا حمام كريم تغذية + فاتيكا أرجان.',
        N'تم اقتراح حجم عبوة بديل متوفر بالصيدلية لبديل فاتيكا بحبة البركة وفي انتظار تأكيد المريض.',
        0.88,
        N'NeedsPatientApproval',
        1, -- PendingReview
        @PharmacistId,
        N'تم اقتراح البديل المناسب للعبوات المتاحة حالياً، بانتظار موافقة المريض.',
        '2026-08-07T13:30:00.0000000',
        NULL,
        '2026-08-07T13:00:00.0000000',
        '2026-08-07T13:30:00.0000000'
    ),

    -- Prescription 6: Approved by Pharmacist - Patient 2
    (
        @Rev6,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_2_06.jpg',
        N'prescription_dressing_06.jpg',
        N'gemini-2.5-flash',
        N'روشتة غيار جروح وتجميل: ضمادة فور إم + صانسيلك زيت شعر + شيستي لورد كريم أوكسجين + شاش فازلين بيوتول + شاش جمال تكس 10سم.',
        N'مستلزمات غيار جروح وعناية. تم التأكد من المقاسات والموافقة عليها.',
        0.96,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تمت المراجعة والموافقة على جميع الأصناف.',
        '2026-08-06T09:50:00.0000000',
        NULL,
        '2026-08-06T09:20:00.0000000',
        '2026-08-06T09:50:00.0000000'
    ),

    -- Prescription 7: Pending Pharmacist Review - Patient 1
    (
        @Rev7,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_2_07.jpg',
        N'prescription_medical_supplies_07.jpg',
        N'gemini-2.5-flash',
        N'روشتة مستلزمات: سيلك بلاست شريط حرير + سيرجباد 10*20سم + سي ام اى قطن + سوبر لورد ماء اكسجين 20 + سوبر لورد ماء اكسجين 10.',
        N'روشتة مطهرات ومستلزمات جراحية قيد انتظار مراجعة الصيدلي.',
        0.94,
        N'PendingPharmacistReview',
        1, -- PendingReview
        NULL,
        NULL,
        NULL,
        NULL,
        '2026-08-05T15:50:00.0000000',
        '2026-08-05T15:50:00.0000000'
    ),

    -- Prescription 8: Approved Syringes & Solutions - Patient 2
    (
        @Rev8,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_2_08.jpg',
        N'prescription_syringes_08.jpg',
        N'gemini-2.5-flash',
        N'روشتة حقن ومحاليل: زولا جهاز نقل محاليل + حقنة انسولين كوري 100 وحدة + حقنة 10مل + حقنة 20مل + جلسرين 60مل.',
        N'أدوات ومستلزمات محاليل وحقن أنسولين تم مراجعتها والموافقة عليها.',
        0.98,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'تم مراجعة الأنواع والموافقة.',
        '2026-08-04T11:10:00.0000000',
        NULL,
        '2026-08-04T10:40:00.0000000',
        '2026-08-04T11:10:00.0000000'
    ),

    -- Prescription 9: Approved Cough & Antibacterial - Patient 1
    (
        @Rev9,
        @PatientUser1,
        N'/uploads/prescriptions/rx_test_2_09.jpg',
        N'prescription_cough_skin_09.jpg',
        N'gemini-2.5-flash',
        N'روشتة: بيكتول استحلاب بالكرز + بيك حقن أنسولين + بيتاكلوتري كريم + بولين فورت شراب + بوريك اسيد غسول.',
        N'روشتة كحّة وحساسية ومسكنات استحلاب مع قطن ومطهر.',
        0.95,
        N'Completed',
        2, -- Approved
        @PharmacistId,
        N'الجرعات سليمة ومطابقة.',
        '2026-08-03T12:45:00.0000000',
        NULL,
        '2026-08-03T12:15:00.0000000',
        '2026-08-03T12:45:00.0000000'
    ),

    -- Prescription 10: Currently Processing - Patient 2
    (
        @Rev10,
        @PatientUser2,
        N'/uploads/prescriptions/rx_test_2_10.jpg',
        N'prescription_processing_10.jpg',
        N'gemini-2.5-flash',
        N'روشتة: بليس ليف ان شيا + بليس ليف ان أرجان + برفكت كيميكال ماء اكسجين + أوركاسين نقط عيون + أرثوباد رباط تحت الجبس.',
        N'جارٍ التدقيق والمراجعة من الصيدلي المكلف.',
        0.91,
        N'Processing',
        1, -- PendingReview
        @PharmacistId,
        N'قيد الفحص والمراجعة الحالية.',
        NULL,
        NULL,
        '2026-08-02T08:30:00.0000000',
        '2026-08-02T08:30:00.0000000'
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
    (NEWID(), @Rev1, N'Declophen | Antipyretic 25mg | 5 Supp', N'ديكلوفين | خافض حرارة 25مج | 5 لبوس', N'Diclofenac Sodium', N'25mg', N'Suppository', N'لبوسة واحدة', N'عند اللزوم أو عند ارتفاع الحرارة', N'3 أيام', 1, N'Rectal', 0.98, '175F3BF3-8C3E-4C32-2C48-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كامل ومؤكد', 0.99, 0, NULL, 0),
    (NEWID(), @Rev1, N'Pyral | 0.5Mg | 20Tab', N'بيرال | 0.5مل | 20قرص', N'Alprazolam', N'0.5mg', N'Tablet', N'قرص واحد', N'قبل النوم', N'7 أيام', 1, N'Oral', 0.95, 'C8A7EB2E-361B-43C5-2C4C-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.96, 0, NULL, 0),
    (NEWID(), @Rev1, N'BOXINATE | 0.4 % E . DROPS | 10ML', N'بوكسينات | نقط للعين | 10مل', N'Oxymetazoline HCl', N'0.4%', N'Eye Drops', N'نقطتان بالعين', N'3 مرات يومياً', N'5 أيام', 1, N'Ophthalmic', 0.96, '6DF290F8-B743-4ED4-2C4D-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.97, 0, NULL, 0),
    (NEWID(), @Rev1, N'Epifenac | 12.5 mg | 5 suppositories', N'ابيفيناك | 12.5 مجم | 5 اقماع', N'Diclofenac Potassium', N'12.5mg', N'Suppository', N'قمع واحد', N'كل 12 ساعة عند الحاجة', N'3 أيام', 1, N'Rectal', 0.97, '894D3233-B587-4758-2C52-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.98, 0, NULL, 0),
    (NEWID(), @Rev1, N'Epifenac 25mg | 5 Suppositories', N'ابيفيناك 25مجم | 5 اقماع شرجية', N'Diclofenac Potassium', N'25mg', N'Suppository', N'قمع واحد شرجي', N'مساءً', N'5 أيام', 1, N'Rectal', 0.94, 'A635D6E0-4771-47B6-2C53-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة البيانات', 0.95, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 2 Medicines (Review 2)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev2, N'Epi Dinitra 10mg | 20 Tabs', N'ابي داينيترا 10مجم | 20 قرص', N'Isosorbide Dinitrate', N'10mg', N'Tablet', N'قرص واحد', N'مرتين يومياً', N'10 أيام', 1, N'Oral', 0.93, '3B769975-C09E-4EF4-2C55-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة سريعة', 0.94, 0, NULL, 0),
    (NEWID(), @Rev2, N'Michaelon | 20 Tablet', N'مايكلون | 20 قرص', N'Miconazole', N'20 Tabs', N'Tablet', N'قرص واحد', N'بعد الإفطار', N'10 أيام', 1, N'Oral', 0.91, '93B96709-86E3-4835-2C58-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.92, 0, NULL, 0),
    (NEWID(), @Rev2, N'Mash Premiere | Ultra Fair Facial Wash | 350ml', N'ماش بريمير | ألترا فير غسول للوجه', N'Facial Cleanser Formula', N'350ml', N'Facial Wash', N'غسيل الوجه', N'مرتين يومياً', N'استخدام يومي', 1, N'Topical', 0.98, 'DA197471-D734-49C3-2C61-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق المنتج التجميلي', 0.99, 0, NULL, 0),
    (NEWID(), @Rev2, N'Vatika | Hair Oil Aloe Vera | 45ml', N'فاتيكا | زيت الشعر بالالوفيرا | 45مل', N'Aloe Vera Hair Oil', N'45ml', N'Hair Oil', N'دهان الشعر', N'مرة يومياً', N'استخدام مستمر', 1, N'Topical', 0.96, '4C45B5B6-A4A1-4191-2C62-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.97, 0, NULL, 0),
    (NEWID(), @Rev2, N'Wave | Hand Gel | 70ml', N'ويف | هاند جيل | 70مل', N'Alcohol Hand Sanitizer Gel', N'70ml', N'Hand Gel', N'تطهير اليدين', N'عند الحاجة', N'مستمر', 1, N'Topical', 0.95, '19D3B65A-7CAD-4E07-2C66-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.96, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 3 Medicines (Review 3)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev3, N'Paediatric Urine Collector for Kids | 100ml', N'كيس جمع بول للاطفال | 100مل', N'Medical Urine Collector Bag', N'100ml', N'Medical Device', N'استعمال واحد', N'حسب الحاجة', N'يوم واحد', 3, N'External', 0.99, 'A1E7DB2E-9478-4CF6-2C77-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ناجحة للمستلزمات', 0.99, 0, NULL, 0),
    (NEWID(), @Rev3, N'Care Ready | Swabs 10x10cm Plain | 4 Ply', N'كير ريدي | مسحات قطنية 10×10سم', N'Cotton Gauze Swabs', N'10x10cm', N'Swabs', N'مسحة واحدة', N'عند غيار الجرح', N'5 أيام', 2, N'Topical', 0.97, '56599E93-1BB0-43BF-2C78-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.98, 0, NULL, 0),
    (NEWID(), @Rev3, N'Medical Cotton | 50gm', N'قطن طبي | 50جم', N'Pure Absorbent Cotton', N'50gm', N'Cotton Pack', N'حسب الحاجة', N'عند التطهير', N'مستمر', 1, N'Topical', 0.98, '7A8DE5BC-49D5-4959-2C7F-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.99, 0, NULL, 0),
    (NEWID(), @Rev3, N'Surgical Cotton | 50gm', N'قطن جراحي | 50جم', N'Sterile Surgical Cotton', N'50gm', N'Cotton Pack', N'حسب الحاجة', N'عند الجراحة أو التطهير', N'مستمر', 1, N'Topical', 0.96, '5792D986-C2CD-4D58-2C80-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كلي', 0.97, 0, NULL, 0),
    (NEWID(), @Rev3, N'First BP Check | One Step Pregnancy Test | 1 Pcs', N'فرست بي بي تشيك | اختبار حمل', N'HCG Pregnancy Test Strip', N'1 Pcs', N'Test Kit', N'اختبار واحد', N'مرة واحدة صباحاً', N'يوم واحد', 1, N'In Vitro Test', 0.99, 'FC301376-D6F6-4D7E-2C84-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مؤكد', 0.99, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 4 Medicines (Review 4 - Rejected)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev4, N'Pharmapore | Adhesive Wound Dressing 10 x 10cm', N'فارمابور | ضمادة لاصقة للجروح', N'Adhesive Island Dressing', N'10x10cm', N'Dressing', N'ضمادة واحدة', N'يومياً عند الغيار', N'5 أيام', 1, N'Topical', 0.62, 'E8EB547F-C541-4AF0-2C85-08DEF23AE66C', NULL, N'FuzzyMatch', N'اسم غير محدد الأبعاد بوضوح بالصورة', 0.65, 0, NULL, 0),
    (NEWID(), @Rev4, N'Vatika Naturals | Hammam Cream Wheatgerm | 35gm', N'فاتيكا ناتشورالز | حمام كريم جنين القمح', N'Wheatgerm Cream Bath', N'35gm', N'Hammam Cream', N'دهان الشعر', N'مرتين أسبوعياً', N'شهر', 1, N'Topical', 0.59, '9E6B3A2D-0553-4915-2C86-08DEF23AE66C', NULL, N'FuzzyMatch', N'صورة باهتة', 0.61, 0, NULL, 0),
    (NEWID(), @Rev4, N'Vatika | Hair Cream with Aloe Vera & Olive Oil | 30ml', N'فاتيكا | كريم شعر بالصبار والزيتون', N'Aloe & Olive Hair Cream', N'30ml', N'Hair Cream', N'دهان يومي', N'مرة يومياً', N'مستمر', 1, N'Topical', 0.64, 'BC808EC4-F19B-4689-2C87-08DEF23AE66C', NULL, N'FuzzyMatch', N'قراءة طفيفة غير جليّة', 0.66, 0, NULL, 0),
    (NEWID(), @Rev4, N'Vatika | Spike Up Styling Gel | 100ml', N'فاتيكا | سبايك اب جل الشعر', N'Styling Hair Gel', N'100ml', N'Hair Gel', N'لتثبيت الشعر', N'عند الخروج', N'مستمر', 1, N'Topical', 0.55, '9BA92653-F083-4B11-2C88-08DEF23AE66C', NULL, N'NotFound', N'خط يد غير مكتمل التوصيف', 0.57, 0, NULL, 0),
    (NEWID(), @Rev4, N'Vatika | Hair Conditioning Cream Almond | 35gm', N'فاتيكا | حمام كريم للشعر باللوز', N'Almond Hair Conditioner', N'35gm', N'Hammam Cream', N'حمام كريم', N'مرة أسبوعياً', N'شهر', 1, N'Topical', 0.60, 'F35A2331-9A2C-442E-2C89-08DEF23AE66C', NULL, N'FuzzyMatch', N'تشابه أسماء', 0.62, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 5 Medicines (Review 5 - Alternative Suggested)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev5, N'Vatika | Naturals Hair Fall Control Hammam Cream | 35gm', N'فاتيكا | حمام كريم للتحكم في تساقط الشعر', N'Hair Fall Control Cream', N'35gm', N'Hammam Cream', N'تدليك الفروة', N'مرتين أسبوعياً', N'شهر', 1, N'Topical', 0.94, 'C3EAE5CF-B129-4160-2C8A-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة سليمة', 0.95, 0, NULL, 0),
    (NEWID(), @Rev5, N'Vatika | Naturals Hammam Garlic Treatment Cream | 35gm', N'فاتيكا | حمام كريم دابر للشعر بخلاصة الثوم', N'Garlic Hair Treatment Cream', N'35gm', N'Hammam Cream', N'حمام كريم', N'مرة أسبوعياً', N'شهر', 1, N'Topical', 0.93, 'FDA2A962-1C6D-4AF6-2C8B-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتازة', 0.94, 0, NULL, 0),
    (NEWID(), @Rev5, N'Vatika | Naturals Hammam Black Seed | 35gm', N'فاتيكا | حمام كريم بحبة البركة', N'Black Seed Hair Cream', N'35gm', N'Hammam Cream', N'دهان بعد الاستحمام', N'مرتين أسبوعياً', N'شهر', 1, N'Topical', 0.86, '45000FED-7F86-482C-2C8C-08DEF23AE66C', '9384AAD5-8DD0-4257-2C8D-08DEF23AE66C', N'AlternativeSuggested', N'تم اقتراح عبوة فاتيكا لتغذية مكثفة كبديل متاح حالياً بالصيدلية', 0.84, 1, NULL, 0),
    (NEWID(), @Rev5, N'Vatika | Hair Cond HM Cream | 35gm', N'فاتيكا | حمام كريم الشعر لتغذية مكثفة', N'Nourishing Hair Cream', N'35gm', N'Hammam Cream', N'حمام كريم', N'مرة أسبوعياً', N'شهر', 1, N'Topical', 0.95, '9384AAD5-8DD0-4257-2C8D-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.96, 0, NULL, 0),
    (NEWID(), @Rev5, N'Vatika | Argan Hair Cream Bath | 35gm', N'فاتيكا | حمام كربم أرجان | 35جم', N'Argan Hair Cream Bath', N'35gm', N'Hammam Cream', N'حمام زيت وكريم', N'مرة أسبوعياً', N'شهر', 1, N'Topical', 0.96, '80F39DB8-D57B-4672-2C8E-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.97, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 6 Medicines (Review 6)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev6, N'Dressing Four M | Post Operative Dressing Mid 10*35', N'ضمادة فور إم | ضمادة جراحية 10*35', N'Surgical Post-Op Bandage', N'10x35cm', N'Dressing', N'ضمادة واحدة', N'تغيير يومي', N'5 أيام', 1, N'Topical', 0.97, '23C0AE49-44C4-44F5-2C90-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة كاملة', 0.98, 0, NULL, 0),
    (NEWID(), @Rev6, N'Sunsilk | Anti Hairfall Oil | 75ml', N'صانسيلك | زيت مضاد لتساقط الشعر | 75مل', N'Anti Hairfall Hair Oil', N'75ml', N'Hair Oil', N'دهان الفروة', N'مرة يومياً', N'شهر', 1, N'Topical', 0.96, '4354A1F4-90AC-4CDA-2C91-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.97, 0, NULL, 0),
    (NEWID(), @Rev6, N'Shesty Lord | Oxygen Cream 30%', N'شيستي لورد | كريم الأكسجين 30%', N'Hydrogen Peroxide Cream 30%', N'30%', N'Cream', N'دهان تفتيح خفيف', N'حسب الحاجة', N'عند الاستخدام', 1, N'Topical', 0.94, 'AC0912D7-F03D-4A91-2C92-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.95, 0, NULL, 0),
    (NEWID(), @Rev6, N'Bio Tulle 10*10 Vaseline Gauze', N'شاش فازلين بيوتول 10*10', N'Petrolatum Gauze Dressing', N'10x10cm', N'Vaseline Gauze', N'طبقة واحدة على الجرح', N'يومياً', N'5 أيام', 2, N'Topical', 0.98, '0138B876-4060-4518-2C93-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ممتازة', 0.99, 0, NULL, 0),
    (NEWID(), @Rev6, N'Gamal Tex Gauze | 10cm', N'شاش جمال تكس | 10سم', N'Medical Gauze Roll', N'10cm', N'Gauze Roll', N'لفافة واحدة', N'عند الغيار', N'5 أيام', 1, N'Topical', 0.95, 'BD0424AD-2AFF-40CE-2C94-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.96, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 7 Medicines (Review 7 - Pending)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev7, N'Silkplast | Adhesive Tape | 12.5cm * 2m', N'سيلك بلاست | شريط حرير طبي لاصق', N'Medical Silk Tape', N'1.25cm x 2m', N'Adhesive Tape', N'تثبيت الشاش', N'عند الحاجة', N'مستمر', 1, N'Topical', 0.95, '54341EC6-85A5-4EAD-2C95-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق مع قاعدة بيانات المستلزمات', 0.96, 0, NULL, 0),
    (NEWID(), @Rev7, N'Surgipad | 10Cm/20Cm', N'سيرجباد | 10 سم / 20 سم', N'Abdominal Sterile Pad', N'10x20cm', N'Surgical Pad', N'وسادة جراحية واحدة', N'مرتين يومياً', N'5 أيام', 1, N'Topical', 0.93, 'C665CC8C-1837-4DB0-2C96-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ناجح', 0.94, 0, NULL, 0),
    (NEWID(), @Rev7, N'CMI | Cotton | 50gm', N'سي ام اى | قطن | 50جم', N'Medical Cotton 50g', N'50gm', N'Cotton Pack', N'تطهير الجرح', N'3 مرات يومياً', N'5 أيام', 1, N'Topical', 0.96, 'B2B9C558-DFF9-4C23-2C97-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة تامة', 0.97, 0, NULL, 0),
    (NEWID(), @Rev7, N'Super Lord | Hydrogen Peroxide 20 Vol | 80ml', N'سوبر لورد | ماء اكسجين تركيز 20 | 80مل', N'Hydrogen Peroxide 20 Vol', N'20 Vol', N'Liquid Solution', N'تطهير موضعي', N'مرتين يومياً', N'3 أيام', 1, N'Topical', 0.94, 'F4CBD0D3-2AB2-406D-2C99-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0),
    (NEWID(), @Rev7, N'Super Lord | Hydrogen Peroxide 10 Vol | 80ml', N'سوبر لورد | ماء اكسجين تركيز 10 | 80مل', N'Hydrogen Peroxide 10 Vol', N'10 Vol', N'Liquid Solution', N'محلول مطهر', N'عند الحاجة', N'3 أيام', 1, N'Topical', 0.92, '417771D1-303E-4AE1-2C9A-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.93, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 8 Medicines (Review 8)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev8, N'Zola Solution Device Tube | 1 Pc', N'زولا جهاز نقل محاليل وريدي | 1 قطعة', N'IV Infusion Set Tube', N'1 Pc', N'Medical Tube Device', N'جهاز محاليل كامل', N'مرة واحدة', N'يوم واحد', 2, N'Intravenous', 0.99, 'FD6FFC2C-E14D-40DD-2C9C-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة تامة للجهاز الجراحي', 0.99, 0, NULL, 0),
    (NEWID(), @Rev8, N'Syringe 100 unit insulin Korean | 10 syringes', N'حقنة أنسولين سعة 100 وحدة كوري | 10 حقن', N'Insulin Syringe 100U', N'100 Unit', N'Syringe Pack', N'حقنة واحدة مع كل جرعة أنسولين', N'يومياً', N'10 أيام', 1, N'Subcutaneous', 0.98, '8F47D632-23E5-4A44-2CA4-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.99, 0, NULL, 0),
    (NEWID(), @Rev8, N'Disposable Syringe | 10ml', N'حقنة استخدام مرة | 10مل', N'Single Use Syringe 10ml', N'10ml', N'Syringe', N'حقنة واحدة', N'عند الحاجة', N'عند اللزوم', 3, N'Injection', 0.97, 'F45B7B06-36BF-4948-2CA5-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.98, 0, NULL, 0),
    (NEWID(), @Rev8, N'Disposable Syringe 20ml', N'حقنة 20مل', N'Single Use Syringe 20ml', N'20ml', N'Syringe', N'حقنة 20مل لجمع المحاليل', N'مرة واحدة', N'عند الحاجة', 2, N'Injection', 0.96, '08682EBB-0F9F-491A-2CA6-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة ناجحة', 0.97, 0, NULL, 0),
    (NEWID(), @Rev8, N'Glycerin | 60ml', N'جلسرين | 60مل', N'Glycerin Pure Liquid', N'60ml', N'Liquid Solution', N'دهان ترطيب', N'مرتين يومياً', N'مستمر', 1, N'Topical', 0.95, 'FBC4CDC5-5630-4B5C-2CA8-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.96, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 9 Medicines (Review 9)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev9, N'Pectol | Lozenges With Cherry Flavour + Vitamin C', N'بيكتول | استحلاب بنكهة الكرز + فيتامين سي', N'Cetylpyridinium / Vitamin C', N'Cherry Flavour', N'Lozenge', N'قرص استحلاب بالفم', N'كل 3 ساعات', N'5 أيام', 1, N'Oral', 0.96, 'DCAC58A9-B127-42D1-2CAD-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كلي', 0.97, 0, NULL, 0),
    (NEWID(), @Rev9, N'Pic | Solution Insumed 30gm 8mm Insulin Syringes', N'بيك | حقن أنسولين 30جم 8مم | 30 قطعة', N'Pic Insumed Insulin Needles', N'30g 8mm', N'Syringe Pack', N'إبرة أنسولين', N'مع كل جرعة أنسولين', N'30 يوم', 1, N'Subcutaneous', 0.98, '18EC5C7F-6662-4DCF-2CAE-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق كامل ومؤكد', 0.99, 0, NULL, 0),
    (NEWID(), @Rev9, N'Betaclotri | Cream | 15gm', N'بيتاكلوتري | كريم | 15جم', N'Betamethasone / Clotrimazole', N'15gm', N'Cream', N'طبقة خفيفة دهان', N'مرتين يومياً', N'7 أيام', 1, N'Topical', 0.95, 'D40F7865-879B-4ECB-2CAF-08DEF23AE66C', NULL, N'ExactMatch', N'مطابقة سريعة', 0.96, 0, NULL, 0),
    (NEWID(), @Rev9, N'Pauline Forte Syrup', N'بولين فورت شراب', N'Multivitamin Formula Syrup', N'120ml', N'Syrup', N'ملعقة كبيرة 10ml', N'مرة واحدة بعد الغداء', N'15 يوم', 1, N'Oral', 0.93, '8AF9548D-03DA-4E75-2CB0-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.94, 0, NULL, 0),
    (NEWID(), @Rev9, N'Boric Acid | 2% Lotion | 60ml', N'بوريك اسيد | 2٪ غسول | 60مل', N'Boric Acid Topical Solution 2%', N'60ml', N'Lotion', N'غسيل موضعي للجلد', N'مرتين يومياً', N'5 أيام', 1, N'Topical', 0.94, '8C074405-B989-417D-2CB1-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق دقيق', 0.95, 0, NULL, 0),

    -- -----------------------------------------------------------------------------------------
    -- Prescription 10 Medicines (Review 10 - Processing)
    -- -----------------------------------------------------------------------------------------
    (NEWID(), @Rev10, N'Bless | Leave in Cream with Shea Butter Sachets', N'بليس | أكياس ليف ان كريم بزبدة الشيا', N'Shea Butter Hair Cream', N'35ml', N'Cream Sachet', N'كيس واحد دهان للشعر', N'مرتين أسبوعياً', N'شهر', 2, N'Topical', 0.93, 'A66E32BD-FBDD-47ED-2CB2-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق قيد الفحص', 0.94, 0, NULL, 0),
    (NEWID(), @Rev10, N'Bless | Leave in Cream with Argan Oil Sachets', N'بليس | أكياس ليف ان كريم بزيت الأرجان', N'Argan Oil Hair Cream', N'35ml', N'Cream Sachet', N'كيس واحد لترطيب الشعر', N'مرتين أسبوعياً', N'شهر', 2, N'Topical', 0.92, 'E36A5336-CA47-47FA-2CB3-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق قيد التدقيق', 0.93, 0, NULL, 0),
    (NEWID(), @Rev10, N'Perfect Chemical | Hydrogen Peroxide 10% | 100ml', N'برفكت كيميكال | ماء اكسجين بتركيز 10%', N'Hydrogen Peroxide 10%', N'100ml', N'Liquid Solution', N'تطهير الجروح', N'عند الحاجة', N'3 أيام', 1, N'Topical', 0.95, '9B9759FD-56DC-45B3-2CB4-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق تام', 0.96, 0, NULL, 0),
    (NEWID(), @Rev10, N'Orchacin 0.3 % Eye Drops | 5Ml', N'أوركاسين 0.3٪ نقط للعين | 5مل', N'Norfloxacin Ophthalmic', N'0.3%', N'Eye Drops', N'نقطة بالعين المصابة', N'كل 8 ساعات', N'7 أيام', 1, N'Ophthalmic', 0.96, 'BFB7FFBD-4A07-409F-2CB7-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق ممتاز', 0.97, 0, NULL, 0),
    (NEWID(), @Rev10, N'Orthopad | Undercast Padding | 7.5 cm * 2.7m', N'أرثوباد | رباط تحت الجبس وغيار جروح', N'Undercast Synthetic Padding', N'7.5cm x 2.7m', N'Padding Roll', N'لفافة واحدة تحت الجبيرة', N'عند التجبير', N'شهر', 1, N'Topical', 0.94, '338AFC13-EC6B-4746-2CB8-08DEF23AE66C', NULL, N'ExactMatch', N'تطابق سليم', 0.95, 0, NULL, 0);

    COMMIT TRANSACTION;
    PRINT N'SUCCESS: 10 NEW Prescription Reviews and 50 NEW Prescription Review Medicines inserted successfully!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'ERROR occurred during execution:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;

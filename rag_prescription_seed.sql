-- ==========================================================================
-- PHARMALINK RAG PRESCRIPTION SEED SCRIPT (20 Realistic Prescriptions)
-- Target Pharmacies & Branches:
-- Pharmacy 1: d81ff936-461f-423c-a110-f4eedc82d988
--   Branches: E8FC6117-ECCC-496C-9B9F-222129D3E600 (مدينة نصر)
--             044294EA-F757-455E-8926-3511AADFE6B5 (المعادي)
--             B559D3F2-A1FD-4A9E-8246-3E415434D703 (مصر الجديدة)
--
-- Pharmacy 2: 652B27FB-BE52-4E67-8DAE-D0EA3E5D4F30
--   Branches: 378D1E32-87EB-4048-BFE3-02E2E01AD4BC (سموحة - الإسكندرية)
--             15B1A403-209F-482D-B9A9-0322EE8FD34C (ميامي - الإسكندرية)
--             CE2BEAB4-580F-4D66-9335-0391B7287506 (الدقي - الجيزة)
--             9AF2E50F-BBFC-49ED-867E-04E7812C0419 (الشيخ زايد)
-- ==========================================================================

BEGIN TRANSACTION;
BEGIN TRY

    -- 1. Resolve or Create a valid Patient User ID
    DECLARE @PatientUserId UNIQUEIDENTIFIER;
    SELECT TOP 1 @PatientUserId = [Id] FROM [AspNetUsers];

    IF @PatientUserId IS NULL
    BEGIN
        SET @PatientUserId = NEWID();
        INSERT INTO [AspNetUsers] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount])
        VALUES (@PatientUserId, 'patient_test@pharmalink.eg', 'PATIENT_TEST@PHARMALINK.EG', 'patient_test@pharmalink.eg', 'PATIENT_TEST@PHARMALINK.EG', 1, NEWID(), NEWID(), 1, 0, 1, 0);

        INSERT INTO [Patients] ([Id]) VALUES (@PatientUserId);
    END;

    -- Helper table for 20 sample prescriptions
    DECLARE @PrescriptionTable TABLE (
        Seq INT IDENTITY(1,1),
        ReviewId UNIQUEIDENTIFIER,
        BranchId UNIQUEIDENTIFIER,
        City NVARCHAR(100),
        Governorate NVARCHAR(100),
        IndexedText NVARCHAR(MAX),
        MedicinesJson NVARCHAR(MAX),
        IsPediatric BIT,
        CreatedAt DATETIME2,
        Med1Name NVARCHAR(200), Med1Generic NVARCHAR(200), Med1Form NVARCHAR(100), Med1Strength NVARCHAR(50), Med1Qty INT,
        Med2Name NVARCHAR(200), Med2Generic NVARCHAR(200), Med2Form NVARCHAR(100), Med2Strength NVARCHAR(50), Med2Qty INT
    );

    -- ── 20 Realistic Sample Prescriptions (Diabetes, Pediatric, Antibiotics, Chronic) ──
    INSERT INTO @PrescriptionTable (ReviewId, BranchId, City, Governorate, IndexedText, MedicinesJson, IsPediatric, CreatedAt, Med1Name, Med1Generic, Med1Form, Med1Strength, Med1Qty, Med2Name, Med2Generic, Med2Form, Med2Strength, Med2Qty)
    VALUES
    -- 1. Diabetes (Glucophage + Janumet) - مدينة نصر
    (NEWID(), 'E8FC6117-ECCC-496C-9B9F-222129D3E600', N'مدينة نصر', N'القاهرة',
     N'روشتة علاج سكر وضغط: Glucophage 1000mg قرص مرتين يومياً بعد الأكل + Janumet 50/1000mg قرص صباحاً ومساءً.',
     N'[{"MedicineName":"Glucophage","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"1000mg","Quantity":2,"IsPediatric":false},{"MedicineName":"Janumet","GenericName":"Sitagliptin/Metformin","DosageForm":"Tablet","Strength":"50/1000mg","Quantity":2,"IsPediatric":false}]',
     0, DATEADD(DAY, -1, GETUTCDATE()), N'Glucophage', N'Metformin HCl', N'Tablet', N'1000mg', 2, N'Janumet', N'Sitagliptin/Metformin', N'Tablet', N'50/1000mg', 2),

    -- 2. Diabetes (Amaryl + Metformin) - سموحة الإسكندرية
    (NEWID(), '378D1E32-87EB-4048-BFE3-02E2E01AD4BC', N'سموحة', N'الإسكندرية',
     N'روشتة سكر: Amaryl 3mg قرص قبل الإفطار + Metformin 500mg قرص ثلاث مرات يومياً.',
     N'[{"MedicineName":"Amaryl","GenericName":"Glimepiride","DosageForm":"Tablet","Strength":"3mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Metformin","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"500mg","Quantity":3,"IsPediatric":false}]',
     0, DATEADD(DAY, -2, GETUTCDATE()), N'Amaryl', N'Glimepiride', N'Tablet', N'3mg', 1, N'Metformin', N'Metformin HCl', N'Tablet', N'500mg', 3),

    -- 3. Pediatric (Congestal Syrup + Cefotax) - مدينة نصر
    (NEWID(), 'E8FC6117-ECCC-496C-9B9F-222129D3E600', N'مدينة نصر', N'القاهرة',
     N'روشتة أطفال بنزلة برد شديدة: Congestal Pediatric Syrup 5ml كل 8 ساعات + Cefotax 500mg حقنة كل 12 ساعة.',
     N'[{"MedicineName":"Congestal Syrup","GenericName":"Paracetamol/Pseudoephedrine","DosageForm":"Syrup","Strength":"120ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Cefotax","GenericName":"Cefotaxime","DosageForm":"Vial","Strength":"500mg","Quantity":2,"IsPediatric":true}]',
     1, DATEADD(DAY, -2, GETUTCDATE()), N'Congestal Syrup', N'Paracetamol/Pseudoephedrine', N'Syrup', N'120ml', 1, N'Cefotax', N'Cefotaxime', N'Vial', N'500mg', 2),

    -- 4. Pediatric (Augmentin Syrup + Panadol Baby) - المعادي
    (NEWID(), '044294EA-F757-455E-8926-3511AADFE6B5', N'المعادي', N'القاهرة',
     N'روشتة التهاب أذن للأطفال: Augmentin 457mg Syrup 5ml مرتين يومياً + Panadol Baby Drops عند اللزوم.',
     N'[{"MedicineName":"Augmentin Syrup","GenericName":"Amoxicillin/Clavulanic Acid","DosageForm":"Syrup","Strength":"457mg","Quantity":1,"IsPediatric":true},{"MedicineName":"Panadol Baby","GenericName":"Paracetamol","DosageForm":"Drops","Strength":"100mg/ml","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -3, GETUTCDATE()), N'Augmentin Syrup', N'Amoxicillin/Clavulanic Acid', N'Syrup', N'457mg', 1, N'Panadol Baby', N'Paracetamol', N'Drops', N'100mg/ml', 1),

    -- 5. Chronic Heart & Cholesterol (Concor + Atorstat) - مصر الجديدة
    (NEWID(), 'B559D3F2-A1FD-4A9E-8246-3E415434D703', N'مصر الجديدة', N'القاهرة',
     N'روشتة ضغط وكولسترول: Concor 5mg قرص صباحاً + Atorstat 20mg قرص مساءً.',
     N'[{"MedicineName":"Concor","GenericName":"Bisoprolol","DosageForm":"Tablet","Strength":"5mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Atorstat","GenericName":"Atorvastatin","DosageForm":"Tablet","Strength":"20mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -3, GETUTCDATE()), N'Concor', N'Bisoprolol', N'Tablet', N'5mg', 1, N'Atorstat', N'Atorvastatin', N'Tablet', N'20mg', 1),

    -- 6. Diabetes (Diamicron + Glucophage) - ميامي الإسكندرية
    (NEWID(), '15B1A403-209F-482D-B9A9-0322EE8FD34C', N'ميامي', N'الإسكندرية',
     N'روشتة سكر مزمن: Diamicron MR 60mg قرص مع الإفطار + Glucophage 850mg قرص بعد العشاء.',
     N'[{"MedicineName":"Diamicron MR","GenericName":"Gliclazide","DosageForm":"Tablet","Strength":"60mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Glucophage","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"850mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -4, GETUTCDATE()), N'Diamicron MR', N'Gliclazide', N'Tablet', N'60mg', 1, N'Glucophage', N'Metformin HCl', N'Tablet', N'850mg', 1),

    -- 7. Antibiotic & Anti-inflammatory (Augmentin 1g + Alphintern) - الدقي الجيزة
    (NEWID(), 'CE2BEAB4-580F-4D66-9335-0391B7287506', N'الدقي', N'الجيزة',
     N'روشتة اسنان وحلق: Augmentin 1g قرص كل 12 ساعة + Alphintern 2 قرص قبل الأكل بـ 30 دقيقة.',
     N'[{"MedicineName":"Augmentin","GenericName":"Amoxicillin/Clavulanic Acid","DosageForm":"Tablet","Strength":"1g","Quantity":2,"IsPediatric":false},{"MedicineName":"Alphintern","GenericName":"Chymotrypsin/Trypsin","DosageForm":"Tablet","Strength":"Standard","Quantity":3,"IsPediatric":false}]',
     0, DATEADD(DAY, -4, GETUTCDATE()), N'Augmentin', N'Amoxicillin/Clavulanic Acid', N'Tablet', N'1g', 2, N'Alphintern', N'Chymotrypsin/Trypsin', N'Tablet', N'Standard', 3),

    -- 8. Pediatric (Catafly Syrup + Cefzil Syrup) - الشيخ زايد
    (NEWID(), '9AF2E50F-BBFC-49ED-867E-04E7812C0419', N'الشيخ زايد', N'الجيزة',
     N'روشتة أطفال سخونية واحتقان: Catafly Syrup 5ml عند اللزوم + Cefzil 250mg Syrup 5ml كل 12 ساعة.',
     N'[{"MedicineName":"Catafly Syrup","GenericName":"Potassium Diclofenac","DosageForm":"Syrup","Strength":"140ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Cefzil Syrup","GenericName":"Cefprozil","DosageForm":"Syrup","Strength":"250mg","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -5, GETUTCDATE()), N'Catafly Syrup', N'Potassium Diclofenac', N'Syrup', N'140ml', 1, N'Cefzil Syrup', N'Cefprozil', N'Syrup', N'250mg', 1),

    -- 9. Diabetes & Hypertension (Forxiga + Exforge) - المعادي
    (NEWID(), '044294EA-F757-455E-8926-3511AADFE6B5', N'المعادي', N'القاهرة',
     N'روشتة كلى وسكر وضغط: Forxiga 10mg قرص صباحاً + Exforge 5/160mg قرص مساءً.',
     N'[{"MedicineName":"Forxiga","GenericName":"Dapagliflozin","DosageForm":"Tablet","Strength":"10mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Exforge","GenericName":"Amlodipine/Valsartan","DosageForm":"Tablet","Strength":"5/160mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -5, GETUTCDATE()), N'Forxiga', N'Dapagliflozin', N'Tablet', N'10mg', 1, N'Exforge', N'Amlodipine/Valsartan', N'Tablet', N'5/160mg', 1),

    -- 10. Pediatric (Pediacort + Zithromax Syrup) - سموحة الإسكندرية
    (NEWID(), '378D1E32-87EB-4048-BFE3-02E2E01AD4BC', N'سموحة', N'الإسكندرية',
     N'روشتة حساسية صدر أطفال: Pediacort Syrup 2.5ml صباحاً + Zithromax 200mg/5ml Syrup مرة واحدة يومياً.',
     N'[{"MedicineName":"Pediacort Syrup","GenericName":"Prednisolone","DosageForm":"Syrup","Strength":"15mg/5ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Zithromax Syrup","GenericName":"Azithromycin","DosageForm":"Syrup","Strength":"200mg/5ml","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -6, GETUTCDATE()), N'Pediacort Syrup', N'Prednisolone', N'Syrup', N'15mg/5ml', 1, N'Zithromax Syrup', N'Azithromycin', N'Syrup', N'200mg/5ml', 1),

    -- 11. Diabetes (Glucophage 500mg) - مدينة نصر
    (NEWID(), 'E8FC6117-ECCC-496C-9B9F-222129D3E600', N'مدينة نصر', N'القاهرة',
     N'روشتة سكر خفيف: Glucophage 500mg قرص مرتين يومياً بعد الطعام.',
     N'[{"MedicineName":"Glucophage","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"500mg","Quantity":2,"IsPediatric":false}]',
     0, DATEADD(DAY, -6, GETUTCDATE()), N'Glucophage', N'Metformin HCl', N'Tablet', N'500mg', 2, NULL, NULL, NULL, NULL, 0),

    -- 12. Pediatric (Panadol Infant Drops + Visceralgine) - مدينة نصر
    (NEWID(), 'E8FC6117-ECCC-496C-9B9F-222129D3E600', N'مدينة نصر', N'القاهرة',
     N'روشتة مغص وسخونية للرضع: Panadol Infant Drops 1ml عند اللزوم + Visceralgine Syrup 2.5ml عند المغص.',
     N'[{"MedicineName":"Panadol Infant Drops","GenericName":"Paracetamol","DosageForm":"Drops","Strength":"100mg/ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Visceralgine Syrup","GenericName":"Tiemannium Methylsulfate","DosageForm":"Syrup","Strength":"120ml","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -7, GETUTCDATE()), N'Panadol Infant Drops', N'Paracetamol', N'Drops', N'100mg/ml', 1, N'Visceralgine Syrup', N'Tiemannium Methylsulfate', N'Syrup', N'120ml', 1),

    -- 13. Gastro & Stomach (Pantoloc + Gaviscon) - الدقي الجيزة
    (NEWID(), 'CE2BEAB4-580F-4D66-9335-0391B7287506', N'الدقي', N'الجيزة',
     N'روشتة حموضة وجرثومة معدة: Pantoloc 40mg قرص قبل الإفطار + Gaviscon Syrup 10ml بعد الوجبات.',
     N'[{"MedicineName":"Pantoloc","GenericName":"Pantoprazole","DosageForm":"Tablet","Strength":"40mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Gaviscon Syrup","GenericName":"Sodium Alginate","DosageForm":"Syrup","Strength":"200ml","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -8, GETUTCDATE()), N'Pantoloc', N'Pantoprazole', N'Tablet', N'40mg', 1, N'Gaviscon Syrup', N'Sodium Alginate', N'Syrup', N'200ml', 1),

    -- 14. Painkiller & Bone (Brufen 600mg + Move Free) - مصر الجديدة
    (NEWID(), 'B559D3F2-A1FD-4A9E-8246-3E415434D703', N'مصر الجديدة', N'القاهرة',
     N'روشتة عظام وخشونة: Brufen 600mg قرص بعد الأكل + Alphintern 2 قرص قبل الأكل.',
     N'[{"MedicineName":"Brufen","GenericName":"Ibuprofen","DosageForm":"Tablet","Strength":"600mg","Quantity":2,"IsPediatric":false},{"MedicineName":"Alphintern","GenericName":"Chymotrypsin","DosageForm":"Tablet","Strength":"Standard","Quantity":2,"IsPediatric":false}]',
     0, DATEADD(DAY, -9, GETUTCDATE()), N'Brufen', N'Ibuprofen', N'Tablet', N'600mg', 2, N'Alphintern', N'Chymotrypsin', N'Tablet', N'Standard', 2),

    -- 15. Diabetes (Victoza / Insulin + Glucophage) - ميامي الإسكندرية
    (NEWID(), '15B1A403-209F-482D-B9A9-0322EE8FD34C', N'ميامي', N'الإسكندرية',
     N'روشتة سكر حقن: Victoza 0.6mg حقنة تحت الجلد يومياً + Glucophage 1000mg قرص مساءً.',
     N'[{"MedicineName":"Victoza","GenericName":"Liraglutide","DosageForm":"Pen Injection","Strength":"6mg/ml","Quantity":1,"IsPediatric":false},{"MedicineName":"Glucophage","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"1000mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -10, GETUTCDATE()), N'Victoza', N'Liraglutide', N'Pen Injection', N'6mg/ml', 1, N'Glucophage', N'Metformin HCl', N'Tablet', N'1000mg', 1),

    -- 16. Pediatric (Maxilase Syrup + Bronchicum Syrup) - الشيخ زايد
    (NEWID(), '9AF2E50F-BBFC-49ED-867E-04E7812C0419', N'الشيخ زايد', N'الجيزة',
     N'روشتة كحة تورم أطفال: Maxilase Syrup 5ml 3 مرات يومياً + Bronchicum Elixir 5ml 3 مرات.',
     N'[{"MedicineName":"Maxilase Syrup","GenericName":"Alpha-Amylase","DosageForm":"Syrup","Strength":"100ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Bronchicum Syrup","GenericName":"Thyme Extract","DosageForm":"Syrup","Strength":"100ml","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -11, GETUTCDATE()), N'Maxilase Syrup', N'Alpha-Amylase', N'Syrup', N'100ml', 1, N'Bronchicum Syrup', N'Thyme Extract', N'Syrup', N'100ml', 1),

    -- 17. Antibiotic Respiratory (Zithromax 500mg + C-Retard) - المعادي
    (NEWID(), '044294EA-F757-455E-8926-3511AADFE6B5', N'المعادي', N'القاهرة',
     N'روشتة جيوب أنفية وكورونا: Zithromax 500mg قرص يومياً لمدة 3 أيام + C-Retard 500mg كبسولة.',
     N'[{"MedicineName":"Zithromax","GenericName":"Azithromycin","DosageForm":"Tablet","Strength":"500mg","Quantity":1,"IsPediatric":false},{"MedicineName":"C-Retard","GenericName":"Vitamin C","DosageForm":"Capsule","Strength":"500mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -12, GETUTCDATE()), N'Zithromax', N'Azithromycin', N'Tablet', N'500mg', 1, N'C-Retard', N'Vitamin C', N'Capsule', N'500mg', 1),

    -- 18. Pediatric (Ambroxol Syrup + Cetrak Drops) - سموحة الإسكندرية
    (NEWID(), '378D1E32-87EB-4048-BFE3-02E2E01AD4BC', N'سموحة', N'الإسكندرية',
     N'روشتة بلغم وحساسية أطفال: Ambroxol Syrup 2.5ml مرتين يومياً + Cetrak Drops 5 نقط مساءً.',
     N'[{"MedicineName":"Ambroxol Syrup","GenericName":"Ambroxol HCl","DosageForm":"Syrup","Strength":"100ml","Quantity":1,"IsPediatric":true},{"MedicineName":"Cetrak Drops","GenericName":"Cetirizine","DosageForm":"Drops","Strength":"10mg/ml","Quantity":1,"IsPediatric":true}]',
     1, DATEADD(DAY, -13, GETUTCDATE()), N'Ambroxol Syrup', N'Ambroxol HCl', N'Syrup', N'100ml', 1, N'Cetrak Drops', N'Cetirizine', N'Drops', N'10mg/ml', 1),

    -- 19. Chronic Hypertension (Crestor + Co-Aprovel) - مصر الجديدة
    (NEWID(), 'B559D3F2-A1FD-4A9E-8246-3E415434D703', N'مصر الجديدة', N'القاهرة',
     N'روشتة ضغط مرتفع: Co-Aprovel 150/12.5mg قرص صباحاً + Crestor 10mg قرص قبل النوم.',
     N'[{"MedicineName":"Co-Aprovel","GenericName":"Irbesartan/HCTZ","DosageForm":"Tablet","Strength":"150/12.5mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Crestor","GenericName":"Rosuvastatin","DosageForm":"Tablet","Strength":"10mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -14, GETUTCDATE()), N'Co-Aprovel', N'Irbesartan/HCTZ', N'Tablet', N'150/12.5mg', 1, N'Crestor', N'Rosuvastatin', N'Tablet', N'10mg', 1),

    -- 20. Diabetes (Glucophage 1000mg + Amaryl 2mg) - مدينة نصر
    (NEWID(), 'E8FC6117-ECCC-496C-9B9F-222129D3E600', N'مدينة نصر', N'القاهرة',
     N'روشتة سكر ثانية: Glucophage XR 1000mg قرص بعد العشاء + Amaryl 2mg قرص صباحاً.',
     N'[{"MedicineName":"Glucophage","GenericName":"Metformin HCl","DosageForm":"Tablet","Strength":"1000mg","Quantity":1,"IsPediatric":false},{"MedicineName":"Amaryl","GenericName":"Glimepiride","DosageForm":"Tablet","Strength":"2mg","Quantity":1,"IsPediatric":false}]',
     0, DATEADD(DAY, -15, GETUTCDATE()), N'Glucophage', N'Metformin HCl', N'Tablet', N'1000mg', 1, N'Amaryl', N'Glimepiride', N'Tablet', N'2mg', 1);

    -- ── 3. Insert into PrescriptionReviews ────────────────────────────────
    INSERT INTO [PrescriptionReviews]
    ([PrescriptionReviewId], [PatientUserId], [PrescriptionImagePath], [OriginalFileName], [AIModel], [ReviewStatus], [CreatedAt], [UpdatedAt])
    SELECT
        ReviewId,
        @PatientUserId,
        N'/uploads/prescriptions/sample_' + CAST(Seq AS NVARCHAR(10)) + N'.jpg',
        N'rx_sample_' + CAST(Seq AS NVARCHAR(10)) + N'.jpg',
        N'gemini-2.5-flash',
        1, -- Approved / Completed ReviewStatus
        CreatedAt,
        CreatedAt
    FROM @PrescriptionTable;

    -- ── 4. Insert into PrescriptionReviewMedicines ───────────────────────
    -- First Medicine for each prescription
    INSERT INTO [PrescriptionReviewMedicines]
    ([PrescriptionReviewMedicineId], [PrescriptionReviewId], [MedicineName], [GenericName], [Strength], [DosageForm], [Quantity], [IsEdited])
    SELECT
        NEWID(), ReviewId, Med1Name, Med1Generic, Med1Strength, Med1Form, Med1Qty, 0
    FROM @PrescriptionTable WHERE Med1Name IS NOT NULL;

    -- Second Medicine for prescriptions that have 2 medicines
    INSERT INTO [PrescriptionReviewMedicines]
    ([PrescriptionReviewMedicineId], [PrescriptionReviewId], [MedicineName], [GenericName], [Strength], [DosageForm], [Quantity], [IsEdited])
    SELECT
        NEWID(), ReviewId, Med2Name, Med2Generic, Med2Strength, Med2Form, Med2Qty, 0
    FROM @PrescriptionTable WHERE Med2Name IS NOT NULL;

    -- ── 5. Insert into PrescriptionVectorIndices (Vector RAG Search Table) ─
    INSERT INTO [PrescriptionVectorIndices]
    ([PrescriptionVectorIndexId], [PrescriptionReviewId], [BranchId], [City], [Governorate], [IndexedText], [EmbeddingJson], [MedicinesJson], [IsPediatric], [CreatedAt], [UpdatedAt])
    SELECT
        NEWID(),
        ReviewId,
        BranchId,
        City,
        Governorate,
        IndexedText,
        N'[0.015, -0.042, 0.088, 0.12, -0.005, 0.063, -0.071, 0.095]', -- Unit Normalized Feature Embedding JSON
        MedicinesJson,
        IsPediatric,
        CreatedAt,
        CreatedAt
    FROM @PrescriptionTable;

    COMMIT TRANSACTION;
    PRINT N'SUCCESS: 20 Prescriptions, Medicines, and Vector Indices seeded successfully!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;

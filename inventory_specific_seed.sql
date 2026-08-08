-- ================================================================
-- INSERT مخزون للفروع المحددة
--
-- Branch 1 (3937E54A) → Drug A + Drug B فقط
-- Branch 2 (4CA39531) → Drug C + Drug D فقط
-- Branch 3 (6A28D71E) → Drug A + B + C + D (الأربعة)
-- Branch 4 (91206A27) → كل الأدوية من القائمة
-- Branch 5 (845565EE) → كل الأدوية من القائمة
-- ================================================================

BEGIN TRANSACTION;
BEGIN TRY

-- ── Drug IDs المرجعية ─────────────────────────────────────────
DECLARE @DrugA UNIQUEIDENTIFIER = '18C9BF81-EB30-486E-2B30-08DEF23AE66C';
DECLARE @DrugB UNIQUEIDENTIFIER = 'ADAE9FB1-3905-443A-2B31-08DEF23AE66C';
DECLARE @DrugC UNIQUEIDENTIFIER = '27C35FAD-6887-4D44-2B32-08DEF23AE66C';
DECLARE @DrugD UNIQUEIDENTIFIER = '6DA14662-A1EE-44FA-2B33-08DEF23AE66C';

-- ── Branch IDs ────────────────────────────────────────────────
DECLARE @B1 UNIQUEIDENTIFIER = '3937E54A-C830-4DF8-B36D-744A9857E1E4';
DECLARE @B2 UNIQUEIDENTIFIER = '4CA39531-3570-42A6-BD12-4DDADED8F69C';
DECLARE @B3 UNIQUEIDENTIFIER = '6A28D71E-5A9C-4B84-91BC-16EFDBCC0DF6';
DECLARE @B4 UNIQUEIDENTIFIER = '91206A27-3A2E-4494-A1CF-6CADECA5CBE5';
DECLARE @B5 UNIQUEIDENTIFIER = '845565EE-F089-4905-82D9-C229B0C75841';


-- ================================================================
-- Branch 1 → Drug A + Drug B فقط
-- ================================================================
INSERT INTO [PharmacyInventories]
    ([InventoryId],[BranchId],[DrugId],[StockQuantity],[ReservedQuantity],[UnitPrice],[ExpiryDate],[LastSyncedAt],[ReorderPoint])
VALUES
(NEWID(), @B1, @DrugA, 150, 0, 45.00, '2026-12-31', GETUTCDATE(), 15),
(NEWID(), @B1, @DrugB, 200, 2, 30.00, '2027-03-15', GETUTCDATE(), 10);


-- ================================================================
-- Branch 2 → Drug C + Drug D فقط
-- ================================================================
INSERT INTO [PharmacyInventories]
    ([InventoryId],[BranchId],[DrugId],[StockQuantity],[ReservedQuantity],[UnitPrice],[ExpiryDate],[LastSyncedAt],[ReorderPoint])
VALUES
(NEWID(), @B2, @DrugC,  80, 0, 55.00, '2027-01-20', GETUTCDATE(), 20),
(NEWID(), @B2, @DrugD, 120, 5, 25.00, '2026-11-10', GETUTCDATE(), 15);


-- ================================================================
-- Branch 3 → Drug A + B + C + D (الأربعة)
-- ================================================================
INSERT INTO [PharmacyInventories]
    ([InventoryId],[BranchId],[DrugId],[StockQuantity],[ReservedQuantity],[UnitPrice],[ExpiryDate],[LastSyncedAt],[ReorderPoint])
VALUES
(NEWID(), @B3, @DrugA,  60, 1, 47.00, '2026-12-31', GETUTCDATE(), 15),
(NEWID(), @B3, @DrugB,  90, 0, 32.00, '2027-03-15', GETUTCDATE(), 10),
(NEWID(), @B3, @DrugC,  40, 0, 58.00, '2027-01-20', GETUTCDATE(), 20),
(NEWID(), @B3, @DrugD, 110, 3, 27.00, '2026-11-10', GETUTCDATE(), 15);


-- ================================================================
-- Branch 4 + Branch 5 → كل الأدوية من القائمة
-- ================================================================
INSERT INTO [PharmacyInventories]
    ([InventoryId],[BranchId],[DrugId],[StockQuantity],[ReservedQuantity],[UnitPrice],[ExpiryDate],[LastSyncedAt],[ReorderPoint])
SELECT
    NEWID(),
    b.BranchId,
    d.DrugId,
    CASE (ABS(CHECKSUM(NEWID())) % 10)
        WHEN 0 THEN 0
        WHEN 1 THEN 15
        WHEN 2 THEN 30
        WHEN 3 THEN 50
        WHEN 4 THEN 80
        WHEN 5 THEN 100
        WHEN 6 THEN 150
        WHEN 7 THEN 200
        WHEN 8 THEN 300
        ELSE       400
    END,
    ABS(CHECKSUM(NEWID())) % 8,
    CAST(CASE WHEN d.Price > 0 THEN d.Price * 1.15 ELSE 30.00 END AS DECIMAL(18,2)),
    CAST(DATEADD(day, 180 + ABS(CHECKSUM(NEWID())) % 500, GETDATE()) AS DATE),
    GETUTCDATE(),
    10 + ABS(CHECKSUM(NEWID())) % 15
FROM
    (VALUES (@B4), (@B5)) AS b(BranchId)
CROSS JOIN [Drugs] d
WHERE d.DrugId IN (
    '18C9BF81-EB30-486E-2B30-08DEF23AE66C',
    'ADAE9FB1-3905-443A-2B31-08DEF23AE66C',
    '27C35FAD-6887-4D44-2B32-08DEF23AE66C',
    '6DA14662-A1EE-44FA-2B33-08DEF23AE66C',
    'CEA385DD-0F0C-4360-2B34-08DEF23AE66C',
    '944907F2-FE45-47F7-2B35-08DEF23AE66C',
    '1DB4867A-D7CE-4A44-2B36-08DEF23AE66C',
    'D39DB067-809F-422A-2B37-08DEF23AE66C',
    '3310B9DA-2BFD-4C8B-2B38-08DEF23AE66C',
    '7D9BB5BE-82AF-4378-2B39-08DEF23AE66C',
    '42BB981D-8A78-41F7-2B3A-08DEF23AE66C',
    'BAAFD446-35DF-47CB-2B3B-08DEF23AE66C',
    '07CB6A35-D49E-4456-2B3C-08DEF23AE66C',
    'D6EF5742-CAD5-4F3A-2B3D-08DEF23AE66C',
    '18DCAF18-2B8C-4F1A-2B3E-08DEF23AE66C',
    '081C9495-FD5F-44F3-2B3F-08DEF23AE66C',
    '99A49445-E776-4DB2-2B40-08DEF23AE66C',
    'D196EB0A-A505-47FD-2B41-08DEF23AE66C',
    'CF62E637-CC14-4376-2B42-08DEF23AE66C',
    '39200AFF-00F6-4F71-2B43-08DEF23AE66C',
    '55F09449-1620-43E3-2B44-08DEF23AE66C',
    '749F29AA-6DA6-4ABA-2B45-08DEF23AE66C',
    '56B3DA18-2748-45FA-2B46-08DEF23AE66C',
    'F62F1D5D-9451-40B4-2B47-08DEF23AE66C',
    '619A6ACC-D16D-49B1-2B48-08DEF23AE66C',
    '5F2ECE99-2376-440F-2B49-08DEF23AE66C',
    'DE033006-E899-4908-2B4A-08DEF23AE66C',
    '275F8CC7-2A8E-4626-2B4B-08DEF23AE66C',
    '9B1A7684-B13F-4C65-2B4C-08DEF23AE66C',
    'DD75195C-3450-408D-2B4D-08DEF23AE66C',
    '7E661141-855C-4DFD-2B4E-08DEF23AE66C',
    '8F98AAF2-99C5-4502-2B4F-08DEF23AE66C',
    '9E366A3E-A197-4405-2B50-08DEF23AE66C',
    '66EEACA0-765B-4424-2B51-08DEF23AE66C',
    '6760327F-6D74-4B9D-2B52-08DEF23AE66C',
    '821CB302-3086-498F-2B53-08DEF23AE66C',
    '87E2A0D0-B0CE-4075-2B54-08DEF23AE66C',
    '1B739D5A-DA37-4739-2B55-08DEF23AE66C',
    'C65E3846-BCFC-4DC6-2B56-08DEF23AE66C',
    'B7B47563-1ADC-4734-2B57-08DEF23AE66C',
    'EBF9BAB9-5674-4315-2B58-08DEF23AE66C',
    '65B25B40-80F1-4CC8-2B59-08DEF23AE66C',
    'FCE16699-6284-4AF7-2B5A-08DEF23AE66C',
    '62048410-2D48-45AE-2B5B-08DEF23AE66C',
    'BE41DBEB-78AF-497B-2B5C-08DEF23AE66C',
    '39E1C7C2-35FA-44C5-2B5D-08DEF23AE66C',
    '617E8DCC-FBCB-4C30-2B5E-08DEF23AE66C',
    '52E9A7DE-6390-493A-2B5F-08DEF23AE66C',
    '620DBEF2-2E32-456F-2B60-08DEF23AE66C',
    '88FE5DF9-6EF9-49F2-2B61-08DEF23AE66C',
    'AF8B157D-E972-40B5-2B62-08DEF23AE66C',
    'F2EE7775-6D3A-4CDC-2B63-08DEF23AE66C',
    '9A4A10C6-B7AB-4AAE-2B64-08DEF23AE66C',
    'D2DDBE17-B035-4DE5-2B65-08DEF23AE66C',
    'D3DA71C0-E7BA-4384-2B66-08DEF23AE66C',
    '19164183-97FB-4282-2B67-08DEF23AE66C',
    '9863ED66-52ED-4B0B-2B68-08DEF23AE66C',
    'E0C61C3F-0308-45AE-2B69-08DEF23AE66C',
    'E3D81054-4066-4834-2B6A-08DEF23AE66C',
    '3FD7095A-DF26-4390-2B6B-08DEF23AE66C',
    '58AE9621-0717-447E-2B6C-08DEF23AE66C',
    '2DBBCEAF-93FD-489A-2B6D-08DEF23AE66C',
    '85286075-4428-40A2-2B6E-08DEF23AE66C',
    'E2A0E564-24D0-4E1E-2B6F-08DEF23AE66C',
    '8BE9D19A-0472-48F8-2B70-08DEF23AE66C',
    '6434B7DA-19E5-42D0-2B71-08DEF23AE66C',
    '3B1464BD-D383-4D11-2B72-08DEF23AE66C',
    '562CE85B-4209-4A9F-2B73-08DEF23AE66C',
    'FD258D6A-6FB4-4599-2B74-08DEF23AE66C',
    '1CB64A81-6F50-4944-2B75-08DEF23AE66C',
    'CF37CC0A-36D2-4514-2B76-08DEF23AE66C',
    'A82AA8FE-67F0-4C24-2B77-08DEF23AE66C',
    'F661D970-7D7A-48E6-2B78-08DEF23AE66C',
    'D8D2A223-B334-4EAE-2B79-08DEF23AE66C',
    'D5F56FF5-63B3-4F63-2B7A-08DEF23AE66C',
    '359F8FAE-28EE-4BEC-2B7B-08DEF23AE66C',
    '785BA869-7591-421A-2B7C-08DEF23AE66C',
    'F7911C11-D927-4D70-2B7D-08DEF23AE66C',
    '4F0C7EFA-3A6C-4A08-2B7E-08DEF23AE66C',
    '5BF360B3-A7B3-42B5-2B7F-08DEF23AE66C',
    '5C77A5A3-A9A9-4070-2B80-08DEF23AE66C',
    '5527459B-363E-41B1-2B81-08DEF23AE66C',
    'D423CE5F-249E-4976-2B82-08DEF23AE66C',
    '8A926140-D7E2-4238-2B83-08DEF23AE66C',
    '3F14971F-75FD-42CC-2B84-08DEF23AE66C',
    'F753A065-4BB2-4415-2B85-08DEF23AE66C',
    'C4AB9ACD-945E-44C8-2B86-08DEF23AE66C',
    '384AD702-2348-4B31-2B87-08DEF23AE66C',
    '5B84F32E-E9CA-4E3E-2B88-08DEF23AE66C',
    '2F045E8A-6E3E-46B8-2B89-08DEF23AE66C',
    'D3578334-DAEE-43D3-2B8A-08DEF23AE66C',
    '01438B95-C399-4528-2B8B-08DEF23AE66C',
    '52A5B180-A584-4B58-2B8C-08DEF23AE66C',
    '6E7C725B-0F91-4A94-2B8D-08DEF23AE66C',
    '5E19086A-9E54-4545-2B8E-08DEF23AE66C',
    '1F29A76F-3CF3-45F4-2B8F-08DEF23AE66C',
    '27803D44-D1B1-47A8-2B90-08DEF23AE66C',
    '608D7238-B297-47A8-2B91-08DEF23AE66C',
    'E1F4A127-82BE-4441-2B92-08DEF23AE66C',
    '51C9FEFC-47F0-4B86-2B93-08DEF23AE66C',
    'B23EDBC0-2060-4EF2-2B94-08DEF23AE66C',
    'A85A192E-9FE4-44E7-2B95-08DEF23AE66C',
    'A99114FD-FFCE-419D-2B96-08DEF23AE66C',
    '86342F90-B915-4316-2B97-08DEF23AE66C',
    '315E4586-2353-456B-2B98-08DEF23AE66C',
    '1968C3CC-622B-40AB-2B99-08DEF23AE66C',
    'D7292E40-A47E-41E2-2B9A-08DEF23AE66C',
    'A42E2A2C-C796-4F4B-2B9B-08DEF23AE66C',
    '8BB6B391-9AD1-4AFC-2B9C-08DEF23AE66C',
    '5FFA4EF6-13E5-49B4-2B9D-08DEF23AE66C',
    'C80F46CA-FDBE-472A-2B9E-08DEF23AE66C',
    '5407E23A-9192-4296-2B9F-08DEF23AE66C'
);

COMMIT TRANSACTION;
PRINT '✅ تم الإدراج بنجاح';
PRINT '• Branch 1 → 2 أدوية (A + B)';
PRINT '• Branch 2 → 2 أدوية (C + D)';
PRINT '• Branch 3 → 4 أدوية (A + B + C + D)';
PRINT '• Branch 4 → كل الأدوية من القائمة';
PRINT '• Branch 5 → كل الأدوية من القائمة';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ خطأ: ' + ERROR_MESSAGE();
    THROW;
END CATCH;




-- ================================================================
-- أقرب 5 فروع من الإحداثيات المستهدفة
-- lat=30.1291533065186  lng=31.2771244765832
-- ================================================================

DECLARE @MyLocation geography = geography::STPointFromText(
    'POINT(31.2771244765832 30.1291533065186)', 4326
);

SELECT TOP 5
    pb.BranchId,
    pb.BranchName,
    pb.AddressLine,
    pb.City,
    pb.Governorate,
    pb.PhoneNumber,
    p.LegalName                              AS PharmacyName,
    CAST(
        pb.GeoLocation.STDistance(@MyLocation)
    AS DECIMAL(10,2))                        AS DistanceMeters,
    CAST(
        pb.GeoLocation.STDistance(@MyLocation) / 1000.0
    AS DECIMAL(10,3))                        AS DistanceKm,
    pb.SupportsDelivery,
    pb.SupportsPickup
FROM  [PharmacyBranches]  pb
JOIN  [Pharmacies]        p  ON p.PharmacyId = pb.PharmacyId
WHERE pb.GeoLocation IS NOT NULL
ORDER BY pb.GeoLocation.STDistance(@MyLocation) ASC;




-- ================================================================
-- UPDATE إحداثيات فرعين محددين
--
-- Target المرجعي: lat=30.1291533  lng=31.2771244
--
-- Branch 91206A27 → 320m road distance من Target
--   straight-line: ~245m شمال شرق (على شارع الهرم)
--   lat: +0.0022 → +244m شمال
--   lng: +0.0008 → +77m شرق
--   → المسار على الخريطة = ~320m
--
-- Branch 6A28D71E → يمين 91206A27 بـ 30m (شرق)
--   نفس lat، lng + 30/96000
-- ================================================================

BEGIN TRANSACTION;
BEGIN TRY

-- ── Branch 91206A27 → 320m (road) من المركز ─────────────────────
UPDATE [PharmacyBranches]
SET [GeoLocation] = geography::STPointFromText(
    'POINT(31.2779244 30.1313533)', 4326
)
WHERE [BranchId] = '91206A27-3A2E-4494-A1CF-6CADECA5CBE5';

-- ── Branch 6A28D71E → يمين (شرق) 91206A27 بـ 30m ───────────────
-- 30m شرق = +0.0003125 درجة طول (30 ÷ 96,000 م/درجة)
UPDATE [PharmacyBranches]
SET [GeoLocation] = geography::STPointFromText(
    'POINT(31.2782369 30.1313533)', 4326
)
WHERE [BranchId] = '6A28D71E-5A9C-4B84-91BC-16EFDBCC0DF6';

COMMIT TRANSACTION;
PRINT '✅ تم تحديث الإحداثيات بنجاح';

-- ── تحقق فوري من المسافات ──────────────────────────────────────
DECLARE @Target    geography = geography::STPointFromText('POINT(31.2771244765832 30.1291533065186)', 4326);
DECLARE @B91206A27 geography = geography::STPointFromText('POINT(31.2779244 30.1313533)', 4326);
DECLARE @B6A28D71E geography = geography::STPointFromText('POINT(31.2782369 30.1313533)', 4326);

SELECT
    '91206A27 ← → Target'            AS Label,
    CAST(@B91206A27.STDistance(@Target)           AS DECIMAL(8,1)) AS StraightLine_m,
    CAST(@B91206A27.STDistance(@Target) * 1.31    AS DECIMAL(8,1)) AS EstRoadDist_m

UNION ALL SELECT
    '6A28D71E ← → Target'            AS Label,
    CAST(@B6A28D71E.STDistance(@Target)           AS DECIMAL(8,1)),
    CAST(@B6A28D71E.STDistance(@Target) * 1.31    AS DECIMAL(8,1))

UNION ALL SELECT
    '6A28D71E ← → 91206A27 (يمين)'  AS Label,
    CAST(@B6A28D71E.STDistance(@B91206A27)        AS DECIMAL(8,1)),
    CAST(@B6A28D71E.STDistance(@B91206A27) * 1.00 AS DECIMAL(8,1)); -- مسافة مستقيمة (نفس الشارع)

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ خطأ: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
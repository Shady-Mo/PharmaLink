-- ========================================================================================
-- PharmaLink Seed SQL Script
-- Database: SQL Server
-- Description: Seeds roles, test users (Admins, Pharmacists, Patients), pharmacies, and
--              pharmacy branches. Wraps in a transaction and cleans up previous test runs.
-- Password for all seeded users: P@ssword123
-- ========================================================================================

BEGIN TRANSACTION;

BEGIN TRY
    -- ------------------------------------------------------------------------------------
    -- 1. DECLARE CONSTANTS & VARIABLES
    -- ------------------------------------------------------------------------------------
    -- User ID Variables
    DECLARE @Admin1Id UNIQUEIDENTIFIER = 'A1111111-1111-1111-1111-111111111111';
    DECLARE @Admin2Id UNIQUEIDENTIFIER = 'A2222222-2222-2222-2222-222222222222';
    DECLARE @Admin3Id UNIQUEIDENTIFIER = 'A3333333-3333-3333-3333-333333333333';

    DECLARE @Pharmacist1Id UNIQUEIDENTIFIER = 'B1111111-1111-1111-1111-111111111111';
    DECLARE @Pharmacist2Id UNIQUEIDENTIFIER = 'B2222222-2222-2222-2222-222222222222';
    DECLARE @Pharmacist3Id UNIQUEIDENTIFIER = 'B3333333-3333-3333-3333-333333333333';

    DECLARE @Patient1Id UNIQUEIDENTIFIER = 'C1111111-1111-1111-1111-111111111111';
    DECLARE @Patient2Id UNIQUEIDENTIFIER = 'C2222222-2222-2222-2222-222222222222';
    DECLARE @Patient3Id UNIQUEIDENTIFIER = 'C3333333-3333-3333-3333-333333333333';

    -- Pharmacy ID Variables
    DECLARE @Pharma1Id UNIQUEIDENTIFIER = 'D1111111-1111-1111-1111-111111111111';
    DECLARE @Pharma2Id UNIQUEIDENTIFIER = 'D2222222-2222-2222-2222-222222222222';
    DECLARE @Pharma3Id UNIQUEIDENTIFIER = 'D3333333-3333-3333-3333-333333333333';
    DECLARE @Pharma4Id UNIQUEIDENTIFIER = 'D4444444-4444-4444-4444-444444444444';
    DECLARE @Pharma5Id UNIQUEIDENTIFIER = 'D5555555-5555-5555-5555-555555555555';

    -- Role ID Variables
    DECLARE @PatientRoleId UNIQUEIDENTIFIER;
    DECLARE @PharmacistRoleId UNIQUEIDENTIFIER;
    DECLARE @AdminRoleId UNIQUEIDENTIFIER;

    -- Common Password Hash for "P@ssword123" (ASP.NET Core Identity V3 format)
    DECLARE @SharedPasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEPOWUjkBBjnBkT/oFFjsx0EdDCjFhGopC7jS4lWP2FSYdMxbkneSGyQ/OvRHUIegxg==';

    -- ------------------------------------------------------------------------------------
    -- 2. CLEANUP EXISTING SEEDED DATA (For Idempotency)
    -- ------------------------------------------------------------------------------------
    PRINT 'Cleaning up existing seed data...';

    -- Delete branches of seeded pharmacies
    DELETE FROM [PharmacyBranches] 
    WHERE [PharmacyId] IN (@Pharma1Id, @Pharma2Id, @Pharma3Id, @Pharma4Id, @Pharma5Id);

    -- Delete seeded pharmacies
    DELETE FROM [Pharmacies] 
    WHERE [PharmacyId] IN (@Pharma1Id, @Pharma2Id, @Pharma3Id, @Pharma4Id, @Pharma5Id);

    -- Delete roles mappings for seeded users
    DELETE FROM [AspNetUserRoles] 
    WHERE [UserId] IN (
        @Admin1Id, @Admin2Id, @Admin3Id,
        @Pharmacist1Id, @Pharmacist2Id, @Pharmacist3Id,
        @Patient1Id, @Patient2Id, @Patient3Id
    );

    -- Delete seeded users
    DELETE FROM [AspNetUsers] 
    WHERE [Id] IN (
        @Admin1Id, @Admin2Id, @Admin3Id,
        @Pharmacist1Id, @Pharmacist2Id, @Pharmacist3Id,
        @Patient1Id, @Patient2Id, @Patient3Id
    );

    -- ------------------------------------------------------------------------------------
    -- 3. ENSURE ROLES EXIST IN DATABASE
    -- ------------------------------------------------------------------------------------
    PRINT 'Ensuring application roles exist...';

    SELECT @PatientRoleId = [Id] FROM [AspNetRoles] WHERE [Name] = 'Patient';
    IF @PatientRoleId IS NULL
    BEGIN
        SET @PatientRoleId = 'D6F7C85E-B3A9-4E6D-A08D-E4D71D925A11';
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (@PatientRoleId, 'Patient', 'PATIENT', UPPER(CAST(NEWID() AS NVARCHAR(36))));
    END

    SELECT @PharmacistRoleId = [Id] FROM [AspNetRoles] WHERE [Name] = 'Pharmacist';
    IF @PharmacistRoleId IS NULL
    BEGIN
        SET @PharmacistRoleId = 'E7E8D96F-C4B0-5F7E-B19E-F5E82EA36B22';
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (@PharmacistRoleId, 'Pharmacist', 'PHARMACIST', UPPER(CAST(NEWID() AS NVARCHAR(36))));
    END

    SELECT @AdminRoleId = [Id] FROM [AspNetRoles] WHERE [Name] = 'Admin';
    IF @AdminRoleId IS NULL
    BEGIN
        SET @AdminRoleId = 'F8F9EA70-D5C1-6F8F-C2AF-06F93FB47C33';
        INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
        VALUES (@AdminRoleId, 'Admin', 'ADMIN', UPPER(CAST(NEWID() AS NVARCHAR(36))));
    END

    -- ------------------------------------------------------------------------------------
    -- 4. INSERT TEST USERS (AspNetUsers)
    -- ------------------------------------------------------------------------------------
    PRINT 'Inserting test users...';

    -- Insert Admin Users (UserType: SystemAdmin)
    INSERT INTO [AspNetUsers] (
        [Id], [FullName], [Status], [CreatedAt], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed],
        [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [UserName], [NormalizedUserName], [UserType]
    ) VALUES 
    (@Admin1Id, 'John Admin One', 1, GETUTCDATE(), 'admin1@pharmalink.com', 'ADMIN1@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01000000001', 1, 0, 1, 0, 
     'admin1@pharmalink.com', 'ADMIN1@PHARMALINK.COM', 'SystemAdmin'),

    (@Admin2Id, 'Sarah Admin Two', 1, GETUTCDATE(), 'admin2@pharmalink.com', 'ADMIN2@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01000000002', 1, 0, 1, 0, 
     'admin2@pharmalink.com', 'ADMIN2@PHARMALINK.COM', 'SystemAdmin'),

    (@Admin3Id, 'Mike Admin Three', 1, GETUTCDATE(), 'admin3@pharmalink.com', 'ADMIN3@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01000000003', 1, 0, 1, 0, 
     'admin3@pharmalink.com', 'ADMIN3@PHARMALINK.COM', 'SystemAdmin');

    -- Insert Pharmacist Users (UserType: Pharmacist)
    INSERT INTO [AspNetUsers] (
        [Id], [FullName], [Status], [CreatedAt], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed],
        [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [UserName], [NormalizedUserName], [UserType]
    ) VALUES 
    (@Pharmacist1Id, 'Alice Pharmacist One', 1, GETUTCDATE(), 'pharmacist1@pharmalink.com', 'PHARMACIST1@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01100000001', 1, 0, 1, 0, 
     'pharmacist1@pharmalink.com', 'PHARMACIST1@PHARMALINK.COM', 'Pharmacist'),

    (@Pharmacist2Id, 'Bob Pharmacist Two', 1, GETUTCDATE(), 'pharmacist2@pharmalink.com', 'PHARMACIST2@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01100000002', 1, 0, 1, 0, 
     'pharmacist2@pharmalink.com', 'PHARMACIST2@PHARMALINK.COM', 'Pharmacist'),

    (@Pharmacist3Id, 'Charlie Pharmacist Three', 1, GETUTCDATE(), 'pharmacist3@pharmalink.com', 'PHARMACIST3@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01100000003', 1, 0, 1, 0, 
     'pharmacist3@pharmalink.com', 'PHARMACIST3@PHARMALINK.COM', 'Pharmacist');

    -- Insert Patient Users (UserType: Patient)
    INSERT INTO [AspNetUsers] (
        [Id], [FullName], [Status], [CreatedAt], [Email], [NormalizedEmail], [EmailConfirmed],
        [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed],
        [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [UserName], [NormalizedUserName], [UserType]
    ) VALUES 
    (@Patient1Id, 'David Patient One', 1, GETUTCDATE(), 'patient1@pharmalink.com', 'PATIENT1@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01200000001', 1, 0, 1, 0, 
     'patient1@pharmalink.com', 'PATIENT1@PHARMALINK.COM', 'Patient'),

    (@Patient2Id, 'Emma Patient Two', 1, GETUTCDATE(), 'patient2@pharmalink.com', 'PATIENT2@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01200000002', 1, 0, 1, 0, 
     'patient2@pharmalink.com', 'PATIENT2@PHARMALINK.COM', 'Patient'),

    (@Patient3Id, 'Fiona Patient Three', 1, GETUTCDATE(), 'patient3@pharmalink.com', 'PATIENT3@PHARMALINK.COM', 1, 
     @SharedPasswordHash, UPPER(CAST(NEWID() AS NVARCHAR(36))), UPPER(CAST(NEWID() AS NVARCHAR(36))), '01200000003', 1, 0, 1, 0, 
     'patient3@pharmalink.com', 'PATIENT3@PHARMALINK.COM', 'Patient');

    -- ------------------------------------------------------------------------------------
    -- 5. MAP USERS TO ROLES (AspNetUserRoles)
    -- ------------------------------------------------------------------------------------
    PRINT 'Assigning roles to users...';

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    VALUES 
    (@Admin1Id, @AdminRoleId),
    (@Admin2Id, @AdminRoleId),
    (@Admin3Id, @AdminRoleId),
    (@Pharmacist1Id, @PharmacistRoleId),
    (@Pharmacist2Id, @PharmacistRoleId),
    (@Pharmacist3Id, @PharmacistRoleId),
    (@Patient1Id, @PatientRoleId),
    (@Patient2Id, @PatientRoleId),
    (@Patient3Id, @PatientRoleId);

    -- ------------------------------------------------------------------------------------
    -- 6. INSERT PHARMACIES (Pharmacies)
    -- ------------------------------------------------------------------------------------
    PRINT 'Inserting pharmacies...';

    -- VerificationStatus: 2 (Verified) to ensure login claims generation works perfectly
    INSERT INTO [Pharmacies] ([PharmacyId], [LegalName], [LicenseNumber], [OwnerUserId], [VerificationStatus])
    VALUES
    (@Pharma1Id, 'El Ezaby Pharmacy', 'LIC-10001', @Pharmacist1Id, 2), -- Owned by Pharmacist 1
    (@Pharma2Id, 'Seif Pharmacy', 'LIC-10002', @Pharmacist1Id, 2),     -- Owned by Pharmacist 1 (multiple pharmacies ownership test)
    (@Pharma3Id, '19019 Pharmacy', 'LIC-10003', @Pharmacist2Id, 2),    -- Owned by Pharmacist 2
    (@Pharma4Id, 'Misr Pharmacy', 'LIC-10004', @Pharmacist2Id, 2),     -- Owned by Pharmacist 2
    (@Pharma5Id, 'Care Pharmacy', 'LIC-10005', @Pharmacist3Id, 2);     -- Owned by Pharmacist 3

    -- ------------------------------------------------------------------------------------
    -- 7. INSERT PHARMACY BRANCHES (PharmacyBranches)
    -- ------------------------------------------------------------------------------------
    PRINT 'Inserting pharmacy branches (3 branches for each pharmacy)...';

    INSERT INTO [PharmacyBranches] (
        [BranchId], [PharmacyId], [BranchName], [City], [Governorate], [GeoLocation], [ServiceRadiusKm], [SupportsDelivery], [SupportsPickup]
    ) VALUES
    -- Branches for El Ezaby Pharmacy (Pharmacy 1)
    (NEWID(), @Pharma1Id, 'El Ezaby - Nasr City Branch', 'Nasr City', 'Cairo', geography::Point(30.0566, 31.3302, 4326), 5.00, 1, 1),
    (NEWID(), @Pharma1Id, 'El Ezaby - Heliopolis Branch', 'Heliopolis', 'Cairo', geography::Point(30.1017, 31.3400, 4326), 8.00, 1, 1),
    (NEWID(), @Pharma1Id, 'El Ezaby - Maadi Branch', 'Maadi', 'Cairo', geography::Point(29.9602, 31.2569, 4326), 6.50, 1, 1),

    -- Branches for Seif Pharmacy (Pharmacy 2)
    (NEWID(), @Pharma2Id, 'Seif - Dokki Branch', 'Dokki', 'Giza', geography::Point(30.0396, 31.2131, 4326), 4.00, 1, 1),
    (NEWID(), @Pharma2Id, 'Seif - Mohandessin Branch', 'Mohandessin', 'Giza', geography::Point(30.0526, 31.1994, 4326), 6.00, 1, 1),
    (NEWID(), @Pharma2Id, 'Seif - Sheikh Zayed Branch', 'Sheikh Zayed', 'Giza', geography::Point(30.0195, 30.9734, 4326), 10.00, 1, 1),

    -- Branches for 19019 Pharmacy (Pharmacy 3)
    (NEWID(), @Pharma3Id, '19019 - Smouha Branch', 'Smouha', 'Alexandria', geography::Point(31.2089, 29.9559, 4326), 7.00, 1, 1),
    (NEWID(), @Pharma3Id, '19019 - Sporting Branch', 'Sporting', 'Alexandria', geography::Point(31.2183, 29.9392, 4326), 5.00, 1, 0),
    (NEWID(), @Pharma3Id, '19019 - Miami Branch', 'Miami', 'Alexandria', geography::Point(31.2662, 30.0154, 4326), 6.00, 1, 1),

    -- Branches for Misr Pharmacy (Pharmacy 4)
    (NEWID(), @Pharma4Id, 'Misr - Mansoura Main Branch', 'Mansoura', 'Dakahlia', geography::Point(31.0409, 31.3785, 4326), 5.00, 1, 1),
    (NEWID(), @Pharma4Id, 'Misr - Talkha Branch', 'Talkha', 'Dakahlia', geography::Point(31.0567, 31.3850, 4326), 4.50, 1, 1),
    (NEWID(), @Pharma4Id, 'Misr - University Branch', 'Mansoura', 'Dakahlia', geography::Point(31.0425, 31.3570, 4326), 3.00, 1, 1),

    -- Branches for Care Pharmacy (Pharmacy 5)
    (NEWID(), @Pharma5Id, 'Care - Tanta Branch', 'Tanta', 'Gharbia', geography::Point(30.7865, 31.0004, 4326), 6.00, 1, 1),
    (NEWID(), @Pharma5Id, 'Care - Kafr El-Zayat Branch', 'Kafr El-Zayat', 'Gharbia', geography::Point(30.8228, 30.8143, 4326), 8.00, 1, 1),
    (NEWID(), @Pharma5Id, 'Care - Mahalla Branch', 'El Mahalla El Kubra', 'Gharbia', geography::Point(30.9763, 31.1685, 4326), 7.50, 1, 1);

    -- ------------------------------------------------------------------------------------
    -- 8. COMMIT TRANSACTION
    -- ------------------------------------------------------------------------------------
    COMMIT TRANSACTION;
    PRINT 'Seeding completed successfully!';
END TRY
BEGIN CATCH
    -- Rollback on error
    ROLLBACK TRANSACTION;
    PRINT 'Error encountered during seeding. Transaction rolled back!';
    THROW;
END CATCH;

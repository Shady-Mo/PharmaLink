BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PrescriptionReviews]') AND [c].[name] = N'ProcessingStatus');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [PrescriptionReviews] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [PrescriptionReviews] ADD DEFAULT N'PendingPharmacistReview' FOR [ProcessingStatus];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PrescriptionReviewMedicines]') AND [c].[name] = N'MatchStatus');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [PrescriptionReviewMedicines] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [PrescriptionReviewMedicines] ADD DEFAULT N'NotFound' FOR [MatchStatus];

CREATE TABLE [MedicalInquiries] (
    [MedicalInquiryId] uniqueidentifier NOT NULL,
    [PatientUserId] uniqueidentifier NOT NULL,
    [Question] nvarchar(2000) NOT NULL,
    [Answer] nvarchar(4000) NULL,
    [Status] nvarchar(32) NOT NULL,
    [AnsweredByUserId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [AnsweredAt] datetime2 NULL,
    CONSTRAINT [PK_MedicalInquiries] PRIMARY KEY ([MedicalInquiryId]),
    CONSTRAINT [FK_MedicalInquiries_AspNetUsers_AnsweredByUserId] FOREIGN KEY ([AnsweredByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MedicalInquiries_AspNetUsers_PatientUserId] FOREIGN KEY ([PatientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_MedicalInquiries_AnsweredByUserId] ON [MedicalInquiries] ([AnsweredByUserId]);

CREATE INDEX [IX_MedicalInquiries_PatientUserId] ON [MedicalInquiries] ([PatientUserId]);

CREATE INDEX [IX_MedicalInquiries_Status_CreatedAt] ON [MedicalInquiries] ([Status], [CreatedAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260731215546_AddMedicalInquiriesAndReviewTeam', N'10.0.9');

COMMIT;
GO



GO
DECLARE @PatientId INT;
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, RegisteredAt, IsActive)
VALUES ('Anna', 'Bennett', '1990-04-12', 'Female',
        '+1-555-0142', 'anna.bennett@example.com', '12 Birchwood Lane, Springfield', GETUTCDATE(), 1);
SET @PatientId = SCOPE_IDENTITY();

INSERT INTO PatientHistoryEntries (PatientId, Title, Details, RecordDate, CreatedAt, AttachmentFileName, AttachmentStoredPath)
VALUES (@PatientId, 'Full Medical History', 'See attached document.', '2026-06-18', GETUTCDATE(),
        'Anna_Bennett_history.docx', '22b742b5-bec5-4630-b8c2-db6d3f7ec76e.docx');


GO
DECLARE @PatientId INT;
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, RegisteredAt, IsActive)
VALUES ('Maria', 'Torres', '1985-09-03', 'Female',
        '+1-555-0198', 'maria.torres@example.com', '45 Elm Street, Riverside', GETUTCDATE(), 1);
SET @PatientId = SCOPE_IDENTITY();

INSERT INTO PatientHistoryEntries (PatientId, Title, Details, RecordDate, CreatedAt, AttachmentFileName, AttachmentStoredPath)
VALUES (@PatientId, 'Full Medical History', 'See attached document.', '2026-06-18', GETUTCDATE(),
        'Maria_Torres_history.docx', 'e308b6a0-c837-4cdf-b0d7-47fff5e199ea.docx');


GO
DECLARE @PatientId INT;
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, RegisteredAt, IsActive)
VALUES ('David', 'Coleman', '1972-01-22', 'Male',
        '+1-555-0176', 'david.coleman@example.com', '8 Maple Avenue, Fairview', GETUTCDATE(), 1);
SET @PatientId = SCOPE_IDENTITY();

INSERT INTO PatientHistoryEntries (PatientId, Title, Details, RecordDate, CreatedAt, AttachmentFileName, AttachmentStoredPath)
VALUES (@PatientId, 'Full Medical History', 'See attached document.', '2026-06-18', GETUTCDATE(),
        'David_Coleman_history.docx', 'c9a34895-ee08-4517-a1fc-386b2b489f1e.docx');


GO
DECLARE @PatientId INT;
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, RegisteredAt, IsActive)
VALUES ('James', 'Carter', '1995-07-30', 'Male',
        '+1-555-0164', 'james.carter@example.com', '23 Cedar Court, Lakeview', GETUTCDATE(), 1);
SET @PatientId = SCOPE_IDENTITY();

INSERT INTO PatientHistoryEntries (PatientId, Title, Details, RecordDate, CreatedAt, AttachmentFileName, AttachmentStoredPath)
VALUES (@PatientId, 'Full Medical History', 'See attached document.', '2026-06-18', GETUTCDATE(),
        'James_Carter_history.docx', '13ec6ba4-50e2-4b7e-b65a-964b50127269.docx');


GO
DECLARE @PatientId INT;
INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, Address, RegisteredAt, IsActive)
VALUES ('Sarah', 'Mitchell', '1988-11-15', 'Female',
        '+1-555-0187', 'sarah.mitchell@example.com', '17 Willow Drive, Brookside', GETUTCDATE(), 1);
SET @PatientId = SCOPE_IDENTITY();

INSERT INTO PatientHistoryEntries (PatientId, Title, Details, RecordDate, CreatedAt, AttachmentFileName, AttachmentStoredPath)
VALUES (@PatientId, 'Full Medical History', 'See attached document.', '2026-06-18', GETUTCDATE(),
        'Sarah_Mitchell_history.docx', 'ea505bb5-2324-4c56-9c4f-bdadde5994f1.docx');

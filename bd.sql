USE master;
GO

IF DB_ID('BloodBank') IS NOT NULL
BEGIN
    ALTER DATABASE BloodBank SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BloodBank;
END
GO

CREATE DATABASE BloodBank COLLATE Cyrillic_General_CI_AS;
GO

USE BloodBank;
GO

CREATE TABLE Recipients (
    RecipientId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(300) NULL,
    ContactPhone NVARCHAR(20) NULL,
    ContactPerson NVARCHAR(150) NULL
);
GO

CREATE TABLE Employees (
    EmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Login NVARCHAR(50) NOT NULL CONSTRAINT UQ_Employees_Login UNIQUE,
    Password NVARCHAR(100) NOT NULL,
    Position NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL CONSTRAINT DF_Employees_Role DEFAULT 'Регистратор' CONSTRAINT CK_Employees_Role CHECK (Role IN ('Регистратор', 'Медсестра', 'Врач', 'Лаборант', 'Заведующий')),
    ContactInfo NVARCHAR(100) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1
);
GO

CREATE TABLE Donors (
    DonorId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    BirthDate DATE NOT NULL,
    Gender NCHAR(1) NOT NULL CONSTRAINT CK_Donors_Gender CHECK (Gender IN ('М', 'Ж')),
    PassportData NVARCHAR(50) NOT NULL,
    BloodGroup NVARCHAR(5) NOT NULL CONSTRAINT CK_Donors_BloodGroup CHECK (BloodGroup IN ('I', 'II', 'III', 'IV')),
    RhFactor NCHAR(1) NOT NULL CONSTRAINT CK_Donors_RhFactor CHECK (RhFactor IN ('+', '-')),
    KellAntigen NVARCHAR(10) NULL,
    Address NVARCHAR(300) NULL,
    ContactPhone NVARCHAR(20) NULL,
    Email NVARCHAR(150) NULL,
    RegistrationDate DATE NOT NULL CONSTRAINT DF_Donors_RegDate DEFAULT CAST(GETDATE() AS DATE),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Donors_Status DEFAULT 'Активен' CONSTRAINT CK_Donors_Status CHECK (Status IN ('Активен', 'Временное отстранение', 'Постоянное отстранение')),
    DisqualifiedUntil DATE NULL,
    IsHonoraryDonor BIT NOT NULL CONSTRAINT DF_Donors_Honorary DEFAULT 0,
    HonoraryDonorNumber NVARCHAR(50) NULL,
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_Donors_Passport UNIQUE (PassportData)
);
GO

CREATE TABLE MedicalExams (
    ExamId INT IDENTITY(1,1) PRIMARY KEY,
    DonorId INT NOT NULL CONSTRAINT FK_MedExams_Donor REFERENCES Donors(DonorId),
    EmployeeId INT NOT NULL CONSTRAINT FK_MedExams_Employee REFERENCES Employees(EmployeeId),
    ExamDate DATE NOT NULL CONSTRAINT DF_MedExams_Date DEFAULT CAST(GETDATE() AS DATE),
    Result NVARCHAR(20) NOT NULL CONSTRAINT CK_MedExams_Result CHECK (Result IN ('Допущен', 'Отведён')),
    RejectionReason NVARCHAR(200) NULL,
    HemoglobinGdl DECIMAL(5,2) NULL,
    SystolicBP SMALLINT NULL,
    DiastolicBP SMALLINT NULL,
    PulseBpm SMALLINT NULL,
    WeightKg DECIMAL(5,1) NULL,
    TemperatureC DECIMAL(4,1) NULL,
    TotalProteinGdl DECIMAL(5,2) NULL,
    AltUL DECIMAL(6,2) NULL,
    Notes NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Donations (
    DonationId INT IDENTITY(1,1) PRIMARY KEY,
    DonationNumber NVARCHAR(50) NOT NULL,
    DonorId INT NOT NULL CONSTRAINT FK_Donations_Donor REFERENCES Donors(DonorId),
    EmployeeId INT NOT NULL CONSTRAINT FK_Donations_Employee REFERENCES Employees(EmployeeId),
    ExamId INT NULL CONSTRAINT FK_Donations_Exam REFERENCES MedicalExams(ExamId),
    DonationDate DATE NOT NULL CONSTRAINT DF_Donations_Date DEFAULT CAST(GETDATE() AS DATE),
    DonationType NVARCHAR(50) NOT NULL CONSTRAINT CK_Donations_Type CHECK (DonationType IN ('Цельная кровь', 'Плазма', 'Тромбоциты', 'Эритроциты (аферез)', 'Гранулоциты')),
    VolumeMl INT NOT NULL CONSTRAINT CK_Donations_Volume CHECK (VolumeMl > 0),
    MedicalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Donations_Status DEFAULT 'На проверке' CONSTRAINT CK_Donations_Status CHECK (MedicalStatus IN ('На проверке', 'Допущено', 'Брак')),
    CONSTRAINT UQ_Donations_Number UNIQUE (DonationNumber)
);
GO

CREATE TABLE LaboratoryTests (
    TestId INT IDENTITY(1,1) PRIMARY KEY,
    DonationId INT NOT NULL CONSTRAINT FK_LabTests_Donation REFERENCES Donations(DonationId),
    EmployeeId INT NOT NULL CONSTRAINT FK_LabTests_Employee REFERENCES Employees(EmployeeId),
    TestDate DATE NOT NULL CONSTRAINT DF_LabTests_Date DEFAULT CAST(GETDATE() AS DATE),
    HIV_Result NVARCHAR(20) NOT NULL CONSTRAINT CK_LabTests_HIV CHECK (HIV_Result IN ('Отрицательный', 'Положительный', 'Сомнительный')),
    HBsAg_Result NVARCHAR(20) NOT NULL CONSTRAINT CK_LabTests_HBsAg CHECK (HBsAg_Result IN ('Отрицательный', 'Положительный', 'Сомнительный')),
    HCV_Result NVARCHAR(20) NOT NULL CONSTRAINT CK_LabTests_HCV CHECK (HCV_Result IN ('Отрицательный', 'Положительный', 'Сомнительный')),
    Syphilis_Result NVARCHAR(20) NOT NULL CONSTRAINT CK_LabTests_Syphilis CHECK (Syphilis_Result IN ('Отрицательный', 'Положительный', 'Сомнительный')),
    AltUL DECIMAL(6,2) NULL,
    NAT_HIV NVARCHAR(20) NULL CONSTRAINT CK_LabTests_NAT_HIV CHECK (NAT_HIV IN ('Отрицательный', 'Положительный')),
    NAT_HBV NVARCHAR(20) NULL CONSTRAINT CK_LabTests_NAT_HBV CHECK (NAT_HBV IN ('Отрицательный', 'Положительный')),
    NAT_HCV NVARCHAR(20) NULL CONSTRAINT CK_LabTests_NAT_HCV CHECK (NAT_HCV IN ('Отрицательный', 'Положительный')),
    OverallResult NVARCHAR(20) NOT NULL CONSTRAINT CK_LabTests_Overall CHECK (OverallResult IN ('Годен', 'Брак')),
    Notes NVARCHAR(MAX) NULL
);
GO

CREATE TABLE BloodComponents (
    ComponentId INT IDENTITY(1,1) PRIMARY KEY,
    DonationId INT NOT NULL CONSTRAINT FK_Components_Donation REFERENCES Donations(DonationId),
    LotNumber NVARCHAR(50) NOT NULL,
    ComponentType NVARCHAR(100) NOT NULL CONSTRAINT CK_Components_Type CHECK (ComponentType IN ('Эритроцитарная масса', 'Свежезамороженная плазма', 'Тромбоцитарный концентрат', 'Криопреципитат', 'Лейкоцитарная масса', 'Гранулоцитарная масса')),
    VolumeMl INT NOT NULL CONSTRAINT CK_Components_Volume CHECK (VolumeMl > 0),
    CollectionDate DATE NOT NULL,
    ExpirationDate DATE NOT NULL,
    StorageLocation NVARCHAR(100) NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Components_Status DEFAULT 'В наличии' CONSTRAINT CK_Components_Status CHECK (Status IN ('В наличии', 'На карантине', 'Забронировано', 'Выдано', 'Утилизировано')),
    CONSTRAINT CK_Components_Expiry CHECK (ExpirationDate > CollectionDate),
    CONSTRAINT UQ_Components_LotNumber UNIQUE (LotNumber)
);
GO

CREATE TABLE PlasmaQuarantine (
    QuarantineId INT IDENTITY(1,1) PRIMARY KEY,
    ComponentId INT NOT NULL CONSTRAINT FK_Quarantine_Component REFERENCES BloodComponents(ComponentId),
    StartDate DATE NOT NULL,
    PlannedReleaseDate AS CAST(DATEADD(DAY, 180, StartDate) AS DATE) PERSISTED,
    ConfirmationDonationId INT NULL CONSTRAINT FK_Quarantine_ConfirmDonation REFERENCES Donations(DonationId),
    ConfirmationTestId INT NULL CONSTRAINT FK_Quarantine_ConfirmTest REFERENCES LaboratoryTests(TestId),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Quarantine_Status DEFAULT 'На карантине' CONSTRAINT CK_Quarantine_Status CHECK (Status IN ('На карантине', 'Снят с карантина', 'Утилизирован')),
    ReleasedDate DATE NULL,
    ReleasedByEmployeeId INT NULL CONSTRAINT FK_Quarantine_ReleasedBy REFERENCES Employees(EmployeeId),
    CONSTRAINT UQ_Quarantine_Component UNIQUE (ComponentId)
);
GO

CREATE TABLE ComponentIssues (
    IssueId INT IDENTITY(1,1) PRIMARY KEY,
    ComponentId INT NOT NULL CONSTRAINT FK_Issues_Component REFERENCES BloodComponents(ComponentId),
    EmployeeId INT NOT NULL CONSTRAINT FK_Issues_Employee REFERENCES Employees(EmployeeId),
    RecipientId INT NULL CONSTRAINT FK_Issues_Recipient REFERENCES Recipients(RecipientId),
    IssueDate DATE NOT NULL CONSTRAINT DF_Issues_Date DEFAULT CAST(GETDATE() AS DATE),
    IssueType NVARCHAR(20) NOT NULL CONSTRAINT DF_Issues_Type DEFAULT 'Выдача' CONSTRAINT CK_Issues_Type CHECK (IssueType IN ('Выдача', 'Списание')),
    WriteOffReason NVARCHAR(100) NULL,
    Comments NVARCHAR(MAX) NULL
);
GO

CREATE TABLE AuditLog (
    LogId BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NULL CONSTRAINT FK_AuditLog_Employee REFERENCES Employees(EmployeeId),
    ActionTimestamp DATETIME2(3) NOT NULL CONSTRAINT DF_AuditLog_Timestamp DEFAULT SYSDATETIME(),
    TableName NVARCHAR(100) NOT NULL,
    RecordId INT NULL,
    ActionType NVARCHAR(10) NOT NULL CONSTRAINT CK_AuditLog_ActionType CHECK (ActionType IN ('INSERT', 'UPDATE', 'DELETE')),
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    IPAddress NVARCHAR(50) NULL
);
GO

CREATE INDEX IX_Donors_BloodGroup ON Donors(BloodGroup, RhFactor);
CREATE INDEX IX_Donors_Status ON Donors(Status);
CREATE INDEX IX_Donors_FullName ON Donors(FullName);
CREATE INDEX IX_Donations_DonorId ON Donations(DonorId);
CREATE INDEX IX_Donations_Date ON Donations(DonationDate);
CREATE INDEX IX_Donations_DonorDate ON Donations(DonorId, DonationDate);
CREATE INDEX IX_Components_Status ON BloodComponents(Status);
CREATE INDEX IX_Components_Type ON BloodComponents(ComponentType);
CREATE INDEX IX_Components_Expiry ON BloodComponents(ExpirationDate);
CREATE INDEX IX_Issues_Date ON ComponentIssues(IssueDate);
CREATE INDEX IX_LabTests_DonationId ON LaboratoryTests(DonationId);
CREATE INDEX IX_Quarantine_Status ON PlasmaQuarantine(Status);
CREATE INDEX IX_AuditLog_EmployeeTime ON AuditLog(EmployeeId, ActionTimestamp);
CREATE INDEX IX_AuditLog_TableTime ON AuditLog(TableName, ActionTimestamp);
GO

INSERT INTO Recipients (Name, Address, ContactPhone, ContactPerson) VALUES
('ГКБ №1 им. Пирогова', 'ул. Ленина, 1', '+7-900-111-0001', 'Иванова М.С.'),
('Областная клиническая больница', 'пр. Победы, 15', '+7-900-222-0002', NULL),
('Городская детская больница №3', NULL, '+7-900-333-0003', 'Кузнецова Л.П.');
GO

INSERT INTO Employees (FullName, Login, Password, Position, Role, ContactInfo) VALUES
('Смирнова Ольга Владимировна', 'smirnova', '123456', 'Заведующая отделением', 'Заведующий', 'smirnova@sspk.ru'),
('Козлов Андрей Сергеевич', 'kozlov', '123456', 'Врач-трансфузиолог', 'Врач', 'kozlov@sspk.ru'),
('Новикова Елена Ивановна', 'novikova', '123456', 'Медицинская сестра', 'Медсестра', NULL),
('Лебедева Наталья Юрьевна', 'lebedeva', '123456', 'Лаборант', 'Лаборант', 'lebedeva@sspk.ru');
GO

INSERT INTO Donors (FullName, BirthDate, Gender, PassportData, BloodGroup, RhFactor, KellAntigen, ContactPhone, Status, RegistrationDate) VALUES
('Алексеев Пётр Николаевич', '1985-03-14', 'М', '4510 123456', 'I', '+', NULL, '+7-916-100-0001', 'Активен', '2025-11-15'),
('Белова Анна Сергеевна', '1992-07-22', 'Ж', '4511 234567', 'II', '+', 'K-', '+7-916-100-0002', 'Активен', '2026-04-01'),
('Гришин Кирилл Алексеевич', '1978-11-05', 'М', '4512 345678', 'II', '-', 'K+', '+7-916-100-0003', 'Активен', '2026-05-31'),
('Дмитриева Светлана Игоревна', '1990-01-30', 'Ж', '4513 456789', 'III', '+', NULL, NULL, 'Активен', '2026-05-31'),
('Захаров Сергей Иванович', '1975-06-27', 'М', '4516 789012', 'III', '-', 'K-', '+7-916-100-0007', 'Временное отстранение', '2026-05-31'),
('Иванов Максим Викторович', '1998-02-15', 'М', '4517 111222', 'I', '+', 'K-', '+7-999-123-4567', 'Активен', '2026-01-10'),
('Соколова Дарья Андреевна', '2001-08-20', 'Ж', '4517 222333', 'IV', '+', 'K-', '+7-999-234-5678', 'Активен', '2026-01-15'),
('Морозов Антон Игоревич', '1989-12-05', 'М', '4518 333444', 'II', '+', NULL, '+7-999-345-6789', 'Активен', '2026-02-05'),
('Волкова Мария Сергеевна', '1995-04-18', 'Ж', '4518 444555', 'III', '+', 'K+', '+7-999-456-7890', 'Постоянное отстранение', '2026-02-20'),
('Лебедев Дмитрий Олегович', '1982-09-30', 'М', '4519 555666', 'I', '-', 'K-', '+7-999-567-8901', 'Активен', '2026-03-12'),
('Ковалева Анна Павловна', '1990-11-11', 'Ж', '4519 666777', 'II', '-', 'K-', '+7-999-678-9012', 'Активен', '2026-03-25'),
('Попов Илья Владимирович', '1993-07-07', 'М', '4520 777888', 'I', '+', NULL, '+7-999-789-0123', 'Активен', '2026-04-10'),
('Смирнова Екатерина Ильинична', '1987-05-25', 'Ж', '4520 888999', 'IV', '-', 'K-', '+7-999-890-1234', 'Активен', '2026-04-18'),
('Титов Роман Николаевич', '2000-01-01', 'М', '4521 999000', 'III', '-', 'K-', '+7-999-901-2345', 'Активен', '2026-05-05'),
('Орлова Виктория Денисовна', '1999-10-10', 'Ж', '4521 000111', 'II', '+', 'K-', '+7-999-012-3456', 'Временное отстранение', '2026-05-15');
GO

INSERT INTO MedicalExams (DonorId, EmployeeId, ExamDate, Result, RejectionReason, HemoglobinGdl, SystolicBP, DiastolicBP, PulseBpm, WeightKg, TemperatureC, TotalProteinGdl, AltUL) VALUES
(1, 2, '2025-11-15', 'Допущен', NULL, 138.0, 120, 80, 68, 78.0, 36.6, 70.5, 25.0),
(2, 2, '2026-04-01', 'Допущен', NULL, 128.5, 115, 75, 72, 62.0, 36.7, 68.0, 18.0),
(3, 2, '2026-05-31', 'Допущен', NULL, 141.0, 125, 82, 65, 85.5, 36.5, 72.0, 30.0),
(4, 2, '2026-05-31', 'Допущен', NULL, 122.0, 118, 78, 74, 58.0, 36.8, 66.5, 22.0),
(5, 2, '2026-05-31', 'Отведён', 'Высокое давление', NULL, 155, 95, 98, 72.0, 37.0, NULL, NULL),
(6, 2, '2026-01-10', 'Допущен', NULL, 145.0, 120, 80, 70, 80.0, 36.5, 71.0, 22.0),
(7, 2, '2026-01-15', 'Допущен', NULL, 130.0, 110, 70, 75, 60.0, 36.6, 69.0, 19.0),
(8, 2, '2026-02-05', 'Допущен', NULL, 150.0, 125, 85, 68, 90.0, 36.4, 75.0, 28.0),
(9, 2, '2026-02-20', 'Допущен', NULL, 125.0, 115, 75, 80, 55.0, 36.7, 68.5, 20.0),
(10, 2, '2026-03-12', 'Допущен', NULL, 140.0, 120, 80, 72, 82.0, 36.5, 72.0, 24.0),
(11, 2, '2026-03-25', 'Допущен', NULL, 132.0, 118, 78, 74, 65.0, 36.6, 70.0, 21.0),
(12, 2, '2026-04-10', 'Допущен', NULL, 148.0, 130, 85, 65, 88.0, 36.5, 74.0, 26.0),
(13, 2, '2026-04-18', 'Допущен', NULL, 128.0, 110, 70, 76, 59.0, 36.8, 67.0, 18.0),
(14, 2, '2026-05-05', 'Допущен', NULL, 142.0, 122, 82, 70, 84.0, 36.6, 73.0, 25.0),
(15, 2, '2026-05-15', 'Отведён', 'Низкий гемоглобин', 110.0, 115, 75, 78, 61.0, 36.7, NULL, NULL);
GO

INSERT INTO Donations (DonationNumber, DonorId, EmployeeId, ExamId, DonationDate, DonationType, VolumeMl, MedicalStatus) VALUES
('DON-2025-1115-01', 1, 3, 1, '2025-11-15', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0401-02', 2, 3, 2, '2026-04-01', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0110-03', 6, 3, 6, '2026-01-10', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0115-04', 7, 3, 7, '2026-01-15', 'Плазма', 600, 'Допущено'),
('DON-2026-0205-05', 8, 3, 8, '2026-02-05', 'Тромбоциты', 200, 'Допущено'),
('DON-2026-0220-06', 9, 3, 9, '2026-02-20', 'Цельная кровь', 450, 'Брак'),
('DON-2026-0312-07', 10, 3, 10, '2026-03-12', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0325-08', 11, 3, 11, '2026-03-25', 'Плазма', 600, 'Допущено'),
('DON-2026-0410-09', 12, 3, 12, '2026-04-10', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0418-10', 13, 3, 13, '2026-04-18', 'Тромбоциты', 200, 'Допущено'),
('DON-2026-0505-11', 14, 3, 14, '2026-05-05', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0531-12', 3, 3, 3, '2026-05-31', 'Плазма', 600, 'На проверке');
GO

INSERT INTO LaboratoryTests (DonationId, EmployeeId, TestDate, HIV_Result, HBsAg_Result, HCV_Result, Syphilis_Result, AltUL, OverallResult) VALUES
(1, 4, '2025-11-16', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 25.0, 'Годен'),
(2, 4, '2026-04-02', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 18.0, 'Годен'),
(3, 4, '2026-01-11', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 22.0, 'Годен'),
(4, 4, '2026-01-16', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 19.0, 'Годен'),
(5, 4, '2026-02-06', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 28.0, 'Годен'),
(6, 4, '2026-02-21', 'Положительный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 20.0, 'Брак'),
(7, 4, '2026-03-13', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 24.0, 'Годен'),
(8, 4, '2026-03-26', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 21.0, 'Годен'),
(9, 4, '2026-04-11', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 26.0, 'Годен'),
(10, 4, '2026-04-19', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 18.0, 'Годен'),
(11, 4, '2026-05-06', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 25.0, 'Годен');
GO

INSERT INTO BloodComponents (DonationId, LotNumber, ComponentType, VolumeMl, CollectionDate, ExpirationDate, StorageLocation, Status) VALUES
(1, 'LOT-20251115-1A', 'Эритроцитарная масса', 280, '2025-11-15', '2025-12-27', 'Холодильник №1', 'Выдано'),
(1, 'LOT-20251115-1B', 'Свежезамороженная плазма', 170, '2025-11-15', '2028-11-15', 'Морозильник №1', 'В наличии'), -- <-- ВОТ ТУТ ПОФИКСИЛ
(2, 'LOT-20260401-2A', 'Эритроцитарная масса', 280, '2026-04-01', '2026-05-13', 'Холодильник №2', 'Утилизировано'),
(2, 'LOT-20260401-2B', 'Свежезамороженная плазма', 170, '2026-04-01', '2029-04-01', 'Морозильник №2', 'На карантине'),
(3, 'LOT-20260110-3A', 'Эритроцитарная масса', 280, '2026-01-10', '2026-02-21', 'Холодильник №1', 'Выдано'),
(3, 'LOT-20260110-3B', 'Свежезамороженная плазма', 170, '2026-01-10', '2029-01-10', 'Морозильник №1', 'На карантине'),
(4, 'LOT-20260115-4A', 'Свежезамороженная плазма', 600, '2026-01-15', '2029-01-15', 'Морозильник №3', 'На карантине'),
(5, 'LOT-20260205-5A', 'Тромбоцитарный концентрат', 200, '2026-02-05', '2026-02-10', 'Тромбомиксер', 'Выдано'),
(7, 'LOT-20260312-7A', 'Эритроцитарная масса', 280, '2026-03-12', '2026-04-23', 'Холодильник №2', 'Выдано'),
(7, 'LOT-20260312-7B', 'Свежезамороженная плазма', 170, '2026-03-12', '2029-03-12', 'Морозильник №2', 'На карантине'),
(8, 'LOT-20260325-8A', 'Свежезамороженная плазма', 600, '2026-03-25', '2029-03-25', 'Морозильник №1', 'На карантине'),
(9, 'LOT-20260410-9A', 'Эритроцитарная масса', 280, '2026-04-10', '2026-05-22', 'Холодильник №1', 'Утилизировано'),
(9, 'LOT-20260410-9B', 'Свежезамороженная плазма', 170, '2026-04-10', '2029-04-10', 'Морозильник №3', 'На карантине'),
(10, 'LOT-20260418-10A', 'Тромбоцитарный концентрат', 200, '2026-04-18', '2026-04-23', 'Тромбомиксер', 'Выдано'),
(11, 'LOT-20260505-11A', 'Эритроцитарная масса', 280, '2026-05-05', '2026-06-16', 'Холодильник №2', 'В наличии'),
(11, 'LOT-20260505-11B', 'Свежезамороженная плазма', 170, '2026-05-05', '2029-05-05', 'Морозильник №2', 'На карантине');
GO

INSERT INTO PlasmaQuarantine (ComponentId, StartDate, Status) VALUES
(2, '2025-11-15', 'Снят с карантина'),
(4, '2026-04-01', 'На карантине'),
(6, '2026-01-10', 'На карантине'),
(7, '2026-01-15', 'На карантине'),
(10, '2026-03-12', 'На карантине'),
(11, '2026-03-25', 'На карантине'),
(13, '2026-04-10', 'На карантине'),
(16, '2026-05-05', 'На карантине');
GO

INSERT INTO ComponentIssues (ComponentId, EmployeeId, RecipientId, IssueDate, IssueType, WriteOffReason, Comments) VALUES
(1, 4, 1, '2025-11-20', 'Выдача', NULL, 'Плановая операция'),
(3, 4, NULL, '2026-05-14', 'Списание', 'Истёк срок годности', 'Не востребовано ЛПУ, срок 42 дня истек'),
(5, 4, 2, '2026-01-15', 'Выдача', NULL, 'Экстренная операция'),
(8, 4, 3, '2026-02-08', 'Выдача', NULL, 'Детская онкология'),
(9, 4, 1, '2026-03-20', 'Выдача', NULL, 'Плановая хирургия'),
(12, 4, NULL, '2026-05-23', 'Списание', 'Нарушение температурного режима', 'Сбой холодильника'),
(14, 4, 2, '2026-04-20', 'Выдача', NULL, 'Гематология');
GO

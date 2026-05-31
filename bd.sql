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
('Захаров Сергей Иванович', '1975-06-27', 'М', '4516 789012', 'III', '-', 'K-', '+7-916-100-0007', 'Временное отстранение', '2026-05-31');
GO

INSERT INTO MedicalExams (DonorId, EmployeeId, ExamDate, Result, RejectionReason, HemoglobinGdl, SystolicBP, DiastolicBP, PulseBpm, WeightKg, TemperatureC, TotalProteinGdl, AltUL) VALUES
(1, 2, '2025-11-15', 'Допущен', NULL, 138.0, 120, 80, 68, 78.0, 36.6, 70.5, 25.0),
(2, 2, '2026-04-01', 'Допущен', NULL, 128.5, 115, 75, 72, 62.0, 36.7, 68.0, 18.0),
(3, 2, '2026-05-31', 'Допущен', NULL, 141.0, 125, 82, 65, 85.5, 36.5, 72.0, 30.0),
(4, 2, '2026-05-31', 'Допущен', NULL, 122.0, 118, 78, 74, 58.0, 36.8, 66.5, 22.0),
(5, 2, '2026-05-31', 'Отведён', 'Высокое давление, тахикардия', NULL, 155, 95, 98, 72.0, 37.0, NULL, NULL);
GO

INSERT INTO Donations (DonationNumber, DonorId, EmployeeId, ExamId, DonationDate, DonationType, VolumeMl, MedicalStatus) VALUES
('DON-2025-1115-01', 1, 3, 1, '2025-11-15', 'Цельная кровь', 450, 'Допущено'),
('DON-2026-0401-02', 2, 3, 2, '2026-04-01', 'Цельная кровь', 450, 'Допущено');
GO

INSERT INTO LaboratoryTests (DonationId, EmployeeId, TestDate, HIV_Result, HBsAg_Result, HCV_Result, Syphilis_Result, AltUL, OverallResult) VALUES
(1, 4, '2025-11-16', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 25.0, 'Годен'),
(2, 4, '2026-04-02', 'Отрицательный', 'Отрицательный', 'Отрицательный', 'Отрицательный', 18.0, 'Годен');
GO

INSERT INTO BloodComponents (DonationId, LotNumber, ComponentType, VolumeMl, CollectionDate, ExpirationDate, StorageLocation, Status) VALUES
(1, 'LOT-20251115-1A', 'Эритроцитарная масса', 280, '2025-11-15', '2025-12-27', 'Холодильник №1', 'Выдано'),
(1, 'LOT-20251115-1B', 'Свежезамороженная плазма', 170, '2025-11-15', '2028-11-15', 'Морозильник №1', 'На карантине'), -- Карантин прошел, можно снимать!
(2, 'LOT-20260401-2A', 'Эритроцитарная масса', 280, '2026-04-01', '2026-05-13', 'Холодильник №2', 'Утилизировано'), -- Срок годности истек до 31 мая
(2, 'LOT-20260401-2B', 'Свежезамороженная плазма', 170, '2026-04-01', '2029-04-01', 'Морозильник №2', 'На карантине');
GO

INSERT INTO PlasmaQuarantine (ComponentId, StartDate, Status) VALUES
(2, '2025-11-15', 'На карантине'), -- Плановая дата выхода: 2026-05-14 (уже прошла, готова к выдаче)
(4, '2026-04-01', 'На карантине'); -- Выйдет только в сентябре 2026
GO

INSERT INTO ComponentIssues (ComponentId, EmployeeId, RecipientId, IssueDate, IssueType, WriteOffReason, Comments) VALUES
(1, 4, 1, '2025-11-20', 'Выдача', NULL, 'Плановая операция'),
(3, 4, NULL, '2026-05-14', 'Списание', 'Истёк срок годности', 'Не востребовано ЛПУ, срок 42 дня истек');
GO
USE master;
GO

IF DB_ID('BloodBank') IS NOT NULL
BEGIN
    ALTER DATABASE BloodBank SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BloodBank;
END
GO

CREATE DATABASE BloodBank
    COLLATE Cyrillic_General_CI_AS;
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
    Login NVARCHAR(50) UNIQUE NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Position NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL
        CONSTRAINT DF_Employees_Role DEFAULT 'Регистратор'
        CONSTRAINT CK_Employees_Role CHECK (Role IN ('Регистратор', 'Медсестра', 'Врач', 'Лаборант', 'Заведующий')),
    ContactInfo NVARCHAR(100) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1
);
GO

CREATE TABLE Donors (
    DonorId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    BirthDate DATE NOT NULL,
    PassportData NVARCHAR(50) NOT NULL,
    BloodGroup NVARCHAR(5) NOT NULL
        CONSTRAINT CK_Donors_BloodGroup CHECK (BloodGroup IN ('I', 'II', 'III', 'IV')),
    RhFactor NCHAR(1) NOT NULL
        CONSTRAINT CK_Donors_RhFactor CHECK (RhFactor IN ('+', '-')),
    KellAntigen NVARCHAR(10) NULL,
    ContactPhone NVARCHAR(20) NULL,
    RegistrationDate DATE NOT NULL CONSTRAINT DF_Donors_RegDate DEFAULT CAST(GETDATE() AS DATE),
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Donors_Status DEFAULT 'Активен'
        CONSTRAINT CK_Donors_Status CHECK (Status IN ('Активен', 'Временное отстранение', 'Постоянное отстранение')),
    DisqualifiedUntil DATE NULL,
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_Donors_Passport UNIQUE (PassportData)
);
GO

CREATE TABLE MedicalExams (
    ExamId INT IDENTITY(1,1) PRIMARY KEY,
    DonorId INT NOT NULL
        CONSTRAINT FK_MedExams_Donor REFERENCES Donors(DonorId),
    EmployeeId INT NOT NULL
        CONSTRAINT FK_MedExams_Employee REFERENCES Employees(EmployeeId),
    ExamDate DATE NOT NULL CONSTRAINT DF_MedExams_Date DEFAULT CAST(GETDATE() AS DATE),
    Result NVARCHAR(20) NOT NULL
        CONSTRAINT CK_MedExams_Result CHECK (Result IN ('Допущен', 'Отведён')),
    RejectionReason NVARCHAR(200) NULL,
    HemoglobinGdl DECIMAL(5,2) NULL,
    BloodPressure NVARCHAR(20) NULL,
    PulseBpm SMALLINT NULL,
    WeightKg DECIMAL(5,1) NULL,
    TemperatureC DECIMAL(4,1) NULL,
    Notes NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Donations (
    DonationId INT IDENTITY(1,1) PRIMARY KEY,
    DonorId INT NOT NULL
        CONSTRAINT FK_Donations_Donor REFERENCES Donors(DonorId),
    EmployeeId INT NOT NULL
        CONSTRAINT FK_Donations_Employee REFERENCES Employees(EmployeeId),
    ExamId INT NULL
        CONSTRAINT FK_Donations_Exam REFERENCES MedicalExams(ExamId),
    DonationDate DATE NOT NULL CONSTRAINT DF_Donations_Date DEFAULT CAST(GETDATE() AS DATE),
    DonationType NVARCHAR(50) NOT NULL
        CONSTRAINT CK_Donations_Type CHECK (DonationType IN ('Цельная кровь', 'Плазма', 'Тромбоциты', 'Эритроциты (аферез)', 'Гранулоциты')),
    VolumeMl INT NOT NULL CONSTRAINT CK_Donations_Volume CHECK (VolumeMl > 0),
    MedicalStatus NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Donations_Status DEFAULT 'На проверке'
        CONSTRAINT CK_Donations_Status CHECK (MedicalStatus IN ('На проверке', 'Допущено', 'Брак'))
);
GO

CREATE TABLE BloodComponents (
    ComponentId INT IDENTITY(1,1) PRIMARY KEY,
    DonationId INT NOT NULL
        CONSTRAINT FK_Components_Donation REFERENCES Donations(DonationId),
    LotNumber NVARCHAR(50) NOT NULL,
    ComponentType NVARCHAR(100) NOT NULL
        CONSTRAINT CK_Components_Type CHECK (ComponentType IN ('Эритроцитарная масса', 'Свежезамороженная плазма', 'Тромбоцитарный концентрат', 'Криопреципитат', 'Лейкоцитарная масса', 'Гранулоцитарная масса')),
    VolumeMl INT NOT NULL CONSTRAINT CK_Components_Volume CHECK (VolumeMl > 0),
    CollectionDate DATE NOT NULL,
    ExpirationDate DATE NOT NULL,
    StorageLocation NVARCHAR(100) NULL,
    Status NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Components_Status DEFAULT 'В наличии'
        CONSTRAINT CK_Components_Status CHECK (Status IN ('В наличии', 'Забронировано', 'Выдано', 'Утилизировано')),
    CONSTRAINT CK_Components_Expiry CHECK (ExpirationDate > CollectionDate)
);
GO

CREATE TABLE ComponentIssues (
    IssueId INT IDENTITY(1,1) PRIMARY KEY,
    ComponentId INT NOT NULL
        CONSTRAINT FK_Issues_Component REFERENCES BloodComponents(ComponentId),
    EmployeeId INT NOT NULL
        CONSTRAINT FK_Issues_Employee REFERENCES Employees(EmployeeId),
    RecipientId INT NULL
        CONSTRAINT FK_Issues_Recipient REFERENCES Recipients(RecipientId),
    IssueDate DATE NOT NULL CONSTRAINT DF_Issues_Date DEFAULT CAST(GETDATE() AS DATE),
    IssueType NVARCHAR(20) NOT NULL
        CONSTRAINT DF_Issues_Type DEFAULT 'Выдача'
        CONSTRAINT CK_Issues_Type CHECK (IssueType IN ('Выдача', 'Списание')),
    WriteOffReason NVARCHAR(100) NULL,
    Comments NVARCHAR(MAX) NULL
);
GO

CREATE INDEX IX_Donors_BloodGroup ON Donors(BloodGroup, RhFactor);
CREATE INDEX IX_Donors_Status ON Donors(Status);
CREATE INDEX IX_Donations_DonorId ON Donations(DonorId);
CREATE INDEX IX_Donations_Date ON Donations(DonationDate);
CREATE INDEX IX_Components_Status ON BloodComponents(Status);
CREATE INDEX IX_Components_Type ON BloodComponents(ComponentType);
CREATE INDEX IX_Components_Expiry ON BloodComponents(ExpirationDate);
CREATE INDEX IX_Issues_Date ON ComponentIssues(IssueDate);
GO

INSERT INTO Recipients (Name, Address, ContactPhone, ContactPerson) VALUES
('ГКБ №1 им. Пирогова', 'ул. Ленина, 1', '+7-900-111-0001', 'Иванова М.С.'),
('Областная клиническая больница', 'пр. Победы, 15', '+7-900-222-0002', 'Петров Д.А.'),
('Городская детская больница №3', 'ул. Садовая, 22', '+7-900-333-0003', 'Кузнецова Л.П.'),
('Онкологический диспансер', 'ул. Советская, 5', '+7-900-444-0004', 'Морозов В.Е.'),
('Перинатальный центр', 'ул. Гагарина, 88', '+7-900-555-0005', 'Соколова Т.И.');
GO

INSERT INTO Employees (FullName, Login, Password, Position, Role, ContactInfo) VALUES
('Смирнова Ольга Владимировна', 'smirnova', '123456', 'Заведующая отделением', 'Заведующий', 'smirnova@sspk.ru'),
('Козлов Андрей Сергеевич', 'kozlov', '123456', 'Врач-трансфузиолог', 'Врач', 'kozlov@sspk.ru'),
('Новикова Елена Ивановна', 'novikova', '123456', 'Медицинская сестра', 'Медсестра', 'novikova@sspk.ru'),
('Волков Дмитрий Павлович', 'volkov', '123456', 'Медицинская сестра', 'Медсестра', 'volkov@sspk.ru'),
('Лебедева Наталья Юрьевна', 'lebedeva', '123456', 'Лаборант', 'Лаборант', 'lebedeva@sspk.ru'),
('Фёдоров Игорь Константинович', 'fedorov', '123456', 'Регистратор', 'Регистратор', 'fedorov@sspk.ru');
GO

INSERT INTO Donors (FullName, BirthDate, PassportData, BloodGroup, RhFactor, KellAntigen, ContactPhone, Status) VALUES
('Алексеев Пётр Николаевич', '1985-03-14', '4510 123456', 'I', '+', 'K-', '+7-916-100-0001', 'Активен'),
('Белова Анна Сергеевна', '1992-07-22', '4511 234567', 'II', '+', 'K-', '+7-916-100-0002', 'Активен'),
('Гришин Кирилл Алексеевич', '1978-11-05', '4512 345678', 'II', '-', 'K+', '+7-916-100-0003', 'Активен'),
('Дмитриева Светлана Игоревна', '1990-01-30', '4513 456789', 'III', '+', 'K-', '+7-916-100-0004', 'Активен'),
('Ежов Роман Викторович', '1983-09-18', '4514 567890', 'IV', '+', 'K-', '+7-916-100-0005', 'Активен'),
('Жукова Марина Олеговна', '1995-04-12', '4515 678901', 'I', '-', 'K-', '+7-916-100-0006', 'Активен'),
('Захаров Сергей Иванович', '1975-06-27', '4516 789012', 'III', '-', 'K-', '+7-916-100-0007', 'Временное отстранение'),
('Иванов Михаил Дмитриевич', '2000-02-14', '4517 890123', 'I', '+', 'K-', '+7-916-100-0008', 'Активен'),
('Калинина Юлия Андреевна', '1988-08-03', '4518 901234', 'II', '+', 'K+', '+7-916-100-0009', 'Активен'),
('Логинов Владимир Семёнович', '1970-12-25', '4519 012345', 'IV', '-', 'K-', '+7-916-100-0010', 'Активен');
GO

INSERT INTO MedicalExams (DonorId, EmployeeId, ExamDate, Result, HemoglobinGdl, BloodPressure, PulseBpm, WeightKg, TemperatureC) VALUES
(1, 2, '2024-09-10', 'Допущен', 138.0, '120/80', 68, 78.0, 36.6),
(2, 2, '2024-09-10', 'Допущен', 124.5, '115/75', 72, 62.0, 36.7),
(3, 2, '2024-09-11', 'Допущен', 141.0, '125/82', 65, 85.5, 36.5),
(4, 2, '2024-09-11', 'Допущен', 122.0, '118/78', 74, 58.0, 36.8),
(5, 2, '2024-09-12', 'Допущен', 145.0, '130/85', 70, 90.0, 36.6),
(6, 2, '2024-09-12', 'Допущен', 119.5, '110/70', 76, 55.0, 36.7),
(7, 2, '2024-09-13', 'Отведён', NULL, '140/95', 88, 72.0, 37.2),
(8, 2, '2024-09-13', 'Допущен', 136.0, '118/76', 66, 69.0, 36.5),
(9, 2, '2024-10-01', 'Допущен', 128.0, '122/80', 71, 65.5, 36.6),
(10, 2, '2024-10-01', 'Допущен', 133.5, '128/84', 68, 80.0, 36.6);
GO

UPDATE Donors
SET Status = 'Временное отстранение', DisqualifiedUntil = '2024-11-13'
WHERE DonorId = 7;
GO

INSERT INTO Donations (DonorId, EmployeeId, ExamId, DonationDate, DonationType, VolumeMl, MedicalStatus) VALUES
(1, 3, 1, '2024-09-10', 'Цельная кровь', 450, 'Допущено'),
(2, 3, 2, '2024-09-10', 'Плазма', 600, 'Допущено'),
(3, 4, 3, '2024-09-11', 'Цельная кровь', 450, 'Допущено'),
(4, 3, 4, '2024-09-11', 'Тромбоциты', 300, 'Допущено'),
(5, 4, 5, '2024-09-12', 'Цельная кровь', 450, 'Брак'),
(6, 3, 6, '2024-09-12', 'Плазма', 600, 'Допущено'),
(8, 4, 8, '2024-09-13', 'Цельная кровь', 450, 'Допущено'),
(9, 3, 9, '2024-10-01', 'Плазма', 600, 'Допущено'),
(10, 4, 10, '2024-10-01', 'Цельная кровь', 450, 'Допущено'),
(1, 3, NULL, '2024-10-15', 'Цельная кровь', 450, 'На проверке');
GO

INSERT INTO BloodComponents (DonationId, LotNumber, ComponentType, VolumeMl, CollectionDate, ExpirationDate, StorageLocation, Status) VALUES
(1, 'LOT-2024-001-A', 'Эритроцитарная масса', 280, '2024-09-10', '2024-10-22', 'Холодильник №2, полка A', 'Выдано'),
(1, 'LOT-2024-001-B', 'Свежезамороженная плазма', 170, '2024-09-10', '2025-09-10', 'Морозильник №1, отсек 3', 'В наличии'),
(2, 'LOT-2024-002-A', 'Свежезамороженная плазма', 560, '2024-09-10', '2025-09-10', 'Морозильник №1, отсек 4', 'В наличии'),
(2, 'LOT-2024-002-B', 'Криопреципитат', 40, '2024-09-10', '2025-09-10', 'Морозильник №2, отсек 1', 'Забронировано'),
(3, 'LOT-2024-003-A', 'Эритроцитарная масса', 280, '2024-09-11', '2024-10-23', 'Холодильник №2, полка B', 'В наличии'),
(3, 'LOT-2024-003-B', 'Свежезамороженная плазма', 170, '2024-09-11', '2025-09-11', 'Морозильник №1, отсек 5', 'В наличии'),
(4, 'LOT-2024-004-A', 'Тромбоцитарный концентрат', 300, '2024-09-11', '2024-09-16', 'Термостат тромбоцитов №1', 'Утилизировано'),
(6, 'LOT-2024-006-A', 'Свежезамороженная плазма', 560, '2024-09-12', '2025-09-12', 'Морозильник №1, отсек 6', 'В наличии'),
(7, 'LOT-2024-007-A', 'Эритроцитарная масса', 280, '2024-09-13', '2024-10-25', 'Холодильник №2, полка C', 'В наличии'),
(7, 'LOT-2024-007-B', 'Свежезамороженная плазма', 170, '2024-09-13', '2025-09-13', 'Морозильник №1, отсек 7', 'В наличии'),
(8, 'LOT-2024-008-A', 'Свежезамороженная плазма', 560, '2024-10-01', '2025-10-01', 'Морозильник №2, отсек 2', 'В наличии'),
(9, 'LOT-2024-009-A', 'Эритроцитарная масса', 280, '2024-10-01', '2024-11-12', 'Холодильник №2, полка D', 'В наличии'),
(9, 'LOT-2024-009-B', 'Свежезамороженная плазма', 170, '2024-10-01', '2025-10-01', 'Морозильник №2, отсек 3', 'В наличии');
GO

INSERT INTO ComponentIssues (ComponentId, EmployeeId, RecipientId, IssueDate, IssueType, WriteOffReason, Comments) VALUES
(1, 5, 1, '2024-09-15', 'Выдача', NULL, 'Плановая операция, зарезервировано заранее'),
(7, 5, NULL, '2024-09-17', 'Списание', 'Истёк срок годности', 'Не удалось использовать в течение 5 дней'),
(4, 5, 4, '2024-10-05', 'Выдача', NULL, 'Экстренный запрос для пациента с гемофилией');
GO
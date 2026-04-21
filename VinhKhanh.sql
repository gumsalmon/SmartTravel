-- ==========================================
-- SCRIPT KHỞI TẠO DATABASE VINHKHANHTOUR (FINAL ENTERPRISE VERSION)
-- Tính năng: Đa ngôn ngữ, Quản lý sạp, Bán vé, Offline Sync (UUID, UpdatedAt, IsDeleted)
-- ==========================================

USE master;
GO

-- 0. Xóa database cũ nếu đã tồn tại để làm mới hoàn toàn
IF DB_ID('VinhKhanhTourDB') IS NOT NULL
BEGIN
    ALTER DATABASE VinhKhanhTourDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE VinhKhanhTourDB;
    PRINT N'Đã xóa Database cũ!';
END
GO

CREATE DATABASE VinhKhanhTourDB;
PRINT N'Đã tạo Database VinhKhanhTourDB mới!';
GO

USE VinhKhanhTourDB;
GO

-- ==========================================
-- PHẦN 1: TẠO CÁC BẢNG ĐỘC LẬP (KHÔNG CÓ KHÓA NGOẠI)
-- ==========================================

-- 1. Bảng Ngôn ngữ
CREATE TABLE Languages (
    lang_code NVARCHAR(10) PRIMARY KEY,
    lang_name NVARCHAR(50) NOT NULL,
    flag_icon_url NVARCHAR(255),
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE()
);

-- 2. Bảng Lộ trình Tour
CREATE TABLE Tours (
    id INT IDENTITY(1,1) PRIMARY KEY,
    tour_name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url NVARCHAR(500),
    is_active BIT DEFAULT 1,
    is_top_hot BIT DEFAULT 0,
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE()
);

-- 3. Bảng Tài khoản (Admin & Chủ sạp)
CREATE TABLE Users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(100),
    role NVARCHAR(20) DEFAULT 'StallOwner',
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE()
);

-- 4. Bảng Gói Vé Du Khách (Ticket Packages)
CREATE TABLE TicketPackages (
    id INT IDENTITY(1,1) PRIMARY KEY,
    package_name NVARCHAR(100) NOT NULL,
    price DECIMAL(18, 2) NOT NULL,
    duration_hours INT NOT NULL, 
    is_active BIT DEFAULT 1,
    updated_at DATETIME DEFAULT GETDATE()
);

-- ==========================================
-- PHẦN 2: TẠO CÁC BẢNG CÓ LIÊN KẾT KHÓA NGOẠI (CẤP 1)
-- ==========================================

-- 5. Bảng Sạp Hàng (Liên kết Users và Tours)
CREATE TABLE Stalls (
    id INT IDENTITY(1,1) PRIMARY KEY,
    owner_id INT NULL,
    TourID INT NULL,
    name_default NVARCHAR(255) NOT NULL,
    latitude FLOAT NOT NULL,
    longitude FLOAT NOT NULL,
    radius_meter INT DEFAULT 50,
    is_open BIT DEFAULT 1,
    image_thumb NVARCHAR(500),
    sort_order INT DEFAULT 0 NOT NULL,
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT CHK_Stall_Coords CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180),
    CONSTRAINT FK_Stalls_Users FOREIGN KEY (owner_id) REFERENCES Users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Stalls_Tours FOREIGN KEY (TourID) REFERENCES Tours(id) ON DELETE SET NULL ON UPDATE CASCADE
);

-- 6. Bảng Gói Cước Thiết Bị Chủ Sạp (Subscriptions)
CREATE TABLE Subscriptions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    device_id NVARCHAR(255) NOT NULL,
    activation_code NVARCHAR(100) UNIQUE,
    start_date DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME, 
    is_active BIT DEFAULT 1,
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Subscriptions_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE
);
CREATE TABLE SubscriptionTransactions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    amount DECIMAL(18,2) NOT NULL, -- Số tiền thực tế thu (sau khi giảm giá nếu có)
    payment_date DATETIME DEFAULT GETDATE(),
    duration_days INT, -- Gia hạn bao nhiêu ngày (30, 90, 365)
    note NVARCHAR(255), -- Ghi chú: "Gia hạn tháng 4", "Admin tặng gói thử"
    
    CONSTRAINT FK_Trans_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id)
);

-- 7. Bảng Lịch Sử Mua Vé Của Khách (Liên kết TicketPackages)
CREATE TABLE TouristTickets (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ticket_code NVARCHAR(50) UNIQUE NOT NULL, 
    device_id NVARCHAR(255) NOT NULL,
    package_id INT NOT NULL,
    amount_paid DECIMAL(18,2) NOT NULL, 
    payment_method NVARCHAR(50) DEFAULT 'Mock_VNPay',
    created_at DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME NOT NULL, 

    CONSTRAINT FK_Tickets_Packages FOREIGN KEY (package_id) REFERENCES TicketPackages(id) ON DELETE CASCADE
);

-- ==========================================
-- PHẦN 3: TẠO CÁC BẢNG LIÊN KẾT CẤP 2 (CHI TIẾT SẠP)
-- ==========================================

-- 8. Bảng Nội dung thuyết minh TTS (Liên kết Stalls và Languages)
CREATE TABLE StallContents (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    tts_script NVARCHAR(1000),
    is_active BIT DEFAULT 1,
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_StallContent_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_StallContent_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_Stall_Lang UNIQUE (stall_id, lang_code)
);

-- 9. Bảng Món Ăn / Sản Phẩm
CREATE TABLE Products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    base_price DECIMAL(18, 2) DEFAULT 0,
    image_url NVARCHAR(500),
    is_signature BIT DEFAULT 0,
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Product_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 10. Bảng Thống Kê Lượt Khách Ghé Sạp (Dùng UUID)
CREATE TABLE StallVisits (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    stall_id INT NOT NULL,
    device_id NVARCHAR(255),
    visited_at DATETIME DEFAULT GETDATE(),
    created_at_server DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_StallVisits_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 11. Bảng Dịch Thuật Món Ăn
CREATE TABLE ProductTranslations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    product_name NVARCHAR(255) NOT NULL,
    product_desc NVARCHAR(500),
    is_deleted BIT DEFAULT 0,
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_ProdTrans_Prod FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_ProdTrans_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_Prod_Lang UNIQUE (product_id, lang_code)
);
GO
-- 1. Cập nhật bảng StallVisits: Thêm cột để lưu thời gian nghe audio
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StallVisits') AND name = 'listen_duration_seconds')
BEGIN
    ALTER TABLE StallVisits 
    ADD listen_duration_seconds INT DEFAULT 0;
    PRINT N'Đã thêm cột listen_duration_seconds vào bảng StallVisits.';
END
GO

-- 2. Tạo bảng TouristTrajectories: Lưu vết chân người dùng (Dùng cho Heatmap & Tuyến đi)
IF OBJECT_ID('TouristTrajectories', 'U') IS NULL
BEGIN
    CREATE TABLE TouristTrajectories (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        device_id NVARCHAR(255) NOT NULL, -- Định danh ẩn danh của thiết bị
        latitude FLOAT NOT NULL,
        longitude FLOAT NOT NULL,
        recorded_at DATETIME DEFAULT GETDATE(),
        
        -- Ràng buộc với bảng vé để đảm bảo dữ liệu hợp lệ (tùy chọn)
        -- CONSTRAINT FK_Trajectories_Tickets FOREIGN KEY (device_id) REFERENCES TouristTickets(device_id) ON DELETE CASCADE
    );

    -- Tạo Index để Admin truy vấn Heatmap và Route theo thời gian cực nhanh
    CREATE NONCLUSTERED INDEX IX_Trajectory_Device_Time ON TouristTrajectories(device_id, recorded_at);
    PRINT N'Đã tạo bảng TouristTrajectories và Index thành công.';
END
GO
-- ==========================================
-- PHẦN 4: TẠO TRIGGER CẬP NHẬT THỜI GIAN (DELTA SYNC)
-- ==========================================

CREATE TRIGGER TRG_UpdateTourTime ON Tours AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(updated_at)
        UPDATE t SET updated_at = GETDATE() FROM Tours t INNER JOIN inserted i ON t.id = i.id;
END;
GO

CREATE TRIGGER TRG_UpdateStallTime ON Stalls AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(updated_at)
        UPDATE s SET updated_at = GETDATE() FROM Stalls s INNER JOIN inserted i ON s.id = i.id;
END;
GO

CREATE TRIGGER TRG_UpdateProductTime ON Products AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(updated_at)
        UPDATE p SET updated_at = GETDATE() FROM Products p INNER JOIN inserted i ON p.id = i.id;
END;
GO

CREATE TRIGGER TRG_UpdateContentTime ON StallContents AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(updated_at)
        UPDATE c SET updated_at = GETDATE() FROM StallContents c INNER JOIN inserted i ON c.id = i.id;
END;
GO

CREATE TRIGGER TRG_UpdateTranslationTime ON ProductTranslations AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(updated_at)
        UPDATE pt SET updated_at = GETDATE() FROM ProductTranslations pt INNER JOIN inserted i ON pt.id = i.id;
END;
GO

-- Thêm cột cho bảng nội dung sạp
ALTER TABLE StallContents 
ADD is_processed BIT DEFAULT 0 NOT NULL;

-- Thêm cột cho bảng dịch tên món ăn
ALTER TABLE ProductTranslations 
ADD is_processed BIT DEFAULT 0 NOT NULL;
GO
-- ==========================================
-- PHẦN 5: BƠM DỮ LIỆU MỒI (SEED DATA)
-- ==========================================

-- Bơm 10 Ngôn Ngữ
INSERT INTO Languages (lang_code, lang_name, flag_icon_url)
VALUES 
    ('vi', N'Tiếng Việt', '/icons/flags/vi.png'),
    ('en', N'English', '/icons/flags/en.png'),
    ('ja', N'日本語 (Nhật Bản)', '/icons/flags/ja.png'),
    ('ko', N'한국어 (Hàn Quốc)', '/icons/flags/ko.png'),
    ('zh', N'中文 (Trung Quốc)', '/icons/flags/zh.png'),
    ('fr', N'Français (Pháp)', '/icons/flags/fr.png'),
    ('es', N'Español (Tây Ban Nha)', '/icons/flags/es.png'),
    ('de', N'Deutsch (Đức)', '/icons/flags/de.png'),
    ('th', N'ภาษาไทย (Thái Lan)', '/icons/flags/th.png'),
    ('ru', N'Русский (Nga)', '/icons/flags/ru.png');
GO

-- Bơm 3 Gói Vé Du Lịch
INSERT INTO TicketPackages (package_name, price, duration_hours, is_active)
VALUES 
    (N'Gói Khám Phá Nhanh (2 Giờ)', 50000.00, 2, 1),
    (N'Gói Trải Nghiệm Tiêu Chuẩn (24 Giờ)', 150000.00, 24, 1),
    (N'Gói Bản Địa Không Giới Hạn (1 Tuần)', 300000.00, 168, 1);
GO

-- Bơm Tài khoản Admin mặc định (Pass: 123456 - Đã Hash BCrypt)
INSERT INTO Users (username, password_hash, full_name, role)
VALUES ('admin', '$2a$11$n/A1qU55YyC7o2s1K0kC1O/0wA1oHh5X2w3E1z8e7H7A9R2lX4m', N'System Admin', 'Admin');
GO

PRINT N'✅ HOÀN TẤT SETUP DATABASE VINHKHANHTOURDB THÀNH CÔNG!';

-- ==========================================
-- SCRIPT TẠO DATABASE VINHKHANHTOUR (BẢN CHUẨN MỚI NHẤT)
-- Tính năng: 1 User sở hữu nhiều Sạp hàng, ID tự động tăng và cập nhật
-- ==========================================

CREATE DATABASE VinhKhanhTourDB;
GO
USE VinhKhanhTourDB;
GO

-- 1. Bảng Ngôn ngữ (Không dùng ID tự tăng vì LangCode là khóa chính)
CREATE TABLE Languages (
    lang_code NVARCHAR(10) PRIMARY KEY, -- VD: 'vi', 'en', 'kr', 'jp'
    lang_name NVARCHAR(50) NOT NULL,
    flag_icon_url NVARCHAR(255)
);

-- 2. Bảng Tours (Lộ trình)
CREATE TABLE Tours (
    id INT IDENTITY(1,1) PRIMARY KEY, -- IDENTITY(1,1): Tự động tăng ID
    tour_name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    image_url NVARCHAR(500),
    is_active BIT DEFAULT 1
);

-- 3. Bảng Users (Tài khoản) - ĐÃ XÓA stall_id
CREATE TABLE Users (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username NVARCHAR(50) NOT NULL UNIQUE,
    password_hash NVARCHAR(255) NOT NULL,
    full_name NVARCHAR(100),
    role NVARCHAR(20) DEFAULT 'StallOwner'
);

-- 4. Bảng Stalls (Sạp hàng) - ĐÃ THÊM owner_id
CREATE TABLE Stalls (
    id INT IDENTITY(1,1) PRIMARY KEY,
    owner_id INT NULL, -- Mối quan hệ: 1 User có thể có mã ID ở nhiều sạp
    TourID INT NULL,
    name_default NVARCHAR(255) NOT NULL,
    latitude FLOAT NOT NULL,
    longitude FLOAT NOT NULL,
    radius_meter INT DEFAULT 50,
    is_open BIT DEFAULT 1,
    image_thumb NVARCHAR(500),
    updated_at DATETIME DEFAULT GETDATE(),

    CONSTRAINT CHK_Stall_Coords CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180),
    -- ON UPDATE CASCADE: Tự động cập nhật ID nếu bảng cha thay đổi
    CONSTRAINT FK_Stalls_Users FOREIGN KEY (owner_id) REFERENCES Users(id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Stalls_Tours FOREIGN KEY (TourID) REFERENCES Tours(id) ON DELETE SET NULL ON UPDATE CASCADE
);

-- 5. Bảng StallContents (Nội dung thuyết minh)
CREATE TABLE StallContents (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    tts_script NVARCHAR(1000),
    is_active BIT DEFAULT 1,

    CONSTRAINT FK_StallContent_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_StallContent_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_Stall_Lang UNIQUE (stall_id, lang_code),
    CONSTRAINT CHK_TTS_Length CHECK (LEN(tts_script) <= 1000)
);

-- 6. Bảng Products (Sản phẩm / Món ăn)
CREATE TABLE Products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    base_price DECIMAL(18, 2) DEFAULT 0,
    image_url NVARCHAR(500),
    is_signature BIT DEFAULT 0,

    CONSTRAINT FK_Product_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT CHK_Price CHECK (base_price >= 0)
);

-- 7. Bảng ProductTranslations (Dịch thuật món ăn)
CREATE TABLE ProductTranslations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    product_name NVARCHAR(255) NOT NULL,
    product_desc NVARCHAR(500),

    CONSTRAINT FK_ProdTrans_Prod FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_ProdTrans_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_Prod_Lang UNIQUE (product_id, lang_code),
    CONSTRAINT CHK_ProdDesc_Length CHECK (LEN(product_desc) <= 500)
);

-- 8. Bảng Subscriptions (Gói cước thiết bị)
CREATE TABLE Subscriptions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    device_id NVARCHAR(255) NOT NULL,
    activation_code NVARCHAR(100) UNIQUE,
    start_date DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME, 
    is_active BIT DEFAULT 1
);

-- Trigger tự động cập nhật thời gian (updated_at) cho bảng Stalls
GO
CREATE TRIGGER TRG_UpdateStallTime
ON Stalls
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Nếu không phải đang update cột updated_at thì mới cập nhật thời gian hiện tại
    IF NOT UPDATE(updated_at)
    BEGIN
        UPDATE s
        SET updated_at = GETDATE()
        FROM Stalls s
        INNER JOIN inserted i ON s.id = i.id;
    END
END;
GO

ALTER TABLE Tours ADD is_top_hot BIT DEFAULT 0;
GO

CREATE TABLE StallVisits (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    device_id NVARCHAR(255),
    visited_at DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_StallVisits_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE
);
GO
ALTER TABLE Stalls ADD sort_order INT DEFAULT 0 NOT NULL;

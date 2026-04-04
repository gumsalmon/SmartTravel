-- ==========================================
-- SCRIPT TẠO DATABASE VINHKHANHTOUR (FINAL VERSION)
-- Tính năng: Đa ngôn ngữ, Quản lý sạp, Bán vé khách du lịch (1 tuần), Thống kê
-- Tính năng: Đa ngôn ngữ, Quản lý sạp, Bán vé khách du lịch (1 tuần), Thống kê, Quản lý gói cước
-- ==========================================

USE master;
GO

-- Xóa database cũ nếu đã tồn tại để làm mới hoàn toàn
IF DB_ID('VinhKhanhTourDB') IS NOT NULL
BEGIN
ALTER DATABASE VinhKhanhTourDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE VinhKhanhTourDB;
END
GO

CREATE DATABASE VinhKhanhTourDB;
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
flag_icon_url NVARCHAR(255)
);

-- 2. Bảng Lộ trình Tour
CREATE TABLE Tours (
id INT IDENTITY(1,1) PRIMARY KEY,
tour_name NVARCHAR(255) NOT NULL,
description NVARCHAR(MAX),
image_url NVARCHAR(500),
is_active BIT DEFAULT 1,
is_top_hot BIT DEFAULT 0
);

-- 3. Bảng Tài khoản (Admin & Chủ sạp)
CREATE TABLE Users (
id INT IDENTITY(1,1) PRIMARY KEY,
username NVARCHAR(50) NOT NULL UNIQUE,
password_hash NVARCHAR(255) NOT NULL,
full_name NVARCHAR(100),
role NVARCHAR(20) DEFAULT 'StallOwner'
);

-- 4. Bảng Gói Vé Du Khách (Ticket Packages)
CREATE TABLE TicketPackages (
id INT IDENTITY(1,1) PRIMARY KEY,
package_name NVARCHAR(100) NOT NULL,
price DECIMAL(18, 2) NOT NULL,
duration_hours INT NOT NULL, -- Thời hạn tính bằng giờ (VD: 168h = 1 tuần)
is_active BIT DEFAULT 1,
updated_at DATETIME DEFAULT GETDATE()
);

-- 5. Bảng Gói Cước Thiết Bị Chủ Sạp (Subscriptions)
CREATE TABLE Subscriptions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    device_id NVARCHAR(255) NOT NULL,
    activation_code NVARCHAR(100) UNIQUE,
    start_date DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME, 
    is_active BIT DEFAULT 1
);

-- ==========================================
-- PHẦN 2: TẠO CÁC BẢNG CÓ LIÊN KẾT KHÓA NGOẠI (CẤP 1)
-- ==========================================

-- 6. Bảng Sạp Hàng (Liên kết Users và Tours)
-- 5. Bảng Sạp Hàng (Liên kết Users và Tours)
-- (💡 Chuyển lên trước để bảng Subscriptions có thể móc khóa ngoại vào)
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
updated_at DATETIME DEFAULT GETDATE(),

CONSTRAINT CHK_Stall_Coords CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180),
CONSTRAINT FK_Stalls_Users FOREIGN KEY (owner_id) REFERENCES Users(id) ON DELETE SET NULL ON UPDATE CASCADE,
CONSTRAINT FK_Stalls_Tours FOREIGN KEY (TourID) REFERENCES Tours(id) ON DELETE SET NULL ON UPDATE CASCADE
);

-- 6. Bảng Gói Cước Thiết Bị Chủ Sạp (Subscriptions)
-- (💡 ĐÃ BỔ SUNG: stall_id và Khóa ngoại nối thẳng vào Stalls)
CREATE TABLE Subscriptions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL, -- 💡 Nút thắt là đây
    device_id NVARCHAR(255) NOT NULL,
    activation_code NVARCHAR(100) UNIQUE,
    start_date DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME, 
    is_active BIT DEFAULT 1,

    CONSTRAINT FK_Subscriptions_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE
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

CONSTRAINT FK_Product_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 10. Bảng Thống Kê Lượt Khách Ghé Sạp
CREATE TABLE StallVisits (
id INT IDENTITY(1,1) PRIMARY KEY,
stall_id INT NOT NULL,
device_id NVARCHAR(255),
visited_at DATETIME DEFAULT GETDATE(),

CONSTRAINT FK_StallVisits_Stalls FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE ON UPDATE CASCADE
);

-- 11. Bảng Dịch Thuật Món Ăn
CREATE TABLE ProductTranslations (
id INT IDENTITY(1,1) PRIMARY KEY,
product_id INT NOT NULL,
lang_code NVARCHAR(10) NOT NULL,
product_name NVARCHAR(255) NOT NULL,
product_desc NVARCHAR(500),

CONSTRAINT FK_ProdTrans_Prod FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE ON UPDATE CASCADE,
CONSTRAINT FK_ProdTrans_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code) ON DELETE CASCADE ON UPDATE CASCADE,
CONSTRAINT UQ_Prod_Lang UNIQUE (product_id, lang_code)
);

-- ==========================================
-- PHẦN 4: TRIGGER CẬP NHẬT THỜI GIAN VÀ DỮ LIỆU MỒI
-- PHẦN 4: TRIGGER CẬP NHẬT THỜI GIAN
-- ==========================================
GO

-- Trigger cho bảng Stalls
CREATE TRIGGER TRG_UpdateStallTime
ON Stalls
AFTER UPDATE
AS
BEGIN
SET NOCOUNT ON;
IF NOT UPDATE(updated_at)
BEGIN
UPDATE s
SET updated_at = GETDATE()
FROM Stalls s
INNER JOIN inserted i ON s.id = i.id;
END
END;
GO

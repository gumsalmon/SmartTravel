CREATE DATABASE VinhKhanhTourDB;
GO
USE VinhKhanhTourDB;
GO

-- 1. Bảng Ngôn ngữ
CREATE TABLE Languages (
    lang_code NVARCHAR(10) PRIMARY KEY, -- 'vi', 'en', 'kr', 'jp'
    lang_name NVARCHAR(50) NOT NULL,
    flag_icon_url NVARCHAR(255)
);

-- 2. Bảng Sạp hàng (POI)
CREATE TABLE Stalls (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name_default NVARCHAR(255) NOT NULL,
    latitude FLOAT NOT NULL,
    longitude FLOAT NOT NULL,
    radius_meter INT DEFAULT 50,
    is_open BIT DEFAULT 1, -- Ràng buộc: 1 là Mở, 0 là Đóng
    image_thumb NVARCHAR(500),
    updated_at DATETIME DEFAULT GETDATE(),
    -- RÀNG BUỘC: Tọa độ phải nằm trong dải hợp lý
    CONSTRAINT CHK_Stall_Coords CHECK (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)
);

-- 3. Bảng Nội dung thuyết minh (TTS)
CREATE TABLE StallContents (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    tts_script NVARCHAR(1000), -- GIỚI HẠN: 1000 ký tự để bảo vệ TTS
    is_active BIT DEFAULT 1,
    -- RÀNG BUỘC: Khóa ngoại và duy nhất (1 sạp - 1 ngôn ngữ - 1 bản dịch)
    CONSTRAINT FK_StallContent_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE,
    CONSTRAINT FK_StallContent_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code),
    CONSTRAINT UQ_Stall_Lang UNIQUE (stall_id, lang_code),
    -- RÀNG BUỘC: Kiểm tra độ dài tại tầng DB
    CONSTRAINT CHK_TTS_Length CHECK (LEN(tts_script) <= 1000)
);

-- 4. Bảng Món ăn
CREATE TABLE Products (
    id INT IDENTITY(1,1) PRIMARY KEY,
    stall_id INT NOT NULL,
    base_price DECIMAL(18, 2) DEFAULT 0,
    image_url NVARCHAR(500),
    is_signature BIT DEFAULT 0,
    CONSTRAINT FK_Product_Stall FOREIGN KEY (stall_id) REFERENCES Stalls(id) ON DELETE CASCADE,
    CONSTRAINT CHK_Price CHECK (base_price >= 0)
);

-- 5. Bảng Dịch tên/mô tả món ăn
CREATE TABLE ProductTranslations (
    id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT NOT NULL,
    lang_code NVARCHAR(10) NOT NULL,
    product_name NVARCHAR(255) NOT NULL,
    product_desc NVARCHAR(500), -- GIỚI HẠN: 500 ký tự
    CONSTRAINT FK_ProdTrans_Prod FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE,
    CONSTRAINT FK_ProdTrans_Lang FOREIGN KEY (lang_code) REFERENCES Languages(lang_code),
    CONSTRAINT UQ_Prod_Lang UNIQUE (product_id, lang_code),
    CONSTRAINT CHK_ProdDesc_Length CHECK (LEN(product_desc) <= 500)
);

-- 6. Bảng Gói cước (Kích hoạt 7 ngày)
CREATE TABLE Subscriptions (
    id INT IDENTITY(1,1) PRIMARY KEY,
    device_id NVARCHAR(255) NOT NULL,
    activation_code NVARCHAR(100) UNIQUE,
    start_date DATETIME DEFAULT GETDATE(),
    expiry_date DATETIME, 
    is_active BIT DEFAULT 1
);
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
USE [VinhKhanhTourDB]
GO
/****** Object:  Table [dbo].[Languages]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Languages](
	[lang_code] [nvarchar](10) NOT NULL,
	[lang_name] [nvarchar](50) NOT NULL,
	[flag_icon_url] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[lang_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Products]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Products](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[stall_id] [int] NOT NULL,
	[base_price] [decimal](18, 2) NULL,
	[image_url] [nvarchar](500) NULL,
	[is_signature] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductTranslations]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductTranslations](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[product_id] [int] NOT NULL,
	[lang_code] [nvarchar](10) NOT NULL,
	[product_name] [nvarchar](255) NOT NULL,
	[product_desc] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Prod_Lang] UNIQUE NONCLUSTERED 
(
	[product_id] ASC,
	[lang_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StallContents]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StallContents](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[stall_id] [int] NOT NULL,
	[lang_code] [nvarchar](10) NOT NULL,
	[tts_script] [nvarchar](1000) NULL,
	[is_active] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Stall_Lang] UNIQUE NONCLUSTERED 
(
	[stall_id] ASC,
	[lang_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Stalls]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Stalls](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[TourID] [int] NULL,
	[name_default] [nvarchar](255) NOT NULL,
	[latitude] [float] NOT NULL,
	[longitude] [float] NOT NULL,
	[radius_meter] [int] NULL,
	[is_open] [bit] NULL,
	[image_thumb] [nvarchar](500) NULL,
	[updated_at] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Subscriptions]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Subscriptions](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[device_id] [nvarchar](255) NOT NULL,
	[activation_code] [nvarchar](100) NULL,
	[start_date] [datetime] NULL,
	[expiry_date] [datetime] NULL,
	[is_active] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[activation_code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tours]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tours](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[tour_name] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[image_url] [nvarchar](500) NULL,
	[is_active] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 3/5/2026 3:35:38 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[username] [nvarchar](50) NOT NULL,
	[password_hash] [nvarchar](255) NOT NULL,
	[full_name] [nvarchar](100) NULL,
	[role] [nvarchar](20) NULL,
	[stall_id] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ((0)) FOR [base_price]
GO
ALTER TABLE [dbo].[Products] ADD  DEFAULT ((0)) FOR [is_signature]
GO
ALTER TABLE [dbo].[StallContents] ADD  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[Stalls] ADD  DEFAULT ((50)) FOR [radius_meter]
GO
ALTER TABLE [dbo].[Stalls] ADD  DEFAULT ((1)) FOR [is_open]
GO
ALTER TABLE [dbo].[Stalls] ADD  DEFAULT (getdate()) FOR [updated_at]
GO
ALTER TABLE [dbo].[Subscriptions] ADD  DEFAULT (getdate()) FOR [start_date]
GO
ALTER TABLE [dbo].[Subscriptions] ADD  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[Tours] ADD  DEFAULT ((1)) FOR [is_active]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ('StallOwner') FOR [role]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [FK_Product_Stall] FOREIGN KEY([stall_id])
REFERENCES [dbo].[Stalls] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [FK_Product_Stall]
GO
ALTER TABLE [dbo].[ProductTranslations]  WITH CHECK ADD  CONSTRAINT [FK_ProdTrans_Lang] FOREIGN KEY([lang_code])
REFERENCES [dbo].[Languages] ([lang_code])
GO
ALTER TABLE [dbo].[ProductTranslations] CHECK CONSTRAINT [FK_ProdTrans_Lang]
GO
ALTER TABLE [dbo].[ProductTranslations]  WITH CHECK ADD  CONSTRAINT [FK_ProdTrans_Prod] FOREIGN KEY([product_id])
REFERENCES [dbo].[Products] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ProductTranslations] CHECK CONSTRAINT [FK_ProdTrans_Prod]
GO
ALTER TABLE [dbo].[StallContents]  WITH CHECK ADD  CONSTRAINT [FK_StallContent_Lang] FOREIGN KEY([lang_code])
REFERENCES [dbo].[Languages] ([lang_code])
GO
ALTER TABLE [dbo].[StallContents] CHECK CONSTRAINT [FK_StallContent_Lang]
GO
ALTER TABLE [dbo].[StallContents]  WITH CHECK ADD  CONSTRAINT [FK_StallContent_Stall] FOREIGN KEY([stall_id])
REFERENCES [dbo].[Stalls] ([id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[StallContents] CHECK CONSTRAINT [FK_StallContent_Stall]
GO
ALTER TABLE [dbo].[Stalls]  WITH CHECK ADD  CONSTRAINT [FK_Stalls_Tours] FOREIGN KEY([TourID])
REFERENCES [dbo].[Tours] ([id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Stalls] CHECK CONSTRAINT [FK_Stalls_Tours]
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Stalls] FOREIGN KEY([stall_id])
REFERENCES [dbo].[Stalls] ([id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Stalls]
GO
ALTER TABLE [dbo].[Products]  WITH CHECK ADD  CONSTRAINT [CHK_Price] CHECK  (([base_price]>=(0)))
GO
ALTER TABLE [dbo].[Products] CHECK CONSTRAINT [CHK_Price]
GO
ALTER TABLE [dbo].[ProductTranslations]  WITH CHECK ADD  CONSTRAINT [CHK_ProdDesc_Length] CHECK  ((len([product_desc])<=(500)))
GO
ALTER TABLE [dbo].[ProductTranslations] CHECK CONSTRAINT [CHK_ProdDesc_Length]
GO
ALTER TABLE [dbo].[StallContents]  WITH CHECK ADD  CONSTRAINT [CHK_TTS_Length] CHECK  ((len([tts_script])<=(1000)))
GO
ALTER TABLE [dbo].[StallContents] CHECK CONSTRAINT [CHK_TTS_Length]
GO
ALTER TABLE [dbo].[Stalls]  WITH CHECK ADD  CONSTRAINT [CHK_Stall_Coords] CHECK  (([latitude]>=(-90) AND [latitude]<=(90) AND ([longitude]>=(-180) AND [longitude]<=(180))))
GO
ALTER TABLE [dbo].[Stalls] CHECK CONSTRAINT [CHK_Stall_Coords]
GO

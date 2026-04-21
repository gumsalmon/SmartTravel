-- ==========================================
-- SCRIPT SỬA LỖI HIỂN THỊ SAI KÝ TỰ (MOJIBAKE)
-- Áp dụng cho bảng Languages - SmartTravel Project
-- Cách dùng: Chạy toàn bộ script này trong SSMS (SQL Server Management Studio)
-- ==========================================

USE VinhKhanhTourDB;
GO

-- 1. Xóa toàn bộ dữ liệu cũ trong bảng Languages (để nạp lại chuẩn)
-- Hoặc có thể dùng UPDATE nếu không muốn xóa. Ở đây ta dùng cách an toàn nhất là DELETE và INSERT lại.
DELETE FROM Languages;
GO

-- 2. Nạp lại dữ liệu với tiền tố N'...' để hỗ trợ Unicode tuyệt đối
INSERT INTO Languages (lang_code, lang_name, flag_icon_url, is_deleted, updated_at)
VALUES 
    ('vi', N'Tiếng Việt', '/icons/flags/vi.png', 0, GETDATE()),
    ('en', N'English', '/icons/flags/en.png', 0, GETDATE()),
    ('ja', N'日本語 (Nhật Bản)', '/icons/flags/ja.png', 0, GETDATE()),
    ('ko', N'한국어 (Hàn Quốc)', '/icons/flags/ko.png', 0, GETDATE()),
    ('zh', N'中文 (Trung Quốc)', '/icons/flags/zh.png', 0, GETDATE()),
    ('fr', N'Français (Pháp)', '/icons/flags/fr.png', 0, GETDATE()),
    ('es', N'Español (Tây Ban Nha)', '/icons/flags/es.png', 0, GETDATE()),
    ('de', N'Deutsch (Đức)', '/icons/flags/de.png', 0, GETDATE()),
    ('th', N'ภาษาไทย (Thái Lan)', '/icons/flags/th.png', 0, GETDATE()),
    ('ru', N'Русский (Nga)', '/icons/flags/ru.png', 0, GETDATE());
GO

PRINT N'✅ Đã sửa lỗi hiển thị (Encoding) cho 10 ngôn ngữ thành công!';
SELECT * FROM Languages;

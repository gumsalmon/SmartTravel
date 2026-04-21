-- =============================================
-- SCRIPT SỬA LỖI ENCODING PHIÊN BẢN 2 (CẬP NHẬT 2 NGÔN NGỮ CÒN LẠI)
-- Giải quyết cả mã ngôn ngữ ngắn (zh) và mã dài (zh-hans)
-- =============================================

USE VinhKhanhTourDB;
GO

-- 1. Tiếng Việt
UPDATE Languages SET lang_name = N'Ti' + NCHAR(7871) + N'ng Vi' + NCHAR(7879) + N't' 
WHERE lang_code IN ('vi', 'vi-vn');

-- 2. 日本語 (Nhật Bản)
UPDATE Languages SET lang_name = NCHAR(26085) + NCHAR(26412) + NCHAR(35486) + N' (Nh' + NCHAR(7853) + N't B' + NCHAR(7843) + N'n)' 
WHERE lang_code IN ('ja', 'ja-jp');

-- 3. 한국어 (Hàn Quốc)
UPDATE Languages SET lang_name = NCHAR(54620) + NCHAR(44397) + NCHAR(50612) + N' (H' + NCHAR(224) + N'n Qu' + NCHAR(7889) + N'c)' 
WHERE lang_code IN ('ko', 'ko-kr');

-- 4. 中文 (Trung Quốc) - Sửa lỗi Button 1
UPDATE Languages SET lang_name = NCHAR(20013) + NCHAR(25991) + N' (Trung Qu' + NCHAR(7889) + N'c)' 
WHERE lang_code IN ('zh', 'zh-hans', 'zh-cn');

-- 5. Français (Pháp)
UPDATE Languages SET lang_name = N'Fran' + NCHAR(231) + N'ais (Ph' + NCHAR(225) + N'p)' 
WHERE lang_code IN ('fr', 'fr-fr');

-- 6. Español (Tây Ban Nha)
UPDATE Languages SET lang_name = N'Espa' + NCHAR(241) + N'ol (T' + NCHAR(226) + N'y Ban Nha)' 
WHERE lang_code IN ('es', 'es-es');

-- 7. Русский (Nga)
UPDATE Languages SET lang_name = NCHAR(1056) + NCHAR(1091) + NCHAR(1089) + NCHAR(1089) + NCHAR(1082) + NCHAR(1080) + NCHAR(1081) + N' (Nga)' 
WHERE lang_code IN ('ru', 'ru-ru');

-- 8. ภาษาไทย (Thái Lan)
UPDATE Languages SET lang_name = NCHAR(3616) + NCHAR(3634) + NCHAR(3625) + NCHAR(3634) + NCHAR(3652) + NCHAR(3607) + NCHAR(3618) + N' (Th' + NCHAR(225) + N'i Lan)' 
WHERE lang_code IN ('th', 'th-th');

-- 9. Deutsch (Đức) - Sửa lỗi Button 2
UPDATE Languages SET lang_name = N'Deutsch (' + NCHAR(272) + NCHAR(7913) + N'c)' 
WHERE lang_code IN ('de', 'de-de');

GO

PRINT N'✅ Đã sửa lỗi triệt để cho toàn bộ ngôn ngữ (bao gồm Tiếng Trung và Tiếng Đức).';
SELECT lang_code, lang_name FROM Languages;
GO

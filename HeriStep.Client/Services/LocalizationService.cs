namespace HeriStep.Client.Services
{
    /// <summary>
    /// Centralized runtime localization service.
    /// Usage: L.Get("key") returns the translated string for the current user language.
    /// Call L.SetLanguage("en") to switch language and fire LanguageChanged event.
    /// </summary>
    public static class L
    {
        private static string _currentLang = "vi";

        public static event Action? LanguageChanged;
        public static string CurrentLanguage => _currentLang;
        public static string LastSyncedAudioLanguage => Preferences.Default.Get("last_synced_audio_lang", "vi");

        public static void Init()
        {
            _currentLang = NormalizeLanguageCode(Preferences.Default.Get("user_language", "vi"));
            ApplyCultureSafe(_currentLang);
        }

        public static void SetLanguage(string langCode)
        {
            var normalized = NormalizeLanguageCode(langCode);
            _currentLang = normalized;
            Preferences.Default.Set("user_language", normalized);
            ApplyCultureSafe(normalized);
            LanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (Strings.TryGetValue(_currentLang, out var langDict) && langDict.TryGetValue(key, out var val))
                return val;
            if (Strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
                return enVal;
            return key;
        }

        private static string NormalizeLanguageCode(string? langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode))
            {
                return "vi";
            }

            var code = langCode.Trim().ToLowerInvariant();
            return code switch
            {
                "zh-hans" => "zh",
                "ja-jp" => "ja",
                "ko-kr" => "ko",
                "de-de" => "de",
                "es-es" => "es",
                "ru-ru" => "ru",
                "th-th" => "th",
                _ => code.Split('-')[0]
            };
        }

        private static void ApplyCultureSafe(string normalizedLang)
        {
            try
            {
                var cultureCode = normalizedLang switch
                {
                    "zh" => "zh-Hans",
                    "ja" => "ja-JP",
                    "ko" => "ko-KR",
                    "de" => "de-DE",
                    "es" => "es-ES",
                    "ru" => "ru-RU",
                    "th" => "th-TH",
                    "en" => "en-US",
                    _ => "vi-VN"
                };

                Console.WriteLine($"[LOCALIZATION] Applying culture: {cultureCode}");
                var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureCode);
                
                System.Globalization.CultureInfo.CurrentCulture = culture;
                System.Globalization.CultureInfo.CurrentUICulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
                
                Console.WriteLine($"[LOCALIZATION] Culture {cultureCode} applied successfully.");
            }
            catch (Exception ex)
            {
                // 🔥 CRITICAL: Bắt lỗi để không crash app nếu OS không hỗ trợ Culture đó
                Console.WriteLine($"[LOCALIZATION] ERROR: ApplyCultureSafe failed for {normalizedLang}: {ex.Message}");
            }
        }

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            // ─────────────────────────────────────────────────────────────────
            // VIETNAMESE
            // ─────────────────────────────────────────────────────────────────
            ["vi"] = new()
            {
                ["main_hero_desc"] = "Khám phá khu ẩm thực sầm uất bậc nhất Sài Gòn",
                ["main_cat_title"] = "Hoạt Động Nổi Bật",
                ["main_cat_hot"] = "Đang Hot",
                ["main_cat_seafood"] = "Ốc & Hải Sản",
                ["main_cat_drinks"] = "Đồ Uống",
                ["main_cat_snacks"] = "Ăn Vặt",
                ["main_promo_title"] = "Khám Phá Tự Do 🎧",
                ["main_promo_desc"] = "Đồng bộ GPS — tự phát loa khi trải nghiệm",
                ["main_promo_btn"] = "Thử ngay",
                ["main_badge_visits"] = "🔥 999+ lượt ghé",
                ["main_badge_old"] = "🏅 Hàng quán lâu đời",

                ["tab_explore"] = "Khám Phá", ["tab_map"] = "Bản Đồ", ["tab_aura"] = "Aura", ["tab_profile"] = "Hồ Sơ",
                ["main_header"] = "PHỐ VĨNH KHÁNH", ["main_hero_tag"] = "QUẬN 4 - SAIGON",
                ["main_hero_title"] = "Khám Phá\nTâm Hồn\nQuận 4",
                ["main_hero_desc"] = "Thiên đường ẩm thực đường\nphố sầm uất nhất Sài Gòn.",
                ["main_btn_map"] = "Bản Đồ", ["main_map_desc"] = "Khám phá gần bạn",
                ["main_btn_scan"] = "Quét M/QR", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Top Quán 5 Sao", ["main_top_shops_desc"] = "Được yêu thích nhất",
                ["audio_welcome_stall"] = "Chào mừng bạn đến với {0}!",
                ["main_top_tours_title"] = "🔥 Top 10 Tours Khám Phá", ["main_explore_all"] = "Khám phá tất cả",
                ["main_section_top"] = "Quán Đỉnh Phải\nThử", ["main_view_all"] = "Xem tất\ncả",
                ["main_shop1_tags"] = "🐌 Hải sản · Ốc các loại", ["main_shop2_tags"] = "🥢 Ăn vặt · Đường phố",
                ["main_top5_shops_title"] = "⭐ Top 5 Quán Đỉnh", ["main_top5_shops_fav"] = "👑 Quán Yêu Thích",
                ["search_placeholder"] = "Tìm quán ăn, địa điểm...",

                ["main_voice_tag"] = "VOICE STUDIO", ["main_voice_title"] = "Tùy chỉnh giọng nói",
                ["main_voice_btn"] = "NGHE THỬ", ["main_voice_placeholder"] = "Nhập văn bản để nghe thử...",

                ["map_title"] = "Phố Ẩm Thực Vĩnh Khánh", ["map_active"] = "Đang hoạt động",
                ["map_vacant"] = "Chưa có chủ", ["map_expired"] = "Hết hạn",
                ["map_your_location"] = "Vị trí của bạn", ["map_listen"] = "🔊 Nghe Giới Thiệu",
                ["map_detail"] = "📋 Chi Tiết", ["map_no_owner"] = "Chưa có chủ",
                ["map_status_closed"] = "⛔ Đã đóng", ["map_status_vacant"] = "🟢 Trống", ["map_status_open"] = "🔴 Mở",
                ["map_nearby_label"] = "📍 Sạp gần bạn",
                ["map_play_audio"] = "🔊 Âm Thanh",
                ["map_navigate"] = "🗺️ Chỉ Đường",
                ["map_free_explore_on"] = "🧭 Đang khám phá tự do...",
                ["map_free_explore_off"] = "Khám Phá Tự Do: Tắt",

                ["aura_header"] = "Cài Đặt Voice Aura", ["aura_subtitle"] = "TÙY CHỈNH TRẢI NGHIỆM",
                ["aura_language"] = "Ngôn Ngữ", ["aura_gender"] = "Giới Tính Giọng Nói",
                ["aura_male"] = "Nam", ["aura_female"] = "Nữ",
                ["aura_speed"] = "Tốc Độ Phát", ["aura_speed_slow"] = "CHẬM", ["aura_speed_normal"] = "BÌNH THƯỜNG", ["aura_speed_fast"] = "NHANH",
                ["aura_radius"] = "Bán Kính Thông Báo", ["aura_preview"] = "▶ Nghe Thử Âm Thanh",
                ["aura_save"] = "Lưu Thay Đổi", ["aura_saved_ok"] = "Thiết lập đã được lưu lại.",
                ["aura_error_title"] = "Lỗi", ["aura_error_play"] = "Không thể phát âm thanh",
                ["alert_success"] = "Thành công",

                ["profile_header"] = "Vinh Khanh Guide", ["profile_title"] = "Hồ Sơ",
                ["profile_display_name"] = "Khách Du Lịch",
                ["profile_change_lang"] = "🌐 Đổi Ngôn Ngữ", ["profile_lang_desc"] = "Bấm để thay đổi ngôn ngữ ứng dụng",
                ["profile_change_pkg"] = "🔄 Thay đổi gói cước", ["profile_change_pkg_desc"] = "Chuyển sang màn hình chọn gói",
                ["profile_logout"] = "🚪 Đăng xuất", ["profile_logout_desc"] = "Quay về trang quét QR để chọn lại gói",
                ["profile_logout_confirm"] = "Bạn có chắc muốn đăng xuất và quay về trang quét QR không? Giao dịch cũ sẽ được xóa.",
                ["profile_saved_count"] = "12 địa điểm",
                ["profile_history_item1"] = "Ốc Đảo Vĩnh Khánh", ["profile_history_tag1"] = "Hải sản · Ốc các loại",
                ["profile_history_item2"] = "Bà Sáu Bánh Cuốn", ["profile_history_tag2"] = "Món khai vị truyền thống",
                ["profile_logout"] = "🚪 Đăng xuất", ["profile_logout_desc"] = "Quay về trang quét QR để chọn lại gói",
                ["profile_logout_confirm"] = "Bạn có chắc muốn đăng xuất và quay về trang quét QR không? Giao dịch cũ sẽ được xóa.",
                ["profile_device"] = "Mã máy:", ["profile_expiry_ok"] = "Hạn: CÒN LẠI {0} Ngày {1} Giờ",
                ["profile_expiry_expired"] = "Hạn sử dụng: ĐÃ HẾT HẠN",
                ["profile_visited"] = "Đã ghé", ["profile_saved_lbl"] = "Lưu", ["profile_rating"] = "Xếp hạng",
                ["profile_history"] = "Lịch Sử Gần Đây", ["profile_view_all"] = "Xem Tất Cả",
                ["profile_no_history"] = "Chưa có lịch sử ghé quán",
                ["profile_support"] = "Hỗ trợ", ["profile_support_24"] = "24/7 Trợ giúp",

                ["shop_address"] = "534 Vĩnh Khánh, Phường 10", ["shop_tag_signature"] = "HẢI SẢN NƯỚNG", ["shop_tag_gem"] = "VIÊN NGỌC QUẬN 4",
                ["shop_aura_tag"] = "Aura AI Guide", ["shop_menu_title"] = "Thực Đơn Đặc Biệt",
                ["shop_menu_desc"] = "Hương vị tinh tuyển từ con phố ẩm thực sôi động nhất Sài Gòn.",
                ["shop_add_order"] = "Thêm Vào Đơn", ["shop_aura_active"] = "Voice Aura Đang Hoạt Động",
                ["shop_aura_scan"] = "Quét tên món ăn để nghe câu chuyện bằng ngôn ngữ bạn chọn.",
                ["shop_order_alert"] = "Tính năng đặt món đang được phát triển.",
                ["shop_dish1_desc"] = "Ốc hương rang với muối ớt. Sự cân bằng hoàn hảo giữa độ ngọt của biển và lớp vỏ cay nồng.",
                ["shop_dish2_desc"] = "Rau muống xào nhanh với tỏi thơm lừng.",
                ["shop_dish3_desc"] = "Tôm sú nướng với muối ớt đặc biệt của quán.",
                ["shop_tag_musttry"] = "PHẢI THỬ", ["shop_tag_spicy"] = "CAY",

                ["lang_welcome"] = "Chào mừng đến HeriStep", ["lang_subtitle"] = "Vui lòng chọn quốc tịch để tiếp tục",
                ["lang_footer"] = "Bạn có thể thay đổi lựa chọn này bất cứ lúc nào trong Cài đặt Hồ sơ",
                ["ok"] = "OK", ["close"] = "Đóng", ["notification"] = "Thông báo", ["coming_soon"] = "Sắp ra mắt",

                ["renew_title"] = "Gia hạn gói cước", ["renew_expired"] = "Gói cước đã hết hạn",
                ["renew_info"] = "Vui lòng gia hạn để tiếp tục sử dụng.",
                ["renew_device"] = "Mã thiết bị:", ["renew_choose_pkg"] = "🔄 Chọn gói gia hạn / nâng cấp:",
                ["renew_hours"] = "giờ sử dụng", ["renew_select"] = "Chọn →", ["renew_selected"] = "Gói đã chọn",
                ["renew_scan_qr"] = "Quét mã QR để thanh toán:",
                ["renew_note"] = "Nội dung chuyển khoản đã được điền sẵn!",
                ["renew_waiting"] = "Đang chờ xác nhận thanh toán...", ["renew_back"] = "← Quay lại chọn gói",

                ["tour_detail_title"] = "Chi Tiết Lộ Trình",
                ["tour_navigate_btn"] = "🧭 Dẫn đường",
                ["tour_stalls_title"] = "Các Quán Ăn Trong Lộ Trình",

                ["sub_title"] = "Đăng ký gói cước",
                ["sub_desc"] = "Thiết bị của bạn chưa được kích hoạt hoặc đã hết hạn. Vui lòng chọn gói cước bên dưới để tiếp tục sử dụng.",
                ["sub_device_id"] = "Mã thiết bị:",
                ["sub_qr_title"] = "Quét mã QR dưới đây để thanh toán:",
                ["sub_qr_note"] = "Nội dung chuyển khoản đã được điền sẵn!",
                ["sub_total_amount"] = "Tổng tiền: {0} {1}",
                ["sub_wait"] = "Đang chờ nhận tiền tít tít...",
                ["sub_cancel"] = "Trở lại chọn gói",
                ["alert_payment_success_title"] = "Gia hạn thành công!",
                ["alert_payment_success_msg"] = "Thanh toán thành công. Gói cước đã được gia hạn.",
                ["btn_enter_app"] = "VÀO APP",
                ["alert_payment_error_title"] = "Lỗi",
                ["alert_payment_error_msg"] = "Không thể tạo mã thanh toán. Vui lòng thử lại sau.",
                ["pkg_name_1"] = "Gói Khám Phá Nhanh (2 Giờ)",
                ["pkg_name_2"] = "Gói Trải Nghiệm Tiêu Chuẩn (24 Giờ)",
                ["pkg_name_3"] = "Gói Bản Địa Không Giới Hạn (1 Tuần)",

                // ── Offline / Connectivity ──
                ["offline_title"] = "Không có internet",
                ["offline_msg"] = "Vui lòng kiểm tra kết nối mạng của bạn và thử lại.",
                ["offline_banner"] = "📶 Mất kết nối — Đang dùng dữ liệu offline",
                ["online_banner"] = "✅ Đã kết nối lại",

                // ── Empty States ──
                ["empty_tours"] = "Chưa có lộ trình nào\nHãy thử lại sau",
                ["empty_shops"] = "Chưa có quán nào\nHãy thử lại sau",
                ["empty_history"] = "Bạn chưa ghé thăm quán nào\nBắt đầu khám phá ngay!",
                ["empty_search"] = "Không tìm thấy kết quả",

                // ── PaymentPage (localized) ──
                ["payment_title"] = "Thanh Toán",
                ["payment_order_summary"] = "TÓM TẮT ĐƠN HÀNG",
                ["payment_method_label"] = "PHƯƠNG THỨC THANH TOÁN",
                ["payment_apple_pay"] = "Apple Pay",
                ["payment_apple_pay_desc"] = "Thanh toán một chạm",
                ["payment_card"] = "Thẻ Tín dụng / Ghi nợ",
                ["payment_card_desc"] = "Visa, Mastercard, JCB",
                ["payment_momo"] = "Ví MoMo",
                ["payment_momo_desc"] = "Nhanh chóng & Tiện lợi",
                ["payment_subtotal"] = "Tạm tính",
                ["payment_fee"] = "Phí dịch vụ",
                ["payment_total"] = "Tổng cộng",
                ["payment_cta"] = "🔒  Thanh Toán Ngay",
                ["payment_secure"] = "Thông tin thanh toán của bạn được mã hóa và bảo vệ theo tiêu chuẩn quốc tế.",
                ["payment_demo_msg"] = "Đây là màn hình demo, cổng thanh toán thật sẽ được nối sau.",
                ["payment_selected_method"] = "Bạn đã chọn phương thức: {0}",
            },

            // ─────────────────────────────────────────────────────────────────
            // ENGLISH
            // ─────────────────────────────────────────────────────────────────
            ["en"] = new()
            {
                ["main_hero_desc"] = "Discover Saigon's most vibrant culinary street",
                ["main_cat_title"] = "Featured Activities",
                ["main_cat_hot"] = "Trending",
                ["main_cat_seafood"] = "Seafood",
                ["main_cat_drinks"] = "Drinks",
                ["main_cat_snacks"] = "Street Snacks",
                ["main_promo_title"] = "Free Discovery 🎧",
                ["main_promo_desc"] = "GPS synced audio guide while you walk",
                ["main_promo_btn"] = "Try now",
                ["main_badge_visits"] = "🔥 999+ visits",
                ["main_badge_old"] = "🏅 Historic shop",

                ["tab_explore"] = "Explore", ["tab_map"] = "Map", ["tab_aura"] = "Aura", ["tab_profile"] = "Profile",
                ["main_header"] = "VINH KHANH STREET", ["main_hero_tag"] = "DISTRICT 4 - SAIGON",
                ["main_hero_title"] = "Discover\nThe Soul Of\nDistrict 4",
                ["main_hero_desc"] = "Saigon's most vibrant street food paradise, where stories are told through flavors.",
                ["main_btn_map"] = "Map", ["main_map_desc"] = "Explore near you",
                ["main_btn_scan"] = "Scan QR", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Top 5 Rated Shops", ["main_top_shops_desc"] = "Most loved by travelers",
                ["audio_welcome_stall"] = "Welcome to {0}!",
                ["main_top_tours_title"] = "🔥 Top 10 Guided Tours", ["main_explore_all"] = "Explore All",
                ["main_section_top"] = "Must-Try\nSpots", ["main_view_all"] = "View\nAll",
                ["main_shop1_tags"] = "🐌 Seafood · Snails", ["main_shop2_tags"] = "🥢 Snack · Street food",
                ["main_top5_shops_title"] = "⭐ Top 5 Best Stalls", ["main_top5_shops_fav"] = "👑 Favorite",
                ["search_placeholder"] = "Search food, restaurant...",

                ["main_voice_tag"] = "VOICE STUDIO", ["main_voice_title"] = "Customization",
                ["main_voice_btn"] = "TEST VOICE", ["main_voice_placeholder"] = "Enter texts here to test...",

                ["map_title"] = "Vĩnh Khánh Food Street", ["map_active"] = "Active",
                ["map_vacant"] = "Vacant", ["map_expired"] = "Expired",
                ["map_your_location"] = "Your Location", ["map_listen"] = "🔊 Listen",
                ["map_detail"] = "📋 Details", ["map_no_owner"] = "No owner",
                ["map_status_closed"] = "⛔ Closed", ["map_status_vacant"] = "🟢 Vacant", ["map_status_open"] = "🔴 Open",
                ["map_nearby_label"] = "📍 Nearby Stall",
                ["map_play_audio"] = "🔊 Audio",
                ["map_navigate"] = "🗺️ Navigate",
                ["map_free_explore_on"] = "🧭 Free Exploring...",
                ["map_free_explore_off"] = "Free Explore: Off",

                ["aura_header"] = "Voice Aura Settings", ["aura_subtitle"] = "CUSTOMIZE YOUR EXPERIENCE",
                ["aura_language"] = "Language", ["aura_gender"] = "Voice Gender",
                ["aura_male"] = "Male", ["aura_female"] = "Female",
                ["aura_speed"] = "Playback Speed", ["aura_speed_slow"] = "SLOW", ["aura_speed_normal"] = "NORMAL", ["aura_speed_fast"] = "FAST",
                ["aura_radius"] = "Alert Radius", ["aura_preview"] = "▶ Preview Audio",
                ["aura_save"] = "Save Changes", ["aura_saved_ok"] = "Settings saved successfully.",
                ["aura_error_title"] = "Error", ["aura_error_play"] = "Unable to play audio",
                ["alert_success"] = "Success",

                ["profile_header"] = "Vinh Khanh Guide", ["profile_title"] = "Profile",
                ["profile_display_name"] = "Tourist",
                ["profile_change_lang"] = "🌐 Change Language", ["profile_lang_desc"] = "Tap to change app language",
                ["profile_change_pkg"] = "🔄 Change Package", ["profile_change_pkg_desc"] = "Go to package selection",
                ["profile_logout"] = "🚪 Logout", ["profile_logout_desc"] = "Return to QR scan page to re-select a plan",
                ["profile_logout_confirm"] = "Are you sure you want to logout and return to the QR scan page? Your current session will be cleared.",
                ["profile_saved_count"] = "12 places",
                ["profile_history_item1"] = "Oc Dao Vinh Khanh", ["profile_history_tag1"] = "Seafood · Snails",
                ["profile_history_item2"] = "Mrs. Sau's Rolls", ["profile_history_tag2"] = "Traditional Appetizers",
                ["profile_device"] = "Device ID:", ["profile_expiry_ok"] = "Expires: {0}d {1}h remaining",
                ["profile_expiry_expired"] = "Status: EXPIRED",
                ["profile_visited"] = "Visited", ["profile_saved_lbl"] = "Saved", ["profile_rating"] = "Rating",
                ["profile_history"] = "Recent History", ["profile_view_all"] = "View All",
                ["profile_no_history"] = "No visit history yet",
                ["profile_support"] = "Support", ["profile_support_24"] = "24/7 Help",

                ["shop_address"] = "534 Vinh Khanh, Ward 10", ["shop_tag_signature"] = "GRILLED SEAFOOD", ["shop_tag_gem"] = "DISTRICT 4 HIDDEN GEM",
                ["shop_aura_tag"] = "Aura AI Guide", ["shop_menu_title"] = "Signature Menu",
                ["shop_menu_desc"] = "Curated flavors from Saigon's most vibrant street food artery.",
                ["shop_add_order"] = "Add to Order", ["shop_aura_active"] = "Voice Aura is Active",
                ["shop_aura_scan"] = "Scan any dish name to hear the story in your preferred language.",
                ["shop_order_alert"] = "Ordering feature is under development.",
                ["shop_dish1_desc"] = "Spotted Babylon snails roasted with chili salt. A perfect balance of sea sweetness and spicy crust.",
                ["shop_dish2_desc"] = "Water spinach flash-fried with aromatic garlic.",
                ["shop_dish3_desc"] = "Tiger prawns grilled with spicy house-made chili salt.",
                ["shop_tag_musttry"] = "MUST TRY", ["shop_tag_spicy"] = "SPICY",

                ["lang_welcome"] = "Welcome to HeriStep", ["lang_subtitle"] = "Please select your nationality to continue",
                ["lang_footer"] = "You can change this anytime in Profile settings",
                ["ok"] = "OK", ["close"] = "Close", ["notification"] = "Notification", ["coming_soon"] = "Coming Soon",

                ["renew_title"] = "Renew Package", ["renew_expired"] = "Package Expired",
                ["renew_info"] = "Please renew to continue using the app.",
                ["renew_device"] = "Device Code:", ["renew_choose_pkg"] = "🔄 Choose Renewal Package:",
                ["renew_hours"] = "hours usage", ["renew_select"] = "Select →", ["renew_selected"] = "Selected Package",
                ["renew_scan_qr"] = "Scan QR to pay:",
                ["renew_note"] = "Payment details are pre-filled!",
                ["renew_waiting"] = "Waiting for payment confirmation...", ["renew_back"] = "← Back to Packages",

                ["tour_detail_title"] = "Tour Details",
                ["tour_navigate_btn"] = "🧭 Navigate",
                ["tour_stalls_title"] = "Stalls in this Tour",

                ["sub_title"] = "Select Subscription",
                ["sub_desc"] = "Your device is not activated or has expired. Please select a package below to continue.",
                ["sub_device_id"] = "Device ID:",
                ["sub_qr_title"] = "Scan the QR code below to pay:",
                ["sub_qr_note"] = "Payment description is pre-filled!",
                ["sub_total_amount"] = "Total: {0} {1}",
                ["sub_wait"] = "Waiting for payment...",
                ["sub_cancel"] = "Back to packages",
                ["alert_payment_success_title"] = "Renewal Successful!",
                ["alert_payment_success_msg"] = "Payment successful. The subscription has been renewed.",
                ["btn_enter_app"] = "ENTER APP",
                ["alert_payment_error_title"] = "Error",
                ["alert_payment_error_msg"] = "Could not create payment QR. Please try again later.",
                ["pkg_name_1"] = "Quick Discovery Package (2 Hours)",
                ["pkg_name_2"] = "Standard Experience Package (24 Hours)",
                ["pkg_name_3"] = "Unlimited Local Package (1 Week)",

                // ── Offline / Connectivity ──
                ["offline_title"] = "No Internet",
                ["offline_msg"] = "Please check your network connection and try again.",
                ["offline_banner"] = "📶 No connection — Using offline data",
                ["online_banner"] = "✅ Connected again",

                // ── Empty States ──
                ["empty_tours"] = "No tours available\nPlease try again later",
                ["empty_shops"] = "No shops available\nPlease try again later",
                ["empty_history"] = "You haven't visited any stalls yet\nStart exploring now!",
                ["empty_search"] = "No results found",

                // ── PaymentPage ──
                ["payment_title"] = "Checkout",
                ["payment_order_summary"] = "ORDER SUMMARY",
                ["payment_method_label"] = "PAYMENT METHOD",
                ["payment_apple_pay"] = "Apple Pay",
                ["payment_apple_pay_desc"] = "One-tap payment",
                ["payment_card"] = "Credit / Debit Card",
                ["payment_card_desc"] = "Visa, Mastercard, JCB",
                ["payment_momo"] = "MoMo Wallet",
                ["payment_momo_desc"] = "Fast & Convenient",
                ["payment_subtotal"] = "Subtotal",
                ["payment_fee"] = "Service fee",
                ["payment_total"] = "Total",
                ["payment_cta"] = "🔒  Pay Now",
                ["payment_secure"] = "Your payment information is encrypted and protected to international standards.",
                ["payment_demo_msg"] = "This is a demo screen. Real payment gateway will be connected later.",
                ["payment_selected_method"] = "You selected: {0}",
            },

            // ─────────────────────────────────────────────────────────────────
            // JAPANESE
            // ─────────────────────────────────────────────────────────────────
            ["ja"] = new()
            {
                ["main_hero_desc"] = "サイゴンで最も活気ある屋台街を探索",
                ["main_cat_title"] = "特集アクティビティ",
                ["main_cat_hot"] = "トレンド",
                ["main_cat_seafood"] = "シーフード",
                ["main_cat_drinks"] = "飲み物",
                ["main_cat_snacks"] = "スナック",
                ["main_promo_title"] = "無料ディスカバリー 🎧",
                ["main_promo_desc"] = "GPS同期オーディオガイド",
                ["main_promo_btn"] = "試してみる",
                ["main_badge_visits"] = "🔥 999+ 訪問",
                ["main_badge_old"] = "🏅 老舗",

                ["tab_explore"] = "探す", ["tab_map"] = "マップ", ["tab_aura"] = "オーラ", ["tab_profile"] = "プロフィール",
                ["main_header"] = "ヴィンカーン通り", ["main_hero_tag"] = "4区 - サイゴン",
                ["main_hero_title"] = "4区の\n魂を\n発見しよう",
                ["main_btn_map"] = "地図", ["main_map_desc"] = "近くを探索",
                ["main_top_shops"] = "⭐ 5つ星店舗", ["main_top_shops_desc"] = "旅行者に最も愛されています",
                ["audio_welcome_stall"] = "{0}へようこそ！",
                ["main_top_tours_title"] = "🔥 トップ10ツアー", ["main_explore_all"] = "すべて探索",
                ["main_section_top"] = "必食\nスポット", ["main_view_all"] = "すべて\n見る",
                ["main_shop1_tags"] = "🐌 シーフード・巻き貝", ["main_shop2_tags"] = "🥢 屋台のスナック",
                ["main_top5_shops_title"] = "⭐ トップ5屋台", ["main_top5_shops_fav"] = "👑 お気に入り",
                ["search_placeholder"] = "飲食店、場所を検索...",

                ["map_your_location"] = "現在地", ["map_listen"] = "🔊 聴く", ["map_detail"] = "📋 詳細",
                ["map_active"] = "営業中", ["map_vacant"] = "空き", ["map_expired"] = "期限切れ",
                ["map_nearby_label"] = "📍 近くの屋台",
                ["map_play_audio"] = "🔊 オーディオ",
                ["map_navigate"] = "🗺️ 案内する",
                ["map_free_explore_on"] = "🧭 自由探索中...",
                ["map_free_explore_off"] = "自由探索: オフ",

                ["aura_saved_ok"] = "設定が保存されました。", ["ok"] = "OK", ["close"] = "閉じる",
                ["alert_success"] = "成功",

                ["profile_title"] = "プロフィール",
                ["profile_display_name"] = "観光客",
                ["profile_change_lang"] = "🌐 言語変更", ["profile_lang_desc"] = "タップしてアプリの言語を変更",
                ["profile_change_pkg"] = "🔄 パッケージ変更", ["profile_change_pkg_desc"] = "選択画面へ",
                ["profile_logout"] = "🚪 ログアウト", ["profile_logout_desc"] = "QRスキャンページに戻る",
                ["profile_logout_confirm"] = "ログアウトしてQRスキャンページに戻りますか？",
                ["notification"] = "お知らせ",
                ["profile_device"] = "デバイス:", ["profile_expiry_ok"] = "残り: {0}日 {1}時間",
                ["profile_expiry_expired"] = "状態: 期限切れ",
                ["profile_visited"] = "訪問済み", ["profile_saved_lbl"] = "保存", ["profile_rating"] = "評価",
                ["profile_history"] = "最近の履歴", ["profile_view_all"] = "すべて見る",
                ["profile_no_history"] = "訪問履歴はありません",
                ["profile_support"] = "サポート", ["profile_support_24"] = "24/7 ヘルプ",
                ["profile_saved_count"] = "12か所",
                ["profile_history_item1"] = "お小料理屋ウィンカーン", ["profile_history_tag1"] = "シーフード・各種貝",
                ["profile_history_item2"] = "サウさんののり黎巻き", ["profile_history_tag2"] = "伝統的前辺",

                ["shop_address"] = "10区、ヴィンカーン534番地", ["shop_tag_signature"] = "グリルシーフード",
                ["shop_aura_tag"] = "Aura AI ガイド", ["shop_menu_title"] = "特別メニュー",
                ["shop_add_order"] = "注文する", ["shop_aura_active"] = "Voice Aura 有効",
                ["shop_dish1_desc"] = "チリソルトで焼いたバイ貝。海の甘みとスパイシーな皮の完璧なバランス。",
                ["shop_dish2_desc"] = "香ばしいニンニクと一緒にさっと炒めた空芯菜。",
                ["shop_dish3_desc"] = "自家製のスパイシーなチリソルトで焼いたブラックタイガー。",
                ["shop_tag_musttry"] = "必食", ["shop_tag_spicy"] = "スパイシー",

                ["lang_welcome"] = "HeriStepへようこそ", ["lang_subtitle"] = "続けるには国籍を選択してください",
                ["lang_footer"] = "プロフィール設定でいつでも変更できます",
                ["ok"] = "OK", ["close"] = "閉じる",

                ["renew_title"] = "パッケージ更新", ["renew_expired"] = "期限切れ",
                ["renew_info"] = "継続するには更新してください",
                ["renew_device"] = "デバイス:", ["renew_choose_pkg"] = "🔄 パッケージを選択:",
                ["renew_select"] = "選択 →", ["renew_scan_qr"] = "QRをスキャン:",
                ["renew_note"] = "支払いの詳細は自動入力されます", ["renew_waiting"] = "支払い確認待ち...", ["renew_back"] = "← 戻る",

                ["tour_detail_title"] = "ツアー詳細",
                ["tour_navigate_btn"] = "🧭 案内する",
                ["tour_stalls_title"] = "ルート上の屋台",

                ["sub_title"] = "サブスクリプションの選択",
                ["sub_desc"] = "デバイスがアクティブ化されていないか、期限が切れています。続行するには、以下のパッケージを選択してください。",
                ["sub_device_id"] = "デバイスID:",
                ["sub_qr_title"] = "以下のQRコードをスキャンして支払います:",
                ["sub_qr_note"] = "支払いの詳細は自動入力されます！",
                ["sub_total_amount"] = "合計: {0} {1}",
                ["sub_wait"] = "支払いを待っています...",
                ["sub_cancel"] = "パッケージに戻る",
                ["alert_payment_success_title"] = "更新に成功しました！",
                ["alert_payment_success_msg"] = "支払いが成功しました。サブスクリプションが更新されました。",
                ["btn_enter_app"] = "アプリに入る",
                ["alert_payment_error_title"] = "エラー",
                ["alert_payment_error_msg"] = "支払いQRを作成できませんでした。後でもう一度お試しください。",
                ["pkg_name_1"] = "クイックディスカバリーパッケージ (2時間)",
                ["pkg_name_2"] = "スタンダード体験パッケージ (24時間)",
                ["pkg_name_3"] = "無制限ローカルパッケージ (1週間)",
            },

            // ─────────────────────────────────────────────────────────────────
            // KOREAN
            // ─────────────────────────────────────────────────────────────────
            ["ko"] = new()
            {
                ["main_hero_desc"] = "사이공에서 가장 활기찬 음식 거리를 발견하세요",
                ["main_cat_title"] = "주요 활동",
                ["main_cat_hot"] = "인기",
                ["main_cat_seafood"] = "해산물",
                ["main_cat_drinks"] = "음료",
                ["main_cat_snacks"] = "간식",
                ["main_promo_title"] = "자유 탐색 🎧",
                ["main_promo_desc"] = "GPS 동기화 오디오 가이드",
                ["main_promo_btn"] = "지금 해보기",
                ["main_badge_visits"] = "🔥 999+ 방문",
                ["main_badge_old"] = "🏅 오래된 상점",

                ["tab_explore"] = "탐색", ["tab_map"] = "지도", ["tab_aura"] = "아우라", ["tab_profile"] = "프로필",
                ["main_header"] = "빈칸 거리", ["main_hero_tag"] = "4군 - 사이공",
                ["main_hero_title"] = "4군의\n영혼을\n발견하세요",
                ["main_hero_desc"] = "사이공에서 가장 활기찬\n길거리 음식의 천국,\n맛으로 이야기를 전합니다.",
                ["main_btn_map"] = "지도", ["main_map_desc"] = "내 주변 탐색",
                ["main_top_shops"] = "⭐ 5성급 상점", ["main_top_shops_desc"] = "여행자들에게 가장 사랑받는 곳",
                ["audio_welcome_stall"] = "{0}에 오신 것을 환영합니다!",
                ["main_top_tours_title"] = "🔥 주요 10대 투어", ["main_explore_all"] = "모두 보기",
                ["main_section_top"] = "꼭 먹어봐야 할\n맛집", ["main_view_all"] = "전체\n보기",
                ["main_shop1_tags"] = "🐌 해산물 · 달팽이", ["main_shop2_tags"] = "🥢 길거리 간식",
                ["main_top5_shops_title"] = "⭐ 상위 5개 식당", ["main_top5_shops_fav"] = "👑 즐겨찾는 고급 식당",
                ["search_placeholder"] = "식당, 업체를 검색하세요...",

                ["map_your_location"] = "내 위치", ["map_listen"] = "🔊 듣기", ["map_detail"] = "📋 상세 정보",
                ["map_active"] = "영업 중", ["map_vacant"] = "빈 자리", ["map_expired"] = "만료됨",
                ["map_no_owner"] = "주인 없음",
                ["map_status_closed"] = "⛔ 닫힘", ["map_status_vacant"] = "🟢 빈자리", ["map_status_open"] = "🔴 영업 중",
                ["map_nearby_label"] = "📍 근처 식당",
                ["map_play_audio"] = "🔊 오디오",
                ["map_navigate"] = "🗺️ 길 안내",
                ["map_free_explore_on"] = "🧭 자유 탐험 중...",
                ["map_free_explore_off"] = "자유 탐험: 끄기",

                ["aura_saved_ok"] = "설정이 저장되었습니다.", ["ok"] = "확인", ["close"] = "닫기",
                ["alert_success"] = "성공",

                ["profile_title"] = "프로필",
                ["profile_display_name"] = "관광객",
                ["profile_change_lang"] = "🌐 언어 변경", ["profile_lang_desc"] = "탭 하여 앱 언어 변경",
                ["profile_change_pkg"] = "🔄 패키지 변경", ["profile_change_pkg_desc"] = "패키지 선택으로 이동",
                ["profile_logout"] = "🚪 로그아웃", ["profile_logout_desc"] = "QR 스캔 페이지로 돌아가기",
                ["profile_logout_confirm"] = "로그아웃하고 QR 스캔 페이지로 돌아갈까요?",
                ["notification"] = "알림",
                ["profile_device"] = "기기 ID:", ["profile_expiry_ok"] = "만료: {0}일 {1}시간 남음",
                ["profile_expiry_expired"] = "상태: 만료됨",
                ["profile_visited"] = "방문함", ["profile_saved_lbl"] = "저장함", ["profile_rating"] = "평점",
                ["profile_history"] = "최근 방문 내역", ["profile_view_all"] = "전체 보기",
                ["profile_no_history"] = "방문 기록이 없습니다",
                ["profile_support"] = "고객지원", ["profile_support_24"] = "24/7 도움",
                ["profile_saved_count"] = "12곳",
                ["profile_history_item1"] = "옥 당오 빈 카늕", ["profile_history_tag1"] = "해산물·달팬이",
                ["profile_history_item2"] = "사우 씀의 넷 란", ["profile_history_tag2"] = "전통 전청",

                ["shop_address"] = "10구 빈칸 534번지", ["shop_tag_signature"] = "그릴 해산물", ["shop_tag_gem"] = "4군의 숨겨진 보석",
                ["shop_aura_tag"] = "Aura AI 가이드", ["shop_menu_title"] = "시그니처 메뉴",
                ["shop_menu_desc"] = "사이공에서 가장 활기찬 길거리 음식의 엄선된 맛.",
                ["shop_add_order"] = "주문하기", ["shop_aura_active"] = "Voice Aura 활성",
                ["shop_aura_scan"] = "요리 이름을 스캔하여 원하는 언어로 스토리를 들어보세요.",
                ["shop_order_alert"] = "주문 기능은 개발 중입니다.",
                ["shop_dish1_desc"] = "칠리 소금으로 구운 바빌론 달팽이. 해산물의 달콤함과 매콤함의 완벽한 조화.",
                ["shop_dish2_desc"] = "마늘 향이 향긋한 모닝글로리 볶음.",
                ["shop_dish3_desc"] = "특제 칠리 소금으로 구운 타이거 새우.",
                ["shop_tag_musttry"] = "추천", ["shop_tag_spicy"] = "매운",

                ["lang_welcome"] = "HeriStep에 오신 것을 환영합니다", ["lang_subtitle"] = "계속하려면 국적을 선택하세요",
                ["lang_footer"] = "프로필 설정에서 언제든지 변경할 수 있습니다",
                ["ok"] = "확인", ["close"] = "닫기", ["notification"] = "알림", ["coming_soon"] = "곧 출시",

                ["renew_title"] = "패키지 갱신", ["renew_expired"] = "만료됨",
                ["renew_info"] = "계속 사용하려면 갱신하세요.",
                ["renew_device"] = "기기 코드:", ["renew_choose_pkg"] = "🔄 갱신 패키지 선택:",
                ["renew_hours"] = "시간 사용", ["renew_select"] = "선택 →", ["renew_selected"] = "선택됨",
                ["renew_scan_qr"] = "결제 QR 스캔:",
                ["renew_note"] = "결제 세부 정보가 미리 입력되어 있습니다!",
                ["renew_waiting"] = "결제 대기 중...", ["renew_back"] = "← 뒤로가기",

                ["tour_detail_title"] = "투어 세부 정보",
                ["tour_navigate_btn"] = "🧭 길찾기",
                ["tour_stalls_title"] = "경로에 있는 식당",

                ["sub_title"] = "패키지 구독",
                ["sub_desc"] = "기기가 활성화되지 않았거나 만료되었습니다. 계속하려면 아래 패키지를 선택하세요.",
                ["sub_device_id"] = "기기 ID:",
                ["sub_qr_title"] = "결제하려면 아래 QR 코드를 스캔하세요:",
                ["sub_qr_note"] = "결제 상세 정보가 미리 입력되어 있습니다!",
                ["sub_total_amount"] = "총액: {0} {1}",
                ["sub_wait"] = "결제 대기 중...",
                ["sub_cancel"] = "패키지로 돌아가기",
                ["alert_payment_success_title"] = "갱신 성공!",
                ["alert_payment_success_msg"] = "결제가 성공적으로 처리되었습니다. 패키지가 갱신되었습니다.",
                ["btn_enter_app"] = "앱 진입",
                ["alert_payment_error_title"] = "오류",
                ["alert_payment_error_msg"] = "결제 QR을 생성할 수 없습니다. 나중에 다시 시도하세요.",
                ["pkg_name_1"] = "빠른 탐색 패키지 (2시간)",
                ["pkg_name_2"] = "표준 경험 패키지 (24시간)",
                ["pkg_name_3"] = "무제한 로컬 패키지 (1주일)",
            },

            // ─────────────────────────────────────────────────────────────────
            // CHINESE (Simplified)
            // ─────────────────────────────────────────────────────────────────
            ["zh"] = new()
            {
                ["main_hero_desc"] = "探索西贡最具活力的美食街",
                ["main_cat_title"] = "热门活动",
                ["main_cat_hot"] = "热门",
                ["main_cat_seafood"] = "海鲜",
                ["main_cat_drinks"] = "饮料",
                ["main_cat_snacks"] = "小吃",
                ["main_promo_title"] = "自由漫步 🎧",
                ["main_promo_desc"] = "GPS随行语音导览",
                ["main_promo_btn"] = "立即体验",
                ["main_badge_visits"] = "🔥 999+ 来访",
                ["main_badge_old"] = "🏅 老字号",

                ["tab_explore"] = "探索", ["tab_map"] = "地图", ["tab_aura"] = "Aura", ["tab_profile"] = "个人档案",
                ["main_header"] = "永庆美食街", ["main_hero_tag"] = "4区 - 西贡",
                ["main_hero_title"] = "探索\n4区的\n灵魂",
                ["main_btn_map"] = "地图", ["main_map_desc"] = "探索附近",
                ["main_top_shops"] = "⭐ 五星级店铺", ["main_top_shops_desc"] = "最受游客喜爱",
                ["audio_welcome_stall"] = "欢迎来到 {0}!",
                ["main_top_tours_title"] = "🔥 10大导览之旅", ["main_explore_all"] = "探索全部",
                ["main_section_top"] = "必尝\n美食", ["main_view_all"] = "查看\n全部",
                ["main_shop1_tags"] = "🐌 海鲜·各种螺", ["main_shop2_tags"] = "🥢 街头小吃",
                ["main_top5_shops_title"] = "⭐ 前5名摊位", ["main_top5_shops_fav"] = "👑 收藏夹",
                ["search_placeholder"] = "搜索餐厅、地点...",

                ["map_your_location"] = "我的位置", ["map_listen"] = "🔊 收听", ["map_detail"] = "📋 详情",
                ["map_active"] = "营业中", ["map_vacant"] = "空位", ["map_expired"] = "已过期",
                ["map_nearby_label"] = "📍 附近摊位",
                ["map_play_audio"] = "🔊 音频",
                ["map_navigate"] = "🗺️ 导航",
                ["map_free_explore_on"] = "🧭 自由探索中...",
                ["map_free_explore_off"] = "自由探索: 关闭",

                ["aura_saved_ok"] = "设置已保存。", ["ok"] = "确定", ["close"] = "关闭",

                ["profile_title"] = "个人档案",
                ["profile_display_name"] = "游客",
                ["profile_change_lang"] = "🌐 更改语言", ["profile_lang_desc"] = "点击更改应用语言",
                ["profile_change_pkg"] = "🔄 变更套餐", ["profile_change_pkg_desc"] = "转到包选择",
                ["profile_logout"] = "🚪 登出", ["profile_logout_desc"] = "返回扫描QR页面重新选择套餐",
                ["profile_logout_confirm"] = "确定要登出并返回扫描QR页面吗？",
                ["notification"] = "通知",
                ["profile_device"] = "设备:", ["profile_expiry_ok"] = "剩余: {0}天 {1}小时",
                ["profile_expiry_expired"] = "状态: 已过期",
                ["profile_visited"] = "已访问", ["profile_saved_lbl"] = "已保存", ["profile_rating"] = "评分",
                ["profile_history"] = "最近记录", ["profile_view_all"] = "查看全部",
                ["profile_no_history"] = "暂无访问历史",
                ["profile_support"] = "支持", ["profile_support_24"] = "24/7 帮助",
                ["profile_saved_count"] = "12个地点",
                ["profile_history_item1"] = "蕃岛永庆海鲜食庞", ["profile_history_tag1"] = "海鲜·各种虁",
                ["profile_history_item2"] = "萧小姐的春卷", ["profile_history_tag2"] = "传统开胃菜",

                ["shop_address"] = "第10区534号永庆", ["shop_tag_signature"] = "烤海鲜",
                ["shop_aura_tag"] = "Aura AI 指南", ["shop_menu_title"] = "招牌菜单",
                ["shop_add_order"] = "加入订单", ["shop_aura_active"] = "Voice Aura 生效",
                ["shop_dish1_desc"] = "椒盐烤花螺。海鲜的鲜甜与辣味的完美平衡。",
                ["shop_dish2_desc"] = "蒜香空心菜。",
                ["shop_dish3_desc"] = "秘制椒盐烤黑虎虾。",
                ["shop_tag_musttry"] = "必试", ["shop_tag_spicy"] = "辣",

                ["lang_welcome"] = "欢迎来到 HeriStep", ["lang_subtitle"] = "请选择您的国籍以继续",
                ["lang_footer"] = "您可以随时在个人设置中更改此选项",
                ["ok"] = "确定", ["close"] = "关闭",

                ["renew_title"] = "续订套餐", ["renew_expired"] = "已过期",
                ["renew_info"] = "请续订以继续",
                ["renew_device"] = "设备:", ["renew_choose_pkg"] = "🔄 选择续订包:",
                ["renew_select"] = "选择 →", ["renew_scan_qr"] = "扫描付款:",
                ["renew_note"] = "内容自动填充", ["renew_waiting"] = "等待确认付款...", ["renew_back"] = "← 返回",

                ["tour_detail_title"] = "巡演详情",
                ["tour_navigate_btn"] = "🧭 导航",
                ["tour_stalls_title"] = "路线上的摊位",

                ["sub_title"] = "选择订阅",
                ["sub_desc"] = "您的设备尚未激活或已过期。请选择以下套餐继续使用。",
                ["sub_device_id"] = "设备ID:",
                ["sub_qr_title"] = "扫描下方二维码进行支付:",
                ["sub_qr_note"] = "付款说明已预填！",
                ["sub_total_amount"] = "总计: {0} {1}",
                ["sub_wait"] = "正在等待付款...",
                ["sub_cancel"] = "返回套餐选择",
                ["alert_payment_success_title"] = "续订成功！",
                ["alert_payment_success_msg"] = "付款成功。订阅已续订。",
                ["btn_enter_app"] = "进入应用",
                ["alert_payment_error_title"] = "错误",
                ["alert_payment_error_msg"] = "无法创建付款二维码。请稍后再试。",
                ["pkg_name_1"] = "快速探索套餐 (2小时)",
                ["pkg_name_2"] = "标准体验套餐 (24小时)",
                ["pkg_name_3"] = "无限本地套餐 (1周)",
            },

            // ─────────────────────────────────────────────────────────────────
            // FRENCH
            // ─────────────────────────────────────────────────────────────────
            ["fr"] = new()
            {
                ["main_hero_desc"] = "Découvrez la rue culinaire la plus animée de Saigon",
                ["main_cat_title"] = "Activités Phares",
                ["main_cat_hot"] = "Tendance",
                ["main_cat_seafood"] = "Fruits de mer",
                ["main_cat_drinks"] = "Boissons",
                ["main_cat_snacks"] = "En-cas",
                ["main_promo_title"] = "Découverte Libre 🎧",
                ["main_promo_desc"] = "Guide audio synchronisé par GPS",
                ["main_promo_btn"] = "Essayer",
                ["main_badge_visits"] = "🔥 999+ visites",
                ["main_badge_old"] = "🏅 Boutique historique",

                ["tab_explore"] = "Explorer", ["tab_map"] = "Carte", ["tab_aura"] = "Aura", ["tab_profile"] = "Profil",
                ["main_header"] = "RUE VINH KHANH", ["main_hero_tag"] = "DISTRICT 4 - SAÏGON",
                ["main_hero_title"] = "Découvrez\nL'Âme du\nDistrict 4",
                ["main_btn_map"] = "Carte", ["main_map_desc"] = "Explorer près de vous",
                ["main_top_shops"] = "Meilleurs Stands", ["main_top_shops_desc"] = "Les plus favoris",
                ["main_top_tours_title"] = "🔥 Top 10 Tours", ["main_explore_all"] = "Tout voir",
                ["main_section_top"] = "À Ne Pas\nManquer", ["main_view_all"] = "Voir\nTout",
                ["main_top5_shops_title"] = "⭐ Top 5 Stands", ["main_top5_shops_fav"] = "👑 Favori",
                ["search_placeholder"] = "Rechercher un restaurant...",

                ["map_your_location"] = "Ma position", ["map_listen"] = "🔊 Écouter", ["map_detail"] = "📋 Détails",
                ["map_active"] = "Ouvert", ["map_vacant"] = "Libre", ["map_expired"] = "Expiré",
                ["map_nearby_label"] = "📍 Stand proche",
                ["map_play_audio"] = "🔊 Audio",
                ["map_navigate"] = "🗺️ Naviguer",
                ["map_free_explore_on"] = "🧭 Exploration libre...",
                ["map_free_explore_off"] = "Exploration: Arrêt",

                ["aura_saved_ok"] = "Paramètres enregistrés.", ["ok"] = "OK", ["close"] = "Fermer",

                ["profile_title"] = "Profil",
                ["profile_display_name"] = "Touriste",
                ["profile_change_lang"] = "🌐 Changer la langue", ["profile_lang_desc"] = "Appuyer pour changer la langue",
                ["profile_change_pkg"] = "🔄 Changer de forfait", ["profile_change_pkg_desc"] = "Aller à la sélection",
                ["profile_logout"] = "🚪 Se déconnecter", ["profile_logout_desc"] = "Retour à la page QR pour rechoisir",
                ["profile_logout_confirm"] = "Voulez-vous vous déconnecter et retourner à la page QR?",
                ["notification"] = "Notification",
                ["profile_device"] = "Appareil:", ["profile_expiry_ok"] = "Expire: {0}j {1}h restants",
                ["profile_expiry_expired"] = "Statut: EXPIRÉ",
                ["profile_visited"] = "Visité", ["profile_saved_lbl"] = "Enregistré", ["profile_rating"] = "Note",
                ["profile_history"] = "Historique récent", ["profile_view_all"] = "Voir tout",
                ["profile_no_history"] = "Aucun historique de visite",
                ["profile_support"] = "Assistance", ["profile_support_24"] = "24/7 Aide",
                ["profile_saved_count"] = "12 lieux",
                ["profile_history_item1"] = "Oc Dao Vinh Khanh", ["profile_history_tag1"] = "Fruits de mer·Escargots",
                ["profile_history_item2"] = "Les Rouleaux de Mme Sau", ["profile_history_tag2"] = "Entrées traditionnelles",

                ["shop_tag_signature"] = "FRUITS DE MER GRILLÉS", ["shop_tag_gem"] = "JOYAU CACHÉ DU QUARTIER 4",
                ["shop_aura_tag"] = "Guide IA Aura", ["shop_menu_title"] = "Menu Signature",
                ["shop_add_order"] = "Ajouter à la commande", ["shop_aura_active"] = "Voice Aura est actif",
                ["shop_dish1_desc"] = "Escargots de Babylone rôtis au sel de chili. Un équilibre parfait entre douceur et croûte épicée.",
                ["shop_dish2_desc"] = "Épinards d'eau sautés à l'ail aromatique.",
                ["shop_dish3_desc"] = "Crevettes tigrées grillées avec du sel de chili fait maison.",
                ["shop_tag_musttry"] = "À ESSAYER", ["shop_tag_spicy"] = "ÉPICÉ",

                ["lang_welcome"] = "Bienvenue sur HeriStep", ["lang_subtitle"] = "Veuillez sélectionner votre nationalité pour continuer",
                ["lang_footer"] = "Vous pouvez modifier cela à tout moment dans les paramètres de profil",
                ["ok"] = "OK", ["close"] = "Fermer",

                ["renew_title"] = "Renouveler l'abonnement", ["renew_expired"] = "Abonnement expiré",
                ["renew_info"] = "Veuillez renouveler pour continuer.",
                ["renew_device"] = "Appareil:", ["renew_choose_pkg"] = "🔄 Choisir un forfait:",
                ["renew_select"] = "Sélectionner →", ["renew_scan_qr"] = "Scanner le QR:",
                ["renew_note"] = "Détails de paiement pré-remplis!", ["renew_waiting"] = "En attente de confirmation...", ["renew_back"] = "← Retour",

                ["tour_detail_title"] = "Détails du Tour",
                ["tour_navigate_btn"] = "🧭 Naviguer",
                ["tour_stalls_title"] = "Stands sur le parcours",

                ["sub_title"] = "Sélectionnez un forfait",
                ["sub_desc"] = "Votre appareil n'est pas activé ou a expiré. Veuillez sélectionner un forfait ci-dessous pour continuer.",
                ["sub_device_id"] = "ID de l'appareil:",
                ["sub_qr_title"] = "Scannez le code QR ci-dessous pour payer:",
                ["sub_qr_note"] = "La description du paiement est pré-remplie!",
                ["sub_total_amount"] = "Total: {0} {1}",
                ["sub_wait"] = "En attente du paiement...",
                ["sub_cancel"] = "Retour aux forfaits",
                ["alert_payment_success_title"] = "Renouvellement réussi!",
                ["alert_payment_success_msg"] = "Paiement réussi. L'abonnement a été renouvelé.",
                ["btn_enter_app"] = "ENTRER DANS L'APP",
                ["alert_payment_error_title"] = "Erreur",
                ["alert_payment_error_msg"] = "Impossible de créer le QR de paiement. Veuillez réessayer plus tard.",
                ["pkg_name_1"] = "Forfait Découverte Rapide (2 Heures)",
                ["pkg_name_2"] = "Forfait Expérience Standard (24 Heures)",
                ["pkg_name_3"] = "Forfait Local Illimité (1 Semaine)",
            },

            // ─────────────────────────────────────────────────────────────────
            // THAI
            // ─────────────────────────────────────────────────────────────────
            ["th"] = new()
            {
                ["main_hero_desc"] = "ค้นพบสวรรค์แห่งสตรีทฟู้ดที่มีชีวิตชีวาที่สุดในไซ่ง่อน",
                ["main_cat_title"] = "กิจกรรมเด่น",
                ["main_cat_hot"] = "ยอดนิยม",
                ["main_cat_seafood"] = "อาหารทะเล",
                ["main_cat_drinks"] = "เครื่องดื่ม",
                ["main_cat_snacks"] = "ของว่าง",
                ["main_promo_title"] = "ค้นพบอิสระ 🎧",
                ["main_promo_desc"] = "ออดิโอไกด์ซิงค์กับ GPS",
                ["main_promo_btn"] = "ลองเลย",
                ["main_badge_visits"] = "🔥 999+ การเข้าชม",
                ["main_badge_old"] = "🏅 ร้านเก่าแก่",

                ["tab_explore"] = "สำรวจ", ["tab_map"] = "แผนที่", ["tab_aura"] = "ออร่า", ["tab_profile"] = "โปรไฟล์",
                ["main_header"] = "ถนนวินห์คานห์", ["main_hero_tag"] = "เขต 4 - ไซง่อน",
                ["main_hero_title"] = "ค้นพบ\nจิตวิญญาณแห่ง\nเขต 4",
                ["main_hero_desc"] = "สวรรค์แห่งสตรีทฟู้ดที่มีชีวิตชีวาที่สุดในไซง่อน",
                ["main_btn_map"] = "แผนที่", ["main_map_desc"] = "สำรวจใกล้คุณ",
                ["main_btn_scan"] = "สแกน QR", ["main_btn_aura"] = "วอยซ์ ออร่า",
                ["main_top_shops"] = "⭐ ร้านค้ายอดนิยม 5 ดาว", ["main_top_shops_desc"] = "นักเดินทางชื่นชอบมาก nhất",
                ["audio_welcome_stall"] = "ยินดีต้อนรับสู่ {0}!",
                ["main_top_tours_title"] = "🔥 10 อันดับทัวร์แนะนำ", ["main_explore_all"] = "สำรวจทั้งหมด",
                ["main_section_top"] = "จุดที่ต้อง\nลอง", ["main_view_all"] = "ดู\nทั้งหมด",
                ["main_shop1_tags"] = "🐌 อาหารทะเล · หอยต่างๆ", ["main_shop2_tags"] = "🥢 ของว่าง · สตรีทฟู้ด",
                ["main_top5_shops_title"] = "⭐ 5 อันดับร้านเด็ด", ["main_top5_shops_fav"] = "👑 ร้านโปรด",
                ["search_placeholder"] = "ค้นหาร้านอาหาร, สถานที่...",

                ["map_your_location"] = "ตำแหน่งของคุณ", ["map_listen"] = "🔊 ฟังแนะนำ", ["map_detail"] = "📋 รายละเอียด",
                ["map_active"] = "เปิดบริการ", ["map_vacant"] = "ว่าง", ["map_expired"] = "หมดอายุ",
                ["map_status_closed"] = "⛔ ปิดแล้ว", ["map_status_vacant"] = "🟢 ว่าง", ["map_status_open"] = "🔴 เปิด",
                ["map_nearby_label"] = "📍 ร้านใกล้คุณ",
                ["map_play_audio"] = "🔊 เสียง",
                ["map_navigate"] = "🗺️ นำทาง",
                ["map_free_explore_on"] = "🧭 กำลังสำรวจอิสระ...",
                ["map_free_explore_off"] = "สำรวจอิสระ: ปิด",

                ["aura_header"] = "ตั้งค่า วอยซ์ ออร่า", ["aura_saved_ok"] = "บันทึกการตั้งค่าแล้ว",
                ["aura_preview"] = "▶ ฟังตัวอย่าง", ["aura_save"] = "บันทึกการเปลี่ยนแปลง",
                ["ok"] = "ตกลง", ["close"] = "ปิด", ["alert_success"] = "สำเร็จ",

                ["profile_title"] = "โปรไฟล์",
                ["profile_display_name"] = "นักท่องเที่ยว",
                ["profile_change_lang"] = "🌐 เปลี่ยนภาษา", ["profile_lang_desc"] = "แตะเพื่อเปลี่ยนภาษาของแอป",
                ["profile_change_pkg"] = "🔄 เปลี่ยนแพ็กเกจ", ["profile_change_pkg_desc"] = "ไปยังหน้าเลือกแพ็กเกจ",
                ["profile_logout"] = "🚪 ออกจากระบบ", ["profile_logout_desc"] = "กลับไปหน้าสแกน QR เพื่อเลือกแผนใหม่",
                ["profile_logout_confirm"] = "คุณแน่ใจหรือไม่ว่าต้องการออกจากระบบ và กลับไปหน้าสแกน QR?",
                ["notification"] = "การแจ้งเตือน",
                ["profile_device"] = "รหัสเครื่อง:", ["profile_expiry_ok"] = "เหลือเวลา: {0} วัน {1} ชั่วโมง",
                ["profile_expiry_expired"] = "สถานะ: หมดอายุ",
                ["profile_visited"] = "ไปมาแล้ว", ["profile_saved_lbl"] = "บันทึก", ["profile_คะแนน"] = "คะแนน",
                ["profile_history"] = "ประวัติล่าสุด", ["profile_view_all"] = "ดูทั้งหมด",
                ["profile_no_history"] = "ยังไม่มีประวัติการเข้าชม",
                ["profile_support"] = "สนับสนุน", ["profile_support_24"] = "ช่วยเหลือ 24/7",
                ["profile_saved_count"] = "12 สถานที่",
                ["profile_history_item1"] = "หอยโอเอซิสวินห์คานห์", ["profile_history_tag1"] = "อาหารทะเล · หอยต่างๆ",
                ["profile_history_item2"] = "ก๋วยเตี๋ยวหลอดคุณส่าว", ["profile_history_tag2"] = "อาหารว่างแบบดั้งเดิม",

                ["shop_address"] = "534 วินห์คานห์, แขวง 10", ["shop_tag_signature"] = "อาหารทะเลปิ้งย่าง",
                ["shop_aura_tag"] = "Aura AI คู่มือ", ["shop_menu_title"] = "เมนูแนะนำ",
                ["shop_add_order"] = "เพิ่มในรายการ", ["shop_aura_active"] = "วอยซ์ ออร่า กำลังทำงาน",
                ["shop_dish1_desc"] = "หอยหวานคั่วพริกเกลือ ความสมดุลที่สมบูรณ์แบบระหว่างความหวานจากทะเลและรสเผ็ดร้อน",
                ["shop_dish2_desc"] = "ผัดผักบุ้งไฟแดงใส่กระเทียมหอม",
                ["shop_dish3_desc"] = "กุ้งกuลาดำย่างพริกเกลือสูตรพิเศษ",
                ["shop_tag_musttry"] = "ต้องลอง", ["shop_tag_spicy"] = "เผ็ด",

                ["lang_welcome"] = "ยินดีต้อนรับสู่ HeriStep", ["lang_subtitle"] = "โปรดเลือกสัญชาติของคุณเพื่อดำเนินการต่อ",
                ["lang_footer"] = "คุณสามารถเปลี่ยนตัวเลือกนี้ได้ตลอดเวลาในตั้งค่าโปรไฟล์",
                ["ok"] = "ตกลง", ["close"] = "ปิด",

                ["renew_title"] = "ต่ออายุแพ็กเกจ", ["renew_expired"] = "แพ็กเกจหมดอายุ",
                ["renew_info"] = "โปรดต่ออายุเพื่อใช้งานต่อ",
                ["renew_device"] = "รหัสอุปกรณ์:", ["renew_choose_pkg"] = "🔄 เลือกแพ็กเกจต่ออายุ:",
                ["renew_select"] = "เลือก →", ["renew_scan_qr"] = "สแกน QR เพื่อชำระเงิน:",
                ["renew_note"] = "รายละเอียดการชำระเงินถูกกรอกไว้ล่วงหน้าแล้ว", ["renew_waiting"] = "กำลังรอการยืนยันการชำระเงิน...", ["renew_back"] = "← กลับไปหน้าแพ็กเกจ",

                ["tour_detail_title"] = "รายละเอียดทัวร์",
                ["tour_navigate_btn"] = "🧭 นำทาง",
                ["tour_stalls_title"] = "ร้านอาหารในเส้นทางนี้",

                ["sub_title"] = "ต่ออายุแพ็กเกจ",
                ["sub_desc"] = "อุปกรณ์ของคุณยังไม่ได้รับการเปิดใช้งานหรือหมดอายุแล้ว โปรดเลือกแพ็กเกจด้านล่างเพื่อ tiếp tục",
                ["sub_device_id"] = "รหัสอุปกรณ์:",
                ["sub_qr_title"] = "สแกนรหัส QR ด้านล่างเพื่อชำระเงิน:",
                ["sub_qr_note"] = "กรอกรายละเอียดการชำระเงินไว้ล่วงหน้าแล้ว!",
                ["sub_total_amount"] = "ยอดรวม: {0} {1}",
                ["sub_wait"] = "กำลังรอการชำระเงิน...",
                ["sub_cancel"] = "กลับไปที่แพ็กเกจ",
                ["alert_payment_success_title"] = "ต่ออายุสำเร็จแล้ว!",
                ["alert_payment_success_msg"] = "ชำระเงินสำเร็จแล้ว ต่ออายุการสมัครรับข้อมูลเรียบร้อยแล้ว",
                ["btn_enter_app"] = "เข้าสู่แอป",
                ["alert_payment_error_title"] = "ข้อผิดพลาด",
                ["alert_payment_error_msg"] = "ไม่สามารถสร้าง QR การชำระเงินได้ โปรดลองอีกครั้งในภายหลัง",
                ["pkg_name_1"] = "แพ็กเกจสำรวจด่วน (2 ชั่วโมง)",
                ["pkg_name_2"] = "แพ็กเกจประสบการณ์มาตรฐาน (24 ชั่วโมง)",
                ["pkg_name_3"] = "แพ็กเกจท้องถิ่นไม่จำกัด (1 สัปดาห์)",
            },

            // ─────────────────────────────────────────────────────────────────
            // SPANISH
            // ─────────────────────────────────────────────────────────────────
            ["es"] = new()
            {
                ["main_hero_desc"] = "Descubre la calle gastronómica más vibrante de Saigón",
                ["main_cat_title"] = "Actividades Destacadas",
                ["main_cat_hot"] = "Tendencia",
                ["main_cat_seafood"] = "Mariscos",
                ["main_cat_drinks"] = "Bebidas",
                ["main_cat_snacks"] = "Snacks",
                ["main_promo_title"] = "Descubrimiento Libre 🎧",
                ["main_promo_desc"] = "Guía de audio sincronizada con GPS",
                ["main_promo_btn"] = "Probar ahora",
                ["main_badge_visits"] = "🔥 999+ visitas",
                ["main_badge_old"] = "🏅 Tienda histórica",

                ["tab_explore"] = "Explorar", ["tab_map"] = "Mapa", ["tab_aura"] = "Aura", ["tab_profile"] = "Perfil",
                ["main_header"] = "CALLE VINH KHANH", ["main_hero_tag"] = "DISTRITO 4 - SAIGÓN",
                ["main_hero_title"] = "Descubre\nel Alma del\nDistrito 4",
                ["main_hero_desc"] = "El paraíso de comida callejera más vibrante de Saigón.",
                ["main_btn_map"] = "Mapa", ["main_map_desc"] = "Explorar cerca de ti",
                ["main_btn_scan"] = "Escanear QR", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Puestos de 5 Estrellas", ["main_top_shops_desc"] = "Más favoritos",
                ["audio_welcome_stall"] = "¡Bienvenido a {0}!",
                ["main_top_tours_title"] = "🔥 Top 10 Tours Guiados", ["main_explore_all"] = "Explorar Todo",
                ["main_section_top"] = "Sitios que\nDebes Probar", ["main_view_all"] = "Ver\nTodo",
                ["main_shop1_tags"] = "🐌 Mariscos · Caracoles", ["main_shop2_tags"] = "🥢 Snacks · Callejera",
                ["main_top5_shops_title"] = "⭐ Top 5 Mejores Puestos", ["main_top5_shops_fav"] = "👑 Favorito",
                ["search_placeholder"] = "Buscar comida, restaurante...",

                ["map_your_location"] = "Tu Ubicación", ["map_listen"] = "🔊 Escuchar", ["map_detail"] = "📋 Detalles",
                ["map_active"] = "Activo", ["map_vacant"] = "Vacante", ["map_expired"] = "Expirado",
                ["map_status_closed"] = "⛔ Cerrado", ["map_status_vacant"] = "🟢 Vacante", ["map_status_open"] = "🔴 Abierto",
                ["map_nearby_label"] = "📍 Puesto Cercano",
                ["map_play_audio"] = "🔊 Audio",
                ["map_navigate"] = "🗺️ Navegar",
                ["map_free_explore_on"] = "🧭 Exploración Libre...",
                ["map_free_explore_off"] = "Explorar Libre: Apagado",

                ["aura_header"] = "Ajustes de Voice Aura", ["aura_saved_ok"] = "Ajustes guardados correctamente.",
                ["aura_preview"] = "▶ Previsualizar Audio", ["aura_save"] = "Guardar Cambios",
                ["ok"] = "Aceptar", ["close"] = "Cerrar", ["alert_success"] = "Éxito",

                ["profile_title"] = "Perfil",
                ["profile_display_name"] = "Turista",
                ["profile_change_lang"] = "🌐 Cambiar Idioma", ["profile_lang_desc"] = "Toca para cambiar el idioma",
                ["profile_change_pkg"] = "🔄 Cambiar Paquete", ["profile_change_pkg_desc"] = "Ir a selección",
                ["profile_logout"] = "🚪 Cerrar Sesión", ["profile_logout_desc"] = "Volver a la página QR para elegir plan",
                ["profile_logout_confirm"] = "¿Seguro que quieres cerrar sesión y volver a la página de QR?",
                ["notification"] = "Notificación",
                ["profile_device"] = "ID Dispositivo:", ["profile_expiry_ok"] = "Vence en: {0}d {1}h",
                ["profile_expiry_expired"] = "Estado: EXPIRADO",
                ["profile_visited"] = "Visitado", ["profile_saved_lbl"] = "Guardado", ["profile_rating"] = "Calificación",
                ["profile_history"] = "Historial Reciente", ["profile_view_all"] = "Ver Todo",
                ["profile_no_history"] = "Aún no hay historial de visitas",
                ["profile_support"] = "Soporte", ["profile_support_24"] = "Ayuda 24/7",
                ["profile_saved_count"] = "12 lugares",
                ["profile_history_item1"] = "Oasis de Caracoles Vinh Khanh", ["profile_history_tag1"] = "Mariscos · Caracoles",
                ["profile_history_item2"] = "Rollos de la Sra. Sau", ["profile_history_tag2"] = "Aperitivos Tradicionales",

                ["shop_address"] = "534 Vinh Khanh, Barrio 10", ["shop_tag_signature"] = "MARISCOS A LA PARRILLA",
                ["shop_aura_tag"] = "Guía Aura AI", ["shop_menu_title"] = "Menú de la Casa",
                ["shop_add_order"] = "Añadir al Pedido", ["shop_aura_active"] = "Voice Aura Activo",
                ["shop_dish1_desc"] = "Caracoles de Babilonia con sal de chile. Balance perfecto entre el dulce del mar y el picante.",
                ["shop_dish2_desc"] = "Espinacas de agua salteadas con ajo aromático.",
                ["shop_dish3_desc"] = "Langostinos tigre a la parrilla con sal de chile casera.",
                ["shop_tag_musttry"] = "PROBAR", ["shop_tag_spicy"] = "PICANTE",

                ["lang_welcome"] = "Bienvenido a HeriStep", ["lang_subtitle"] = "Por favor elige tu nacionalidad",
                ["lang_footer"] = "Puedes cambiar esto en Perfil en cualquier momento",
                ["ok"] = "Aceptar", ["close"] = "Cerrar",

                ["renew_title"] = "Renovar Paquete", ["renew_expired"] = "Paquete Expirado",
                ["renew_info"] = "Por favor renueva para continuar.",
                ["renew_device"] = "Código de Dispositivo:", ["renew_choose_pkg"] = "🔄 Elegir Paquete de Renovación:",
                ["renew_select"] = "Seleccionar →", ["renew_scan_qr"] = "Escanear QR para pagar:",
                ["renew_note"] = "¡Detalles de pago completados!", ["renew_waiting"] = "Esperando confirmación...", ["renew_back"] = "← Volver",

                ["tour_detail_title"] = "Detalles del Tour",
                ["tour_navigate_btn"] = "🧭 Navegar",
                ["tour_stalls_title"] = "Puestos en este Tour",

                ["sub_title"] = "Seleccionar suscripción",
                ["sub_desc"] = "Su dispositivo no está activado o ha caducado. Seleccione un paquete a continuación para continuar.",
                ["sub_device_id"] = "ID del dispositivo:",
                ["sub_qr_title"] = "Escanee el código QR a continuación para pagar:",
                ["sub_qr_note"] = "¡La descripción del pago ya está completa!",
                ["sub_total_amount"] = "Total: {0} {1}",
                ["sub_wait"] = "Esperando el pago...",
                ["sub_cancel"] = "Volver a los paquetes",
                ["alert_payment_success_title"] = "¡Renovación exitosa!",
                ["alert_payment_success_msg"] = "Pago exitoso. La suscripción ha sido renovada.",
                ["btn_enter_app"] = "ENTRAR A LA APP",
                ["alert_payment_error_title"] = "Error",
                ["alert_payment_error_msg"] = "No se pudo crear el QR de pago. Inténtelo de nuevo más tarde.",
                ["pkg_name_1"] = "Paquete de descubrimiento rápido (2 horas)",
                ["pkg_name_2"] = "Paquete de experiencia estándar (24 horas)",
                ["pkg_name_3"] = "Paquete local ilimitado (1 semana)",
            },

            // ─────────────────────────────────────────────────────────────────
            // GERMAN
            // ─────────────────────────────────────────────────────────────────
            ["de"] = new()
            {
                ["main_hero_desc"] = "Entdecke Saigons lebhafteste kulinarische Straße",
                ["main_cat_title"] = "Ausgewählte Aktivitäten",
                ["main_cat_hot"] = "Im Trend",
                ["main_cat_seafood"] = "Meeresfrüchte",
                ["main_cat_drinks"] = "Getränke",
                ["main_cat_snacks"] = "Snacks",
                ["main_promo_title"] = "Freie Entdeckung 🎧",
                ["main_promo_desc"] = "GPS-synchronisierter Audioguide",
                ["main_promo_btn"] = "Jetzt testen",
                ["main_badge_visits"] = "🔥 999+ Besuche",
                ["main_badge_old"] = "🏅 Historischer Laden",

                ["tab_explore"] = "Entdecken", ["tab_map"] = "Karte", ["tab_aura"] = "Aura", ["tab_profile"] = "Profil",
                ["main_header"] = "VINH KHANH STRASSE", ["main_hero_tag"] = "DISTRIKT 4 - SAIGON",
                ["main_hero_title"] = "Entdecke\ndie Seele von\nDistrikt 4",
                ["main_hero_desc"] = "Saigons lebhaftestes Streetfood-Paradies.",
                ["main_btn_map"] = "Karte", ["main_map_desc"] = "In deiner Nähe erkunden",
                ["main_btn_scan"] = "QR scannen", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Top 5 Geschäfte", ["main_top_shops_desc"] = "Bei Reisenden am beliebtesten",
                ["audio_welcome_stall"] = "Willkommen bei {0}!",
                ["main_top_tours_title"] = "🔥 Top 10 Führungen", ["main_explore_all"] = "Alles Erkunden",
                ["main_section_top"] = "Highlights", ["main_view_all"] = "Alle\nansehen",
                ["main_shop1_tags"] = "🐌 Meeresfrüchte · Schnecken", ["main_shop2_tags"] = "🥢 Snacks · Streetfood",
                ["main_top5_shops_title"] = "⭐ Top 5 Stände", ["main_top5_shops_fav"] = "👑 Favorit",
                ["search_placeholder"] = "Essen, Restaurant suchen...",

                ["map_your_location"] = "Dein Standort", ["map_listen"] = "🔊 Anhören", ["map_detail"] = "📋 Details",
                ["map_active"] = "Aktiv", ["map_vacant"] = "Frei", ["map_expired"] = "Abgelaufen",
                ["map_status_closed"] = "⛔ Geschlossen", ["map_status_vacant"] = "🟢 Frei", ["map_status_open"] = "🔴 Offen",
                ["map_nearby_label"] = "📍 Stand in der Nähe",
                ["map_play_audio"] = "🔊 Audio",
                ["map_navigate"] = "🗺️ Navigieren",
                ["map_free_explore_on"] = "🧭 Freie Erkundung...",
                ["map_free_explore_off"] = "Freie Erkundung: Aus",

                ["aura_header"] = "Voice Aura Einstellungen", ["aura_saved_ok"] = "Einstellungen gespeichert.",
                ["aura_preview"] = "▶ Vorschau hören", ["aura_save"] = "Speichern",
                ["ok"] = "OK", ["close"] = "Schließen", ["alert_success"] = "Erfolg",

                ["profile_title"] = "Profil",
                ["profile_display_name"] = "Tourist",
                ["profile_change_lang"] = "🌐 Sprache ändern", ["profile_lang_desc"] = "Tippen zum Ändern",
                ["profile_change_pkg"] = "🔄 Paket ändern", ["profile_change_pkg_desc"] = "Zur Auswahl gehen",
                ["profile_logout"] = "🚪 Abmelden", ["profile_logout_desc"] = "Zum QR-Scannen zurückkehren",
                ["profile_logout_confirm"] = "Möchten Sie sich wirklich abmelden?",
                ["notification"] = "Benachrichtigung",
                ["profile_device"] = "Geräte-ID:", ["profile_expiry_ok"] = "Gültig bis: còn {0} Tage {1} Std.",
                ["profile_expiry_expired"] = "Status: ABGELAUFEN",
                ["profile_visited"] = "Besucht", ["profile_saved_lbl"] = "Gespeichert", ["profile_rating"] = "Bewertung",
                ["profile_history"] = "Verlauf", ["profile_view_all"] = "Alle ansehen",
                ["profile_no_history"] = "Noch kein Besuchsverlauf",
                ["profile_support"] = "Support", ["profile_support_24"] = "24/7 Hilfe",
                ["profile_saved_count"] = "12 Orte",
                ["profile_history_item1"] = "Schnecken-Oase Vinh Khanh", ["profile_history_tag1"] = "Meeresfrüchte · Schnecken",
                ["profile_history_item2"] = "Frau Saus Rollen", ["profile_history_tag2"] = "Traditionelle Vorspeisen",

                ["shop_address"] = "534 Vinh Khanh, Viertel 10", ["shop_tag_signature"] = "GEGRILLTE MEERESFRÜCHTE",
                ["shop_aura_tag"] = "Aura KI Guide", ["shop_menu_title"] = "Spezialitäten",
                ["shop_add_order"] = "Zum Warenkorb", ["shop_aura_active"] = "Voice Aura Aktiv",
                ["shop_dish1_desc"] = "Schnecken mit Chili-Salz geröstet. Perfekte Balance aus Süße und Schärfe.",
                ["shop_dish2_desc"] = "Wasserspinat mit aromatischem Knoblauch.",
                ["shop_dish3_desc"] = "Riesengarnelen mit hausgemachtem Chili-Salz.",
                ["shop_tag_musttry"] = "PROBIEREN", ["shop_tag_spicy"] = "SCHARF",

                ["lang_welcome"] = "Willkommen bei HeriStep", ["lang_subtitle"] = "Bitte wählen Sie Ihre Nationalität",
                ["lang_footer"] = "Änderungen jederzeit im Profil möglich",
                ["ok"] = "OK", ["close"] = "Schließen",

                ["renew_title"] = "Paket erneuern", ["renew_expired"] = "Paket abgelaufen",
                ["renew_info"] = "Bitte erneuern, um fortzufahren.",
                ["renew_device"] = "Geräte-Code:", ["renew_choose_pkg"] = "🔄 Verlängerung wählen:",
                ["renew_select"] = "Wählen →", ["renew_scan_qr"] = "QR scannen zum Bezahlen:",
                ["renew_note"] = "Zahlungsdetails sind vorausgefüllt!", ["renew_waiting"] = "Warte auf Bestätigung...", ["renew_back"] = "← Zurück",

                ["tour_detail_title"] = "Tour-Details",
                ["tour_navigate_btn"] = "🧭 Navigieren",
                ["tour_stalls_title"] = "Stände in dieser Tour",

                ["sub_title"] = "Abonnement auswählen",
                ["sub_desc"] = "Ihr Gerät ist nicht aktiviert oder abgelaufen. Bitte wählen Sie unten ein Paket aus, um fortzufahren.",
                ["sub_device_id"] = "Geräte-ID:",
                ["sub_qr_title"] = "Scannen Sie den QR-Code unten zum Bezahlen:",
                ["sub_qr_note"] = "Zahlungsbeschreibung ist bereits ausgefüllt!",
                ["sub_total_amount"] = "Gesamt: {0} {1}",
                ["sub_wait"] = "Warten auf Zahlung...",
                ["sub_cancel"] = "Zurück zu den Paketen",
                ["alert_payment_success_title"] = "Erneuerung erfolgreich!",
                ["alert_payment_success_msg"] = "Zahlung erfolgreich. Das Abonnement wurde verlängert.",
                ["btn_enter_app"] = "APP ÖFFNEN",
                ["alert_payment_error_title"] = "Fehler",
                ["alert_payment_error_msg"] = "Zahlungs-QR konnte nicht erstellt werden. Bitte versuchen Sie es später noch einmal.",
                ["pkg_name_1"] = "Schnellentdecker-Paket (2 Stunden)",
                ["pkg_name_2"] = "Standard-Erlebnispaket (24 Stunden)",
                ["pkg_name_3"] = "Unbegrenztes lokales Paket (1 Woche)",
            },

            // ─────────────────────────────────────────────────────────────────
            // RUSSIAN
            // ─────────────────────────────────────────────────────────────────
            ["ru"] = new()
            {
                ["main_hero_desc"] = "Откройте для себя самую оживленную кулинарную улицу Сайгона",
                ["main_cat_title"] = "Популярные развлечения",
                ["main_cat_hot"] = "В тренде",
                ["main_cat_seafood"] = "Морепродукты",
                ["main_cat_drinks"] = "Напитки",
                ["main_cat_snacks"] = "Закуски",
                ["main_promo_title"] = "Свободное открытие 🎧",
                ["main_promo_desc"] = "Аудиогид с синхронизацией по GPS",
                ["main_promo_btn"] = "Попробовать",
                ["main_badge_visits"] = "🔥 999+ визитов",
                ["main_badge_old"] = "🏅 Исторический магазин",

                ["tab_explore"] = "Обзор", ["tab_map"] = "Карта", ["tab_aura"] = "Аура", ["tab_profile"] = "Профиль",
                ["main_header"] = "УЛИЦA ВИНЬ ХАНЬ", ["main_hero_tag"] = "РАЙОН 4 - САЙГОН",
                ["main_hero_title"] = "Открой\nДушу\nРайона 4",
                ["main_hero_desc"] = "Самый яркий рай уличной еды в Сайгоне.",
                ["main_btn_map"] = "Карта", ["main_map_desc"] = "Изучить рядом",
                ["main_btn_scan"] = "Сканировать QR", ["main_btn_aura"] = "Вайс Аура",
                ["main_top_shops"] = "⭐ Топ-5 магазинов", ["main_top_shops_desc"] = "Больше всего любят путешественники",
                ["audio_welcome_stall"] = "Добро пожаловать в {0}!",
                ["main_top_tours_title"] = "🔥 Топ 10 Туров", ["main_explore_all"] = "Изучить всё",
                ["main_section_top"] = "Места,\nкоторые стоит\nпосетить", ["main_view_all"] = "Смотреть\nвсе",
                ["main_shop1_tags"] = "🐌 Морепродукты · Улитки", ["main_shop2_tags"] = "🥢 Закуски · Уличная еда",
                ["main_top5_shops_title"] = "⭐ Топ 5 лучших лавок", ["main_top5_shops_fav"] = "👑 Любимое",
                ["search_placeholder"] = "Поиск еды, ресторана...",

                ["map_your_location"] = "Ваше местоположение", ["map_listen"] = "🔊 Слушать", ["map_detail"] = "📋 Подробнее",
                ["map_active"] = "Активен", ["map_vacant"] = "Свободно", ["map_expired"] = "Истек",
                ["map_status_closed"] = "⛔ Закрыто", ["map_status_vacant"] = "🟢 Свободно", ["map_status_open"] = "🔴 Открыто",
                ["map_nearby_label"] = "📍 Лавка рядом",
                ["map_play_audio"] = "🔊 Аудио",
                ["map_navigate"] = "🗺️ Навигация",
                ["map_free_explore_on"] = "🧭 Свободное изучение...",
                ["map_free_explore_off"] = "Свободное изучение: Выкл",

                ["aura_header"] = "Настройки Вайс Ауры", ["aura_saved_ok"] = "Настройки сохранены.",
                ["aura_preview"] = "▶ Предпрослушивание", ["aura_save"] = "Сохранить",
                ["ok"] = "ОК", ["close"] = "Закрыть", ["alert_success"] = "Успех",

                ["profile_title"] = "Профиль",
                ["profile_display_name"] = "Турист",
                ["profile_change_lang"] = "🌐 Сменить язык", ["profile_lang_desc"] = "Нажмите для смены языка",
                ["profile_change_pkg"] = "🔄 Сменить тариф", ["profile_change_pkg_desc"] = "Перейти к выбору",
                ["profile_logout"] = "🚪 Выйти", ["profile_logout_desc"] = "Вернуться к QR для смены тарифа",
                ["profile_logout_confirm"] = "Вы уверены, что хотите выйти?",
                ["notification"] = "Уведомление",
                ["profile_device"] = "ID устройства:", ["profile_expiry_ok"] = "Осталось: {0}д {1}ч",
                ["profile_expiry_expired"] = "Статус: ИСТЕК",
                ["profile_visited"] = "Посещено", ["profile_saved_lbl"] = "Сохранено", ["profile_rating"] = "Рейтинг",
                ["profile_history"] = "История", ["profile_view_all"] = "Посмотреть все",
                ["profile_no_history"] = "История посещений пуста",
                ["profile_support"] = "Поддержка", ["profile_support_24"] = "Помощь 24/7",
                ["profile_saved_count"] = "12 мест",
                ["profile_history_item1"] = "Оазис улиток Винь Хань", ["profile_history_tag1"] = "Морепродукты · Улитки",
                ["profile_history_item2"] = "Блинчики госпожи Сау", ["profile_history_tag2"] = "Традиционные закуски",

                ["shop_address"] = "534 Винь Хань, квартал 10", ["shop_tag_signature"] = "МОРЕПРОDUКТЫ НА ГРИЛЕ",
                ["shop_aura_tag"] = "AI Гид Аура", ["shop_menu_title"] = "Фирменное меню",
                ["shop_add_order"] = "Добавить в заказ", ["shop_aura_active"] = "Вайс Аура активна",
                ["shop_dish1_desc"] = "Улитки, запеченные с солью и чили. Идеальный баланс морской сладости и остроты.",
                ["shop_dish2_desc"] = "Водяной шпинат, обжаренный с ароматным чесноком.",
                ["shop_dish3_desc"] = "Тигровые креветки на гриле с острым домашним солью и чили.",
                ["shop_tag_musttry"] = "ПОПРОБОВАТЬ", ["shop_tag_spicy"] = "ОСТРОЕ",

                ["lang_welcome"] = "Добро пожаловать в HeriStep", ["lang_subtitle"] = "Выберите гражданство, чтобы продолжить",
                ["lang_footer"] = "Вы можете изменить это в профиле в любое время",
                ["ok"] = "ОК", ["close"] = "Закрыть",

                ["renew_title"] = "Продлить тариф", ["renew_expired"] = "Тариф истек",
                ["renew_info"] = "Пожалуйста, продлите, чтобы продолжить.",
                ["renew_device"] = "Код устройства:", ["renew_choose_pkg"] = "🔄 Выберите тариф для продления:",
                ["renew_select"] = "Выбрать →", ["renew_scan_qr"] = "Сканировать QR для оплаты:",
                ["renew_note"] = "Платежные реквизиты заполнены!", ["renew_waiting"] = "Ожидание подтверждения...", ["renew_back"] = "← Назад",

                ["tour_detail_title"] = "Детали тура",
                ["tour_navigate_btn"] = "🧭 Навигация",
                ["tour_stalls_title"] = "Лавки в этом туре",

                ["sub_title"] = "Выбор подписки",
                ["sub_desc"] = "Ваше устройство не активировано или срок действия истек. Пожалуйста, выберите пакет ниже, чтобы продолжить.",
                ["sub_device_id"] = "ID устройства:",
                ["sub_qr_title"] = "Сканируйте QR-код ниже для оплаты:",
                ["sub_qr_note"] = "Описание платежа уже заполнено!",
                ["sub_total_amount"] = "Всего: {0} {1}",
                ["sub_wait"] = "Ожидание оплаты...",
                ["sub_cancel"] = "Назад к пакетам",
                ["alert_payment_success_title"] = "Продление успешно!",
                ["alert_payment_success_msg"] = "Оплата прошла успешно. Подписка была продлена.",
                ["btn_enter_app"] = "ВОЙТИ В ПРИЛОЖЕНИЕ",
                ["alert_payment_error_title"] = "Ошибка",
                ["alert_payment_error_msg"] = "Не удалось создать QR-код оплаты. Пожалуйста, попробуйте позже.",
                ["pkg_name_1"] = "Пакет «Быстрое открытие» (2 часа)",
                ["pkg_name_2"] = "Пакет «Стандартный опыт» (24 часа)",
                ["pkg_name_3"] = "Безлимитный локальный пакет (1 неделя)",
            },
        };
    }
}

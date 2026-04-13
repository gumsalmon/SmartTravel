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

        public static void Init()
        {
            _currentLang = Preferences.Default.Get("user_language", "vi");
        }

        public static void SetLanguage(string langCode)
        {
            _currentLang = langCode;
            Preferences.Default.Set("user_language", langCode);
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

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            // ─────────────────────────────────────────────────────────────────
            // VIETNAMESE
            // ─────────────────────────────────────────────────────────────────
            ["vi"] = new()
            {
                ["tab_explore"] = "Khám Phá", ["tab_map"] = "Bản Đồ", ["tab_aura"] = "Aura", ["tab_profile"] = "Hồ Sơ",
                ["main_header"] = "PHỐ VĨNH KHÁNH", ["main_hero_tag"] = "QUẬN 4 - SAIGON",
                ["main_hero_title"] = "Khám Phá\nTâm Hồn\nQuận 4",
                ["main_hero_desc"] = "Thiên đường ẩm thực đường\nphố sầm uất nhất Sài Gòn.",
                ["main_btn_map"] = "Bản Đồ", ["main_map_desc"] = "Khám phá gần bạn",
                ["main_btn_scan"] = "Quét M/QR", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Top Quán 5 Sao", ["main_top_shops_desc"] = "Được yêu thích nhất",
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
            },

            // ─────────────────────────────────────────────────────────────────
            // ENGLISH
            // ─────────────────────────────────────────────────────────────────
            ["en"] = new()
            {
                ["tab_explore"] = "Explore", ["tab_map"] = "Map", ["tab_aura"] = "Aura", ["tab_profile"] = "Profile",
                ["main_header"] = "VINH KHANH STREET", ["main_hero_tag"] = "DISTRICT 4 - SAIGON",
                ["main_hero_title"] = "Discover\nThe Soul Of\nDistrict 4",
                ["main_hero_desc"] = "Saigon's most vibrant street food paradise, where stories are told through flavors.",
                ["main_btn_map"] = "Map", ["main_map_desc"] = "Explore near you",
                ["main_btn_scan"] = "Scan QR", ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "⭐ Top 5-Star Stalls", ["main_top_shops_desc"] = "Most favorite",
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
                ["profile_logout"] = "🚪 Logout", ["profile_logout_desc"] = "Return to QR scan page to re-select a plan",
                ["profile_logout_confirm"] = "Are you sure you want to logout and return to the QR scan page? Your current session will be cleared.",
                ["profile_device"] = "Device ID:", ["profile_expiry_ok"] = "Expires: {0}d {1}h remaining",
                ["profile_expiry_expired"] = "Status: EXPIRED",
                ["profile_visited"] = "Visited", ["profile_saved_lbl"] = "Saved", ["profile_rating"] = "Rating",
                ["profile_history"] = "Recent History", ["profile_view_all"] = "View All",
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
            },

            // ─────────────────────────────────────────────────────────────────
            // JAPANESE
            // ─────────────────────────────────────────────────────────────────
            ["ja"] = new()
            {
                ["tab_explore"] = "探索", ["tab_map"] = "地図", ["tab_aura"] = "オーラ", ["tab_profile"] = "プロフィール",
                ["main_header"] = "ヴィンカーン通り", ["main_hero_tag"] = "4区 - サイゴン",
                ["main_hero_title"] = "4区の\n魂を\n発見しよう",
                ["main_btn_map"] = "地図", ["main_map_desc"] = "近くを探索",
                ["main_top_shops"] = "トップ5つ星屋台", ["main_top_shops_desc"] = "最も人気",
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
            },

            // ─────────────────────────────────────────────────────────────────
            // KOREAN
            // ─────────────────────────────────────────────────────────────────
            ["ko"] = new()
            {
                ["tab_explore"] = "탐색", ["tab_map"] = "지도", ["tab_aura"] = "아우라", ["tab_profile"] = "프로필",
                ["main_header"] = "빈칸 거리", ["main_hero_tag"] = "4군 - 사이공",
                ["main_hero_title"] = "4군의\n영혼을\n발견하세요",
                ["main_hero_desc"] = "사이공에서 가장 활기찬\n길거리 음식의 천국,\n맛으로 이야기를 전합니다.",
                ["main_btn_map"] = "지도", ["main_map_desc"] = "내 주변 탐색",
                ["main_top_shops"] = "인기 5성급 식당", ["main_top_shops_desc"] = "가장 좋아하는",
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
            },

            // ─────────────────────────────────────────────────────────────────
            // CHINESE (Simplified)
            // ─────────────────────────────────────────────────────────────────
            ["zh"] = new()
            {
                ["tab_explore"] = "探索", ["tab_map"] = "地图", ["tab_aura"] = "Aura", ["tab_profile"] = "个人档案",
                ["main_header"] = "永庆美食街", ["main_hero_tag"] = "4区 - 西贡",
                ["main_hero_title"] = "探索\n4区的\n灵魂",
                ["main_btn_map"] = "地图", ["main_map_desc"] = "探索附近",
                ["main_top_shops"] = "热门5星摊位", ["main_top_shops_desc"] = "最受欢迎",
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
            },

            // ─────────────────────────────────────────────────────────────────
            // FRENCH
            // ─────────────────────────────────────────────────────────────────
            ["fr"] = new()
            {
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
            },
        };
    }
}

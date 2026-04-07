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

        /// <summary>Fires when the app language is changed. Subscribe to refresh UI text.</summary>
        public static event Action? LanguageChanged;

        /// <summary>Gets the current language code (e.g. "vi", "en", "ja").</summary>
        public static string CurrentLanguage => _currentLang;

        /// <summary>
        /// Initializes the language from saved preferences. Call once at app startup.
        /// </summary>
        public static void Init()
        {
            _currentLang = Preferences.Default.Get("user_language", "vi");
        }

        /// <summary>
        /// Changes the app language, saves to preferences, and fires LanguageChanged.
        /// </summary>
        public static void SetLanguage(string langCode)
        {
            _currentLang = langCode;
            Preferences.Default.Set("user_language", langCode);
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// Returns the translated string for the given key in the current language.
        /// Falls back to English, then to the key itself if no translation found.
        /// </summary>
        public static string Get(string key)
        {
            if (Strings.TryGetValue(_currentLang, out var langDict) && langDict.TryGetValue(key, out var val))
                return val;

            // Fallback to English
            if (Strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
                return enVal;

            return key; // Last resort: return the key itself
        }

        // ═══════════════════════════════════════════
        // TRANSLATION DICTIONARIES
        // ═══════════════════════════════════════════

        private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            // ───────────────────────────────────────
            // VIETNAMESE
            // ───────────────────────────────────────
            ["vi"] = new()
            {
                // Tab titles
                ["tab_explore"] = "Khám Phá",
                ["tab_map"] = "Bản Đồ",
                ["tab_aura"] = "Aura",
                ["tab_profile"] = "Hồ Sơ",

                // Main Page
                ["main_header"] = "PHỐ VĨNH KHÁNH",
                ["main_hero_tag"] = "QUẬN 4 - SAIGON",
                ["main_hero_title"] = "Khám Phá\nTâm Hồn\nQuận 4",
                ["main_hero_desc"] = "Thiên đường ẩm thực đường\nphố sầm uất nhất Sài Gòn, nơi\nnhững câu chuyện được kể qua\nhương vị.",
                ["main_btn_map"] = "Bản Đồ",
                ["main_map_desc"] = "Khám phá gần bạn",
                ["main_btn_scan"] = "Quét M/QR",
                ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "Top Quán",
                ["main_top_shops_desc"] = "Được yêu thích nhất",
                ["main_section_top"] = "Quán Đỉnh Phải\nThử",
                ["main_section_top_desc"] = "Top rating từ cộng đồng thực\nthần",
                ["main_view_all"] = "Xem tất\ncả",
                ["main_shop1_tags"] = "🐌 Hải sản · Ốc các loại",
                ["main_shop2_tags"] = "🥢 Ăn vặt · Đường phố",
                
                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "Tùy chỉnh giọng nói",
                ["main_voice_desc"] = "Chọn giọng nói, nhập văn bản và nghe thử tức thì.",
                ["main_voice_profile"] = "HỒ SƠ GIỌNG NÓI",
                ["main_voice_text"] = "VĂN BẢN CỦA BẠN",
                ["main_voice_speed"] = "TỐC ĐỘ",
                ["main_voice_btn"] = "NGHE THỬ",
                ["main_voice_placeholder"] = "Nhập văn bản để nghe thử...",
                ["main_voice_status_default"] = "Chọn giọng nói, nhập văn bản, rồi nhấn Nghe Thử",

                // Map Page
                ["map_title"] = "Phố Ẩm Thực Vĩnh Khánh",
                ["map_active"] = "Đang hoạt động",
                ["map_vacant"] = "Chưa có chủ",
                ["map_expired"] = "Hết hạn",
                ["map_your_location"] = "Vị trí của bạn",
                ["map_listen"] = "🔊 Nghe Giới Thiệu",
                ["map_detail"] = "📋 Chi Tiết",
                ["map_status_closed"] = "⛔ Đã đóng",
                ["map_status_vacant"] = "🟢 Trống",
                ["map_status_open"] = "🔴 Mở",
                ["map_no_owner"] = "Chưa có chủ",

                // Voice Aura Page
                ["aura_header"] = "Cài Đặt Voice Aura",
                ["aura_subtitle"] = "TÙY CHỈNH TRẢI NGHIỆM",
                ["aura_hero_title"] = "Cá nhân hóa giọng\nnói dẫn đường",
                ["aura_language"] = "Ngôn Ngữ",
                ["aura_gender"] = "Giới Tính Giọng Nói",
                ["aura_male"] = "Nam",
                ["aura_female"] = "Nữ",
                ["aura_speed"] = "Tốc Độ Phát",
                ["aura_speed_slow"] = "CHẬM",
                ["aura_speed_normal"] = "BÌNH THƯỜNG",
                ["aura_speed_fast"] = "NHANH",
                ["aura_radius"] = "Bán Kính Thông Báo",
                ["aura_radius_desc"] = "Khoảng cách Aura sẽ tự động kích hoạt khi bạn đến gần điểm tham quan.",
                ["aura_preview"] = "▶  Nghe Thử Âm Thanh",
                ["aura_save"] = "Lưu Thay Đổi",
                ["aura_saved_ok"] = "Thiết lập đã được lưu lại.",
                ["aura_error_title"] = "Lỗi",
                ["aura_error_play"] = "Không thể phát âm thanh",
                ["alert_success"] = "Thành công",

                // Profile Page
                ["profile_header"] = "Vinh Khanh Guide",
                ["profile_title"] = "Hồ Sơ",
                ["profile_pass_title"] = "7-Day Gold Pass",
                ["profile_aura_title"] = "Cài Đặt Voice Aura",
                ["profile_aura_desc"] = "Tùy chỉnh trải nghiệm giọng nói dẫn đường",
                ["profile_configure"] = "Cấu Hình",
                ["profile_saved"] = "Đã Lưu\nĐịa Điểm",
                ["profile_places"] = "12 địa điểm",
                ["profile_support"] = "Hỗ trợ",
                ["profile_support_24"] = "24/7 Trợ giúp",
                ["profile_history"] = "Lịch Sử Gần Đây",
                ["profile_view_all"] = "Xem Tất Cả",
                ["profile_change_lang"] = "🌐 Đổi Ngôn Ngữ",
                ["profile_lang_desc"] = "Bấm để thay đổi ngôn ngữ ứng dụng",

                // Shop Detail
                ["shop_address"] = "534 Vĩnh Khánh, Phường 10",
                ["shop_tag_signature"] = "GRILLED SEAFOOD WORK",
                ["shop_tag_gem"] = "VIÊN NGỌC QUẬN 4",
                ["shop_aura_tag"] = "Aura AI Guide",
                ["shop_menu_title"] = "Thực Đơn Đặc Biệt",
                ["shop_menu_desc"] = "Hương vị tinh tuyển từ con phố ẩm thực sôi động nhất Sài Gòn. Mô tả đa ngôn ngữ được hỗ trợ bởi Aura.",
                ["shop_add_order"] = "Thêm Vào Đơn",
                ["shop_aura_active"] = "Voice Aura Đang Hoạt Động",
                ["shop_aura_scan"] = "Quét tên món ăn để nghe câu chuyện, nguyên liệu và hương vị bằng ngôn ngữ bạn chọn.",
                ["shop_order_alert"] = "Tính năng đặt món đang được phát triển.",
                ["shop_feature_soon"] = "Tính năng đa ngôn ngữ cho Voice Aura đang được cập nhật.",
                
                ["shop_dish1_desc"] = "Ốc hương rang với muối ớt. Sự cân bằng hoàn hảo giữa độ ngọt của biển và lớp vỏ cay nồng.",
                ["shop_dish2_desc"] = "Rau muống xào nhanh với tỏi thơm lừng.",
                ["shop_dish3_desc"] = "Tôm sú nướng với muối ớt đặc biệt của quán.",
                ["shop_tag_musttry"] = "PHẢI THỬ",
                ["shop_tag_spicy"] = "CAY",

                // Language Page
                ["lang_welcome"] = "Chào mừng đến HeriStep",
                ["lang_subtitle"] = "Vui lòng chọn quốc tịch để tiếp tục",
                ["lang_footer"] = "Bạn có thể thay đổi lựa chọn này bất cứ lúc nào trong Cài đặt Hồ sơ",

                // General
                ["ok"] = "OK",
                ["close"] = "Đóng",
                ["notification"] = "Thông báo",
                ["coming_soon"] = "Sắp ra mắt",
            },

            // ───────────────────────────────────────
            // ENGLISH
            // ───────────────────────────────────────
            ["en"] = new()
            {
                ["tab_explore"] = "Explore",
                ["tab_map"] = "Map",
                ["tab_aura"] = "Aura",
                ["tab_profile"] = "Profile",

                ["main_header"] = "VINH KHANH STREET",
                ["main_hero_tag"] = "DISTRICT 4 - SAIGON",
                ["main_hero_title"] = "Discover\nThe Soul Of\nDistrict 4",
                ["main_hero_desc"] = "Saigon's most vibrant street\nfood paradise, where stories are\ntold through flavors.",
                ["main_btn_map"] = "Map",
                ["main_map_desc"] = "Explore near you",
                ["main_btn_scan"] = "Scan QR",
                ["main_btn_aura"] = "Voice Aura",
                ["main_top_shops"] = "Top Stalls",
                ["main_top_shops_desc"] = "Most favorite",
                ["main_section_top"] = "Must-Try\nSpots",
                ["main_section_top_desc"] = "Top community-rated\nfood stalls",
                ["main_view_all"] = "View\nAll",
                ["main_shop1_tags"] = "🐌 Seafood · Snails",
                ["main_shop2_tags"] = "🥢 Snack · Street food",
                
                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "Customization",
                ["main_voice_desc"] = "Select a voice, type your text, and preview it instantly.",
                ["main_voice_profile"] = "VOICE PROFILE",
                ["main_voice_text"] = "YOUR TEXT",
                ["main_voice_speed"] = "SPEED",
                ["main_voice_btn"] = "TEST VOICE",
                ["main_voice_placeholder"] = "Enter texts here to test...",
                ["main_voice_status_default"] = "Select a voice, enter text, then tap Test Voice",

                ["map_title"] = "Vĩnh Khánh Food Street",
                ["map_active"] = "Active",
                ["map_vacant"] = "Vacant",
                ["map_expired"] = "Expired",
                ["map_your_location"] = "Your Location",
                ["map_listen"] = "🔊 Listen",
                ["map_detail"] = "📋 Details",
                ["map_status_closed"] = "⛔ Closed",
                ["map_status_vacant"] = "🟢 Vacant",
                ["map_status_open"] = "🔴 Open",
                ["map_no_owner"] = "No owner",

                ["aura_header"] = "Voice Aura Settings",
                ["aura_subtitle"] = "CUSTOMIZE YOUR EXPERIENCE",
                ["aura_hero_title"] = "Personalize your\nguide voice",
                ["aura_language"] = "Language",
                ["aura_gender"] = "Voice Gender",
                ["aura_male"] = "Male",
                ["aura_female"] = "Female",
                ["aura_speed"] = "Playback Speed",
                ["aura_speed_slow"] = "SLOW",
                ["aura_speed_normal"] = "NORMAL",
                ["aura_speed_fast"] = "FAST",
                ["aura_radius"] = "Alert Radius",
                ["aura_radius_desc"] = "The distance at which Aura will automatically activate when you approach a point of interest.",
                ["aura_preview"] = "▶  Preview Audio",
                ["aura_save"] = "Save Changes",
                ["aura_saved_ok"] = "Settings saved successfully.",
                ["aura_error_title"] = "Error",
                ["aura_error_play"] = "Unable to play audio",
                ["alert_success"] = "Success",

                ["profile_header"] = "Vinh Khanh Guide",
                ["profile_title"] = "Profile",
                ["profile_change_lang"] = "🌐 Change Language",
                ["profile_lang_desc"] = "Tap to change app language",

                ["shop_address"] = "534 Vinh Khanh, Ward 10",
                ["shop_tag_signature"] = "GRILLED SEAFOOD WORK",
                ["shop_tag_gem"] = "DISTRICT 4 HIDDEN GEM",
                ["shop_aura_tag"] = "Aura AI Guide",
                ["shop_menu_title"] = "Signature Menu",
                ["shop_menu_desc"] = "Curated flavors from Saigon's most vibrant street food artery. Multilingual descriptions powered by Aura.",
                ["shop_add_order"] = "Add to Order",
                ["shop_aura_active"] = "Voice Aura is Active",
                ["shop_aura_scan"] = "Scan any dish name to hear the story, ingredients, and flavor profile in your preferred language.",
                
                ["shop_dish1_desc"] = "Spotted Babylon snails roasted with chili salt. A perfect balance of sea sweetness and spicy crust.",
                ["shop_dish2_desc"] = "Water spinach flash-fried with aromatic garlic.",
                ["shop_dish3_desc"] = "Tiger prawns grilled with spicy house-made chili salt.",
                ["shop_tag_musttry"] = "MUST TRY",
                ["shop_tag_spicy"] = "SPICY",

                ["lang_welcome"] = "Welcome to HeriStep",
                ["lang_subtitle"] = "Please select your nationality to continue",
                ["lang_footer"] = "You can change this anytime in Profile settings",
                ["ok"] = "OK",
                ["close"] = "Close",
                ["notification"] = "Notification",
                ["coming_soon"] = "Coming Soon",
            },

            // ───────────────────────────────────────
            // JAPANESE
            // ───────────────────────────────────────
            ["ja"] = new()
            {
                ["tab_explore"] = "探索",
                ["tab_map"] = "地図",
                ["tab_aura"] = "オーラ",
                ["tab_profile"] = "プロフィール",
                ["main_header"] = "ヴィンカーン通り",
                ["main_hero_tag"] = "4区 - サイゴン",
                ["main_hero_title"] = "4区の\n魂を\n発見しよう",
                ["main_hero_desc"] = "サイゴンで最も活気ある\nストリートフードの楽園。\n味わいで語る物語。",
                ["main_btn_map"] = "地図",
                ["main_map_desc"] = "近くを探索",
                ["main_top_shops"] = "トップ屋台",
                ["main_top_shops_desc"] = "最も人気",
                ["main_btn_scan"] = "QRスキャン",
                ["main_btn_aura"] = "Voice Aura",
                ["main_section_top"] = "必食\nスポット",
                ["main_section_top_desc"] = "コミュニティ評価\nトップの屋台",
                ["main_view_all"] = "すべて\n見る",
                ["main_shop1_tags"] = "🐌 シーフード・巻き貝",
                ["main_shop2_tags"] = "🥢 屋台のスナック",
                
                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "カスタマイズ",
                ["main_voice_desc"] = "音声を選択し、テキストを入力してすぐにプレビューします。",
                ["main_voice_profile"] = "音声プロファイル",
                ["main_voice_text"] = "あなたのテキスト",
                ["main_voice_speed"] = "速度",
                ["main_voice_btn"] = "音声をテスト",
                ["main_voice_placeholder"] = "ここにテストするテキストを入力...",
                ["main_voice_status_default"] = "音声を選び、テキストを入力してテストをタップ",

                ["map_title"] = "ヴィンカーン フードストリート",
                ["map_active"] = "営業中",
                ["map_vacant"] = "空き",
                ["map_expired"] = "期限切れ",
                ["map_your_location"] = "現在地",
                ["map_listen"] = "🔊 聴く",
                ["map_detail"] = "📋 詳細",

                ["profile_title"] = "プロフィール",
                ["profile_change_lang"] = "🌐 言語変更",
                ["profile_lang_desc"] = "タップしてアプリの言語を変更",

                ["shop_address"] = "10区、ヴィンカーン534番地",
                ["shop_tag_signature"] = "グリルシーフード",
                ["shop_tag_gem"] = "4区の隠れた名店",
                ["shop_aura_tag"] = "Aura AI ガイド",
                ["shop_menu_title"] = "特別メニュー",
                ["shop_menu_desc"] = "サイゴンで最も活気あるストリートフードの厳選フレーバー。多言語対応。",
                ["shop_add_order"] = "注文する",
                ["shop_aura_active"] = "Voice Aura 有効",
                ["shop_aura_scan"] = "料理名をスキャンして、お好みの言語でストーリーを聞きましょう。",
                
                ["shop_dish1_desc"] = "チリソルトで焼いたバイ貝。海の甘みとスパイシーな皮の完璧なバランス。",
                ["shop_dish2_desc"] = "香ばしいニンニクと一緒にさっと炒めた空芯菜。",
                ["shop_dish3_desc"] = "自家製のスパイシーなチリソルトで焼いたブラックタイガー。",
                ["shop_tag_musttry"] = "必食",
                ["shop_tag_spicy"] = "スパイシー",
                ["aura_saved_ok"] = "設定が保存されました。",
                ["ok"] = "OK",
                ["close"] = "閉じる",
            },

            // ───────────────────────────────────────
            // KOREAN
            // ───────────────────────────────────────
            ["ko"] = new()
            {
                ["main_header"] = "빈칸 거리",
                ["main_hero_tag"] = "4군 - 사이공",
                ["main_hero_title"] = "4군의\n영혼을\n발견하세요",
                ["main_hero_desc"] = "사이공에서 가장 활기찬\n길거리 음식의 천국,\n맛으로 이야기를 전합니다.",
                ["main_btn_map"] = "지도",
                ["main_map_desc"] = "내 주변 탐색",
                ["main_top_shops"] = "인기 식당",
                ["main_top_shops_desc"] = "가장 좋아하는",
                ["main_section_top"] = "꼭 먹어봐야 할\n맛집",
                ["main_section_top_desc"] = "커뮤니티 평점\n최고 맛집",
                ["main_view_all"] = "전체\n보기",
                ["main_shop1_tags"] = "🐌 해산물 · 달팽이",
                ["main_shop2_tags"] = "🥢 길거리 간식",

                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "커스터마이징",
                ["main_voice_desc"] = "음성을 선택하고 텍스트를 입력하여 즉시 미리 듣습니다.",
                ["main_voice_profile"] = "음성 프로필",
                ["main_voice_text"] = "텍스트",
                ["main_voice_speed"] = "속도",
                ["main_voice_btn"] = "음성 테스트",
                ["main_voice_placeholder"] = "여기에 텍스트를 입력하세요...",
                ["main_voice_status_default"] = "음성을 선택하고 텍스트를 입력한 후 테스트를 탭하세요",

                ["shop_address"] = "10구 빈칸 534번지",
                ["shop_tag_signature"] = "그릴 해산물",
                ["shop_tag_gem"] = "4군의 숨겨진 보석",
                ["shop_aura_tag"] = "Aura AI 가이드",
                ["shop_menu_title"] = "시그니처 메뉴",
                ["shop_menu_desc"] = "사이공에서 가장 활기찬 길거리 음식의 엄선된 맛.",
                ["shop_add_order"] = "주문하기",
                ["shop_aura_active"] = "Voice Aura 활성",
                ["shop_aura_scan"] = "요리 이름을 스캔하여 원하는 언어로 스토리를 들어보세요.",
                
                ["shop_dish1_desc"] = "칠리 소금으로 구운 바빌론 달팽이. 해산물의 달콤함과 매콤함의 완벽한 조화.",
                ["shop_dish2_desc"] = "마늘 향이 향긋한 모닝글로리 볶음.",
                ["shop_dish3_desc"] = "특제 칠리 소금으로 구운 타이거 새우.",
                ["shop_tag_musttry"] = "추천",
                ["shop_tag_spicy"] = "매운",
                ["aura_saved_ok"] = "설정이 저장되었습니다.",
                ["ok"] = "확인",
            },

            // ───────────────────────────────────────
            // CHINESE
            // ───────────────────────────────────────
            ["zh"] = new()
            {
                ["main_header"] = "永庆美食街",
                ["main_hero_tag"] = "4区 - 西贡",
                ["main_hero_title"] = "探索\n4区的\n灵魂",
                ["main_hero_desc"] = "西贡最热闹的街头\n美食天堂，用味觉\n讲述故事。",
                ["main_btn_map"] = "地图",
                ["main_map_desc"] = "探索附近",
                ["main_top_shops"] = "热门摊位",
                ["main_top_shops_desc"] = "最受欢迎",
                ["main_section_top"] = "必尝\n美食",
                ["main_section_top_desc"] = "社区评分\n最高的摊位",
                ["main_view_all"] = "查看\n全部",
                ["main_shop1_tags"] = "🐌 海鲜·各种螺",
                ["main_shop2_tags"] = "🥢 街头小吃",

                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "语音定制",
                ["main_voice_desc"] = "选择语音，输入文本并立即预览。",
                ["main_voice_profile"] = "语音档案",
                ["main_voice_text"] = "您的文本",
                ["main_voice_speed"] = "极速",
                ["main_voice_btn"] = "测试语音",
                ["main_voice_placeholder"] = "在此输入文字以进行测试...",
                ["main_voice_status_default"] = "选择一种声音，输入文字，然后点击测试声音",

                ["shop_address"] = "第10区534号永庆",
                ["shop_tag_signature"] = "烤海鲜",
                ["shop_tag_gem"] = "第四区隐藏的宝石",
                ["shop_aura_tag"] = "Aura AI 指南",
                ["shop_menu_title"] = "招牌菜单",
                ["shop_menu_desc"] = "西贡最热闹街头美食的精选风味。",
                ["shop_add_order"] = "加入订单",
                ["shop_aura_active"] = "Voice Aura 生效",
                ["shop_aura_scan"] = "扫描任何菜名，即可用您首选的语言聆听。",
                
                ["shop_dish1_desc"] = "椒盐烤花螺。海鲜的鲜甜与辣味的完美平衡。",
                ["shop_dish2_desc"] = "蒜香空心菜。",
                ["shop_dish3_desc"] = "秘制椒盐烤黑虎虾。",
                ["shop_tag_musttry"] = "必试",
                ["shop_tag_spicy"] = "辣",
                ["aura_saved_ok"] = "设置已保存。",
                ["ok"] = "确定",
            },

            // ───────────────────────────────────────
            // FRENCH
            // ───────────────────────────────────────
            ["fr"] = new()
            {
                ["main_header"] = "RUE VINH KHANH",
                ["main_hero_tag"] = "DISTRICT 4 - SAÏGON",
                ["main_hero_title"] = "Découvrez\nL'Âme du\nDistrict 4",
                ["main_hero_desc"] = "Le paradis de la cuisine de rue\nle plus vibrant de Saïgon, où les\nhistoires se racontent à travers\nles saveurs.",
                ["main_btn_map"] = "Carte",
                ["main_map_desc"] = "Explorer près de vous",
                ["main_top_shops"] = "Meilleurs Stands",
                ["main_top_shops_desc"] = "Les plus favoris",
                ["main_section_top"] = "À Ne Pas\nManquer",
                ["main_section_top_desc"] = "Les stands les mieux\nnotés par la communauté",
                ["main_view_all"] = "Voir\nTout",
                ["main_shop1_tags"] = "🐌 Fruits de mer · Escargots",
                ["main_shop2_tags"] = "🥢 Snack de rue",

                ["main_voice_tag"] = "VOICE STUDIO",
                ["main_voice_title"] = "Personnalisation",
                ["main_voice_desc"] = "Sélectionnez une voix, tapez votre texte et prévisualisez-le instantanément.",
                ["main_voice_profile"] = "PROFIL VOCAL",
                ["main_voice_text"] = "VOTRE TEXTE",
                ["main_voice_speed"] = "VITESSE",
                ["main_voice_btn"] = "TESTER LA VOIX",
                ["main_voice_placeholder"] = "Entrez des textes ici pour tester...",
                ["main_voice_status_default"] = "Sélectionnez une voix, saisissez du texte, puis appuyez sur Tester la voix",

                ["shop_address"] = "534 Vinh Khanh, Quartier 10",
                ["shop_tag_signature"] = "FRUITS DE MER GRILLÉS",
                ["shop_tag_gem"] = "JOYAU CACHÉ DU QUARTIER 4",
                ["shop_aura_tag"] = "Guide IA Aura",
                ["shop_menu_title"] = "Menu Signature",
                ["shop_menu_desc"] = "Saveurs sélectionnées de l'artère de cuisine de rue la plus vibrante de Saïgon.",
                ["shop_add_order"] = "Ajouter à la commande",
                ["shop_aura_active"] = "Voice Aura est actif",
                ["shop_aura_scan"] = "Scannez n'importe quel nom de plat pour entendre l'histoire.",
                
                ["shop_dish1_desc"] = "Escargots de Babylone tachetés rôtis au sel de chili. Un équilibre parfait entre la douceur de la mer et la croûte épicée.",
                ["shop_dish2_desc"] = "Épinards d'eau sautés à l'ail aromatique.",
                ["shop_dish3_desc"] = "Crevettes tigrées grillées avec du sel de chili épicé fait maison.",
                ["shop_tag_musttry"] = "À ESSAYER ABSOLUMENT",
                ["shop_tag_spicy"] = "ÉPICÉ",
                ["aura_saved_ok"] = "Paramètres enregistrés.",
                ["ok"] = "OK",
            },
            
            // ... (German, Spanish, Thai are handled by fallback)
        };
    }
}

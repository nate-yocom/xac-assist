using SixLabors.Fonts;

namespace XacAssist.Renderer {

    public static class FontManager {
        public const string DEFAULT_FONT_FAMILY = "Roboto";
        public const int DEFAULT_FONT_SIZE = 36;
        public const FontStyle DEFAULT_FONT_STYLE = FontStyle.Regular;

        private static readonly Lazy<FontCollection> s_lazyFontCollection = new Lazy<FontCollection>(() => {
            var collection = new FontCollection();
            foreach(string font in Directory.EnumerateFiles("data/fonts", "*.ttf", new EnumerationOptions() { RecurseSubdirectories = true })) {
                collection.Add(font);
            }
            return collection;
        });
        
        public static FontCollection Fonts { get { return s_lazyFontCollection.Value; } }

        public static Font GetFont() {
            return GetFont(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, DEFAULT_FONT_STYLE);
        }

        public static Font GetFont(FontStyle style) {
            return GetFont(DEFAULT_FONT_FAMILY, DEFAULT_FONT_SIZE, style);
        }

        public static Font GetFont(int size, FontStyle style) {
            return GetFont(DEFAULT_FONT_FAMILY, size, style);
        }

        public static Font GetFont(string family, int size, FontStyle style) {
            FontFamily fontFamily;
            if(!Fonts.TryGet(family, out fontFamily)) {
                fontFamily = Fonts.Families.First();
            }

            return fontFamily.CreateFont(size, style);
        }
    }
}
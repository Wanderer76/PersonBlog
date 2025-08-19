using Blog.Domain.Entities;

namespace Blog.Persistence.Seed
{
    internal class DataCategorySeedData
    {
        private readonly List<string> _categories =
        [
            "Развлечения",
            "Музыка",
            "Игры",
            "Образование",
            "Наука и технологии",
            "Спорт",
            "Кино и анимация",
            "Новости и политика",
            "Авто и транспорт",
            "Путешествия и события",
            "Образ жизни",
            "Кулинария",
            "Юмор",
            "Личный блог",
        ];

        public IEnumerable<Category> GetSeedData()
        {
            return _categories.Select((x, index) => new Category(index + 1, x, (CategoryType)(index + 1)));
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Music.Domain.Entities;

public class Genre : IMusicEntity
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Genre(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}


public class GenreList
{
    public List<Genre> GetGenres()
    {

        return new List<Genre>
    {

new Genre(Guid.Parse("f2778a49-d7bf-4681-902f-dbfcf5ae2a22"),"Альтернативный рок"),
new Genre(Guid.Parse("1e66ef2d-dc44-4a0e-94e4-936865bcd293"),"Альтернативный хип-хоп"),
new Genre(Guid.Parse("1d3caf62-dd89-4a1d-9b59-5390b59083c8"),"Академическая музыка"),
new Genre(Guid.Parse("dd1c1063-a144-4efd-824b-b9549ad31171"),"Африканская музыка"),
new Genre(Guid.Parse("1a0e3094-6407-4b9e-af9f-95e40128a2f3"),"Бардовская песня"),
new Genre(Guid.Parse("c850ac27-166e-4b06-a7ae-d9798c9251ef"),"Битмейкерская"),
new Genre(Guid.Parse("da0c444c-6af4-4dc1-a822-3fa818159f8f"),"Блэк-метал"),
new Genre(Guid.Parse("86e438cf-3974-4b13-b298-f9f67242b40f"),"Блюз"),
new Genre(Guid.Parse("68d95226-3d27-49a9-abe8-7843a4cf047d"),"Военная песня"),
new Genre(Guid.Parse("e94c084c-5f48-4c55-8c17-469b3a6804ec"),"Восточная музыка"),
new Genre(Guid.Parse("d10c70c8-c0c0-401f-aa58-cb36c7f5a256"),"Гангста-рэп"),
new Genre(Guid.Parse("e2319db5-a08c-47c5-9033-00a1c086fca2"),"Готик-рок"),
new Genre(Guid.Parse("9dd7e004-1d84-425b-8108-4e44c51e2f0c"),"Гранж"),
new Genre(Guid.Parse("a99ed714-c09f-4a6e-a097-7752d8544a0a"),"Дарк-эмбиент"),
new Genre(Guid.Parse("b65b84f4-d43c-4a9a-b8c4-4fa4fc80e361"),"Джаз"),
new Genre(Guid.Parse("bd692739-f147-4838-86d4-5dac6c5434da"),"Джаз-рэп"),
new Genre(Guid.Parse("27be86de-805a-4d35-9d6a-89230e076869"),"Драм-н-бэйс"),
new Genre(Guid.Parse("5f4ce11a-a071-4a35-bff4-10985ebb4a3b"),"Дубстеп"),
new Genre(Guid.Parse("d979f535-f46a-4c90-b731-d665c00c74b7"),"Дум-метал"),
new Genre(Guid.Parse("8dfa2863-9d23-4cbd-b692-e9fca360d3f4"),"Дэт-метал"),
new Genre(Guid.Parse("1d4f99b2-5165-4741-9177-ddaddd3a3ed9"),"Дип-хаус"),
new Genre(Guid.Parse("0f1838c9-548b-4bcf-ad1b-d87b9770c03a"),"Диско"),
new Genre(Guid.Parse("df43ae79-3b05-46db-b611-77e4bbc7763b"),"Диксиленд"),
new Genre(Guid.Parse("391456b1-3807-4d68-a744-31b2d489f6cc"),"Дрилл"),
new Genre(Guid.Parse("399cd0cc-de05-4664-ad9f-f3ca17b7f549"),"Индie-рок"),
new Genre(Guid.Parse("d7d432be-0110-487b-b9c6-db86bc6a8d48"),"Кантри"),
new Genre(Guid.Parse("2df59016-82e4-49a2-b91a-4486a70b5684"),"Классическая музыка"),
new Genre(Guid.Parse("a785727b-f20d-49ef-b8a3-3de004dd71e0"),"Латино"),
new Genre(Guid.Parse("f27247d3-864a-47b6-b986-4eeb15898edc"),"Лоу-фай"),
new Genre(Guid.Parse("ec20b847-f556-4ef4-80ef-8edc0df8be2e"),"Метал"),
new Genre(Guid.Parse("9fee064d-041e-40c8-ae28-6bf4252debea"),"Металкор"),
new Genre(Guid.Parse("a017b28d-4044-40e1-a708-4ab9cba8e182"),"Нью-вейв"),
new Genre(Guid.Parse("c3587d6e-37a0-4984-8d84-011e59882588"),"Ню-метал"),
new Genre(Guid.Parse("a39b2367-400c-46bb-91c5-f9ba4285927b"),"Панк-рок"),
new Genre(Guid.Parse("6750e71b-dec4-4494-aee3-b2ee5d0d7a59"),"Пауэр-метал"),
new Genre(Guid.Parse("3f955fd0-c5af-4153-b86c-2c384b33163f"),"Поп-музыка"),
new Genre(Guid.Parse("07b8bb61-386c-425e-8496-85dd58d653ef"),"Пост-панк"),
new Genre(Guid.Parse("eafac7da-7e73-4000-9253-011daf0d246a"),"Прогрессив-хаус"),
new Genre(Guid.Parse("fb07e109-0d38-4feb-889b-6133ac20219b"),"R&B"),
new Genre(Guid.Parse("88b0cab6-6983-45d7-8e09-23b91b035d1e"),"Регги"),
new Genre(Guid.Parse("4751bb51-b803-4819-8bb4-458085f77742"),"Рок"),
new Genre(Guid.Parse("183ab4a5-aefd-4725-9a40-f3b266a64a1f"),"Русский рок"),
new Genre(Guid.Parse("02bb7d51-8284-4af9-a7dd-006beb21c8a1"),"Саундтреки"),
new Genre(Guid.Parse("88c6ca3d-76cd-43f6-8982-dfa5d708677c"),"Свинг"),
new Genre(Guid.Parse("5f70961b-49c3-4570-9604-ac47ba0395cd"),"Симфоник-метал"),
new Genre(Guid.Parse("06ab95f9-a482-4dd9-a149-086443feb8e0"),"Синтипоп"),
new Genre(Guid.Parse("08c747f0-1411-4917-bda3-51c2f0780385"),"Ска"),
new Genre(Guid.Parse("1ac35dd4-782a-4621-9eab-f50a8534637c"),"Соул"),
new Genre(Guid.Parse("58927cf6-b95c-4d64-a9ff-ff11e101d63f"),"Техно"),
new Genre(Guid.Parse("dbf26d58-c773-4511-82fa-b144d0f01a97"),"Транс"),
new Genre(Guid.Parse("adff33bc-f61a-4d89-ada2-eca1fe68bf52"),"Трэп"),
new Genre(Guid.Parse("7379c832-2b23-49c3-9ad2-d08b32fc78c9"),"Трэш-метал"),
new Genre(Guid.Parse("5bd4a0ce-d963-472a-a916-90941977a424"),"Фанк"),
new Genre(Guid.Parse("8be47170-7bd6-4aef-ac82-00146c6d94a2"),"Фолк"),
new Genre(Guid.Parse("8503b304-a3ec-4ff2-b7e7-a61e7b8b1cbb"),"Хард-рок"),
new Genre(Guid.Parse("202d6de5-9d81-4d49-a917-6420066a9304"),"Хардкор-рэп"),
new Genre(Guid.Parse("e318470f-24fc-447a-ac30-787766769013"),"Хаус"),
new Genre(Guid.Parse("98ff54bb-672b-44f8-80ff-bf5515f8d2c8"),"Хви-метал"),
new Genre(Guid.Parse("3412b477-8f20-465e-82e7-7443bb9f473e"),"Хип-хоп"),
new Genre(Guid.Parse("56b16776-2a2a-4f9f-b491-3b5a0383aa9d"),"Хэви-метал"),
new Genre(Guid.Parse("2676182d-4e78-4d00-9f87-3cb15a9f309c"),"Шансон"),
new Genre(Guid.Parse("924ab745-5ac3-408e-ac4c-d1a4245db9e4"),"Электронная музыка"),
new Genre(Guid.Parse("d50b6aa2-38a7-4af2-af3b-844583d32055"),"Эмбиент"),
new Genre(Guid.Parse("1f85bb27-e221-47e1-a9a7-161f6c4e738f"),"Эмо"),
new Genre(Guid.Parse("aa2b80af-21f0-4796-bfcb-33a9bb206d1f"),"Эстрада"),
    };
    }
}
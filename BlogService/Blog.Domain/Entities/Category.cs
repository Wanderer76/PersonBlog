using System.ComponentModel.DataAnnotations;

namespace Blog.Domain.Entities;

public class Category : IBlogEntity
{
    [Key]
    public int Id { get; private set; }

    [StringLength(100)]
    public string Title { get; private set; }

    public CategoryType Type { get; private set; }

    public Category(int id, string title, CategoryType type)
    {
        Id = id;
        Title = title;
        Type = type;
    }
}
public enum CategoryType
{
    Entertainment = 1,
    Music,
    Gaming,
    Education,
    ScienceAndTechnology,
    Sports,
    FilmAndAnimation,
    NewsAndPolitics,
    AutosAndVehicles,
    TravelAndEvents,
    Lifestyle,
    Cooking,
    Comedy,
    PersonalBlog,
}

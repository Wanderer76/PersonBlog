namespace Music.Contract.Models;

public class CreateTrackViewModel
{
    public IReadOnlyList<GenreItem> Genres { get; }

    public CreateTrackViewModel(IReadOnlyList<GenreItem> genres)
    {
        Genres = genres;
    }

    public override bool Equals(object? obj)
    {
        return obj is CreateTrackViewModel other &&
               EqualityComparer<IReadOnlyList<GenreItem>>.Default.Equals(Genres, other.Genres);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Genres);
    }
}
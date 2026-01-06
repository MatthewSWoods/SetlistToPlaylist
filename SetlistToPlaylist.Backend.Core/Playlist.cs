namespace SetlistToPlaylist.Backend.Core;

public abstract class Playlist<T> where T : Playlist<T>
{
    public required string PlaylistId { get; set; }
}

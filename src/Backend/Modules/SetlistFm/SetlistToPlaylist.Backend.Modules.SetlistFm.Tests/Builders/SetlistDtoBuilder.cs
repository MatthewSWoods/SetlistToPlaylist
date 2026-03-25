using SetlistToPlaylist.Backend.Modules.SetlistFm.Abstractions.DTOs;

namespace SetlistToPlaylist.Backend.Modules.SetlistFm.Tests.Builders;

internal sealed class SetlistDtoBuilder
{
    private string _id = "63eb7e6b";
    private string _artistName = "Radiohead";
    private string _venueName = "Roundhouse";
    private string _cityName = "London";
    private string _eventDate = "15-06-2016";
    private SongDto[] _songs = [];

    public SetlistDtoBuilder WithId(string id) { _id = id; return this; }
    public SetlistDtoBuilder WithArtist(string name) { _artistName = name; return this; }
    public SetlistDtoBuilder WithEventDate(string date) { _eventDate = date; return this; }

    public SetlistDtoBuilder WithSongNames(params string[] names)
    {
        _songs = names.Select(n => new SongDto { Name = n }).ToArray();
        return this;
    }

    public SetlistDtoBuilder WithSongs(params SongDto[] songs)
    {
        _songs = songs;
        return this;
    }

    public SetlistDto Build() => new()
    {
        Id = _id,
        EventDate = _eventDate,
        Artist = new ArtistDto { Name = _artistName },
        Venue = new VenueDto { Name = _venueName, City = new CityDto { Name = _cityName } },
        Sets = new SetsDto
        {
            Set = [new SetDto { Song = _songs }]
        }
    };
}

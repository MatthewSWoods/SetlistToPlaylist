# SetlistToPlaylist
A containerised web app frontend + backend that pulls setlist data from the Setlist.Fm api
and converts them into Spotify playlists using the Spotify Api

## Usage & Setup
Simply copy the url of a setlist.fm page and paste it into the box and hit enter, you will be prompted to log in to Spotify.

### Api Keys
Api keys are not checked into this repo, to use this repo after pulling you will have to sign up to the spotify and setlist.fm
apis respectively, then retrieve the needed clientIds/secrets and populate a file named 'ApiSecrets.json' in SetlistToPlaylist.Api root with the following:

~~~
{
  "ApiSecrets": {
    "SetlistFmApiKey": "********",
    "SpotifyClientId": "********",
    "SpotifyClientSecret": "********"
  }
}
~~~

### Spotify Redirect
Ensure that in the spotify app dashboard the callback url for the app (defined in environment variables) has been whitelisted on your Spotify Development dashboard

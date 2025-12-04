import './setlisttoplaylist.css';
import { useState, useEffect } from 'react';
import QRCode from 'react-qr-code';
import axios from 'axios';

function SetlistToPlaylist() {
    const [playlist, setPlaylist] = useState(null);
    const [setlist, setSetlist] = useState(null);
    const [setlistFmUrl, setSetlistFmUrl] = useState("");
    const [failedTracks, setFailedTracks] = useState([]);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [playlistLoading, setPlaylistLoading] = useState(true);
    const [isLoggedIn, setIsLoggedIn] = useState(false); // For toggling login/logout

    const apiBaseUrl = "https://127.0.0.1:5001/"
    const authStatusEndpoint = apiBaseUrl + 'auth/status';
    const loginEndpoint = apiBaseUrl + 'auth/login';
    const logoutEndpoint = apiBaseUrl + 'auth/logout';
    const generatePlaylistEndpoint = apiBaseUrl + 'setlisttoplaylist/GeneratePlaylist'
    const populatePlaylistEndpoint = apiBaseUrl + 'setlisttoplaylist/PopulatePlaylist'

    useEffect(() => {
        const cookieUrl = getCookie("submitted_setlist_url")
        if (cookieUrl)
        {
            setCookie("submitted_setlist_url", "", -1)
            submitUrl(cookieUrl);
        }
    }, []);

    useEffect(() => {
        checkLoginStatus();
    }, []);

    // Function to check login status
    const checkLoginStatus = () => {
        axios.get(authStatusEndpoint, { withCredentials: true })
            .then(response => {
                setIsLoggedIn(response.data);
            })
            .catch(() => setIsLoggedIn(false));
    };

    // Function to toggle login/logout state
    const handleLoginLogout = () => {
        if (isLoggedIn) {
            window.location.href = logoutEndpoint;
        } else {
            window.location.href = loginEndpoint;
        }
    };

    // Handle form submission for Setlist.Fm URL
    function handleUrlSubmission(e) {
        e.preventDefault();
        const url = e.target.InputBar.value;  // Get the input value
        if (url.trim() === '')
        {
            console.log("No Url entered");
            return;
        }
        if (isLoggedIn)
        {
            submitUrl(url)
        }
        else
        {
            setCookie("submitted_setlist_url", url, 1);
            window.location.href = loginEndpoint;
        }
    }

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays*24*60*60*1000));
        let expires = "expires="+ d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    }

    function getCookie(cname) {
        let name = cname + "=";
        let decodedCookie = decodeURIComponent(document.cookie);
        let ca = decodedCookie.split(';');
        for(let i = 0; i <ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    }

    function submitUrl(url)
    {
        setSetlistFmUrl(url)
        setIsSubmitted(true); // Start isSubmitted while fetching data

        // Fetch playlist data from the backend
        axios.post(generatePlaylistEndpoint, JSON.stringify(url), {
            headers: { 'Content-Type': 'application/json' },
            withCredentials: true,
        })
            .then(response => {
                setPlaylist(response.data.playlist);
                setSetlist(response.data.setlist);
                setFailedTracks(response.data.failed_tracks || []);
            })
            .catch(error => console.error("Error fetching playlist data:", error))
            .finally(() => setIsSubmitted(false));
    }

    function refreshPage() {
        window.location.reload();
    }

    // Fetch setlist data and populate playlist
    useEffect(() => {
        if (playlist && setlist) {
            const targetUrl = populatePlaylistEndpoint + `?playlistid=${playlist.id}`;
            setPlaylistLoading(true);
            axios.post(targetUrl, setlist, {
                headers: { 'Content-Type': 'application/json' },
                withCredentials: true,
            })
                .then(response => {
                    setFailedTracks(response.data.failed_tracks || []);
                })
                .catch(error => console.error("Error fetching setlist data:", error))
                .finally(() => setPlaylistLoading(false));
        }
    }, [playlist, setlist]);

    return (
        <div className="setlisttoplaylist-container">
            {!isSubmitted && !playlist ? (
                <div className="setlistfm-url-input">
                    <header>
                        {!playlist ? (
                                <button onClick={handleLoginLogout} className="login-logout-button">
                                    {isLoggedIn ? 'Logout' : 'Login'}
                                </button>)
                            : null}
                    </header>
                    <form onSubmit={handleUrlSubmission}>
                        <input name="InputBar" placeholder="Enter Setlist.Fm URL"/>
                        <button type="submit">Generate Playlist</button>
                    </form>
                    <h2>Enter Setlist.Fm url of setlist to convert</h2>
                </div>) : null}

            {isSubmitted ? (
                <div className="loading-container">Loading Playlist...</div>
            ) : playlist ? (
                <div className="playlist-container">
                    <h1>{playlist.name}</h1>
                    <h2>{playlist.description}</h2>

                    {/* Setlist.Fm URL Button */}
                    <a href={setlistFmUrl} target="_blank">
                        <button className="setlist-button">Open on Setlist.Fm</button>
                    </a>

                    {/* Home Button */}
                    <a>
                        <button className="home-button" onClick={refreshPage}>New Playlist</button>
                    </a>

                    {/* QR Code */}
                    <div className="qr-code-container">
                        <a href={playlist.external_urls?.spotify || ''} target="_blank">
                            <div className="qr-code-box">
                                <QRCode
                                    value={playlist.external_urls?.spotify || ''}
                                    size={128}
                                    level="H"
                                    includeMargin={false}
                                />
                            </div>
                            <h3>Scan or click to listen</h3>
                        </a>
                    </div>

                    {/* Spotify Embed iframe */}
                    {playlistLoading ? (
                        <div className="loading-container">Loading Player...</div>
                    ) : (
                        <iframe
                            title="Spotify Embed: Playlist"
                            src={`https://open.spotify.com/embed/playlist/${playlist.id}?utm_source=generator&theme=0`}
                            width="100%"
                            height="500"
                            style={{border: 0, borderRadius: '20px'}}
                            allow="autoplay; clipboard-write; encrypted-media; fullscreen; picture-in-picture"
                            loading="lazy"
                        />
                    )}
                </div>

            ) : null}

                {/* Setlist (Failed Tracks) */}
                <div className="setlist">
                    {failedTracks.length > 0 && (
                        <div className="failed-tracks">
                            <h3>Failed to add the following tracks:</h3>
                            <ul>
                                {failedTracks.map((track, index) => (
                                    <li key={index}>{track}</li>
                                ))}
                            </ul>
                            <p className="failed-tracks-note">Please add these tracks manually to your
                                playlist.</p>
                        </div>
                    )}
                </div>
        </div>
    );
}

export default SetlistToPlaylist;

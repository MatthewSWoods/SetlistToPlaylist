using Microsoft.AspNetCore.Http;

namespace SetlistToPlaylist.Api.Test.Helpers;

public static class SessionMockHelper
{
    public static ISession CreateMockSession()
    {
        return new TestSession();
    }
}
using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;


namespace SetlistToPlaylist.Api.Test.Helpers
{
    public static class SessionMockHelper
    {
        public static ISession CreateMockSession()
        {
            var session = new Mock<ISession>();

            var sessionStorage = new Dictionary<string, byte[]>();

            session.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Callback<string, byte[]>((key, value) => sessionStorage[key] = value);

            session.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                .Returns((string key, out byte[] value) =>
                {
                    var exists = sessionStorage.TryGetValue(key, out var val);
                    value = val;
                    return exists;
                });

            session.Setup(s => s.Remove(It.IsAny<string>()))
                .Callback<string>(key => sessionStorage.Remove(key));

            return session.Object;
        }
    }
}

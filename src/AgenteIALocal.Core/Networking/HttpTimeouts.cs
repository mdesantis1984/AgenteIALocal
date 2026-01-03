using System;

namespace AgenteIALocal.Core.Networking
{
    public static class HttpTimeouts
    {
        public static readonly TimeSpan LmStudioChatRequestTimeout = TimeSpan.FromMinutes(10);
    }
}

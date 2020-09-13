using System;

namespace WebSocketSharp.Net
{
    [Flags]
    internal enum HttpHeaderType
    {
        Unspecified = 0x0,
        Request = 0x1,
        Response = 0x2,
        Restricted = 0x4,
        MultiValue = 0x8,
        MultiValueInRequest = 0x10,
        MultiValueInResponse = 0x20
    }
}

using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IHandlePacket
    {
        MarshalParseState handlePacket<T>(PacketReader packet, T client, ushort blockId) where T : IBaseClient;
    }
}

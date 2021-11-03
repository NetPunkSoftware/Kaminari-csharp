
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IMarshal : IHandlePacket
    {
        int size<T>();
        void Update<T>(T client, ushort blockId) where T : IBaseClient;
        void Reset();
        int PacketSize(PacketReader packet);
    }
}

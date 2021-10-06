using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IMarshal : IHandlePacket
    {
        int size<T>();
        bool Update<T>(T client, ushort blockId) where T : IBaseClient;
    }
}

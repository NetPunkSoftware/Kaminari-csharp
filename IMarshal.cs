using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IMarshal : IHandlePacket
    {
        int size<T>();
    }
}

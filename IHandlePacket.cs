using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public interface IHandlePacket
    {
        bool handlePacket<T>(PacketReader packet, T client) where T : IBaseClient;
    }
}

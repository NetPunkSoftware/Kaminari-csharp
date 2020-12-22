using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHandlePacket
{
    bool handlePacket<T>(PacketReader packet, T client) where T : IBaseClient;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMarshal : IHandlePacket
{
    int size<T>();
}

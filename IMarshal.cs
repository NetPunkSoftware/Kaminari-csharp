using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public interface IMarshal : IHandlePacket
    {
        int size<T>();
    }
}

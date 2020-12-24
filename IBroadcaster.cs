using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public interface IBroadcaster<T>
    {
        void broadcast(Action<T> operation);
    }
}

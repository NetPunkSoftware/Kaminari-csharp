using System;
using System.Collections;
using System.Collections.Generic;


namespace Kaminari
{
    public interface IBroadcaster<T>
    {
        void broadcast(Action<T> operation);
    }
}

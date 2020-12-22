using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBroadcaster<T>
{
    void broadcast(IBroadcastOperation<T> operation);
}

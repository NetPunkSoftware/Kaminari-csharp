using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBroadcaster<T>
{
    void broadcast(Action<T> operation);
}

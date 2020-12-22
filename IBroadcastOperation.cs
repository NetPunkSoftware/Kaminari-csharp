using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBroadcastOperation<T>
{
    void onCandidate(T pq);
}

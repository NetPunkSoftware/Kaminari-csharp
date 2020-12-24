using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public interface IHasId : IData
    {
        ulong getId();
        void setId(ulong id);
    }
}

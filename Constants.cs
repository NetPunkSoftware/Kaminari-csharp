using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public static class Constants
    {
        public static ushort WorldHeartBeat = 50;
        public static ushort PingInterval = 100;
        public static ushort MaximumBlocksUntilResync = 200;
        public static ushort MaxBlocksUntilDisconnection = 300;
        public static ushort ResendThreshold = (ushort)(250 / WorldHeartBeat);
    }
}

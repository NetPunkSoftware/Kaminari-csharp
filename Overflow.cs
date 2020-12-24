using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaminari
{
    public static class Overflow
    {
        // TODO(gpascualg): Templatize
        public static bool le(ushort x, ushort y, ushort threshold = ushort.MaxValue / 2)
        {
            return
                (x < y && (y - x) < threshold) ||  // Standard case, 200 < 255 && (255 - 200) < thr
                (x > y && (x - y) > threshold);    // Overflow case, 255 > 1 && (255 - 1) > thr
        }

        public static bool ge(ushort x, ushort y, ushort threshold = ushort.MaxValue / 2)
        {
            return
                (x > y && (x - y) < threshold) ||  // Standard case, 255 > 200 && (255 - 200) < thr
                (y > x && (y - x) > threshold);    // Overflow case, 255 > 1 && (255 - 1) > thr
        }

        public static bool geq(ushort x, ushort y, ushort threshold = ushort.MaxValue / 2)
        {
            return
                (x >= y && (x - y) < threshold) ||  // Standard case, 255 > 200 && (255 - 200) < thr
                (y > x && (y - x) > threshold);    // Overflow case, 255 > 1 && (255 - 1) > thr
        }

        public static ushort sub(ushort x, ushort y)
        {
            return
                (x > y) ? (ushort)(x - y) : (ushort)(ushort.MaxValue - y + x);
        }

        public static ushort sub0(ushort x, ushort y)
        {
            ushort z = sub(x, y);
            if (z != 0)
            {
                return z;
            }
            return ushort.MaxValue;
        }

        public static ushort inc(ushort x)
        {
            return (ushort)(++x);
        }

        public static ushort inc0(ushort x)
        {
            ushort z = inc(x);
            if (z != 0)
            {
                return z;
            }
            return 1;
        }
    }
}

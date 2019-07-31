using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Temperature
{
    class Record
    {
        public static volatile List<int[]> recordList = new List<int[]>();
        public static Queue<int[]> recordQueue = new Queue<int[]>();
        public static int num = 0;

        public static void NewRecord(int tp)
        {
            int[] t = new int[2];
            t[0] = ++num;
            t[1] = tp;
            recordQueue.Enqueue(t);
        }
    }
}

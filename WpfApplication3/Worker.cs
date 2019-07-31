using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace Temperature
{
    class Worker
    {
        private static List<Thread> threadList = new List<Thread>();
        private static List<Worker> workerList = new List<Worker>();

        public static void WorkerExit()
        {
            foreach(Thread t in threadList)
            {
                t.Abort();
            }
        }

        public static void WorkerAdd(Worker wk)
        {
            workerList.Add(wk);
        }

        private string name;
        public delegate void WorkContent();
        
        public Worker(string n, WorkContent wk)
        {
            this.name = n;
            Thread t = new Thread(new ThreadStart(wk));
            workerList.Add(this);
            threadList.Add(t);
            t.Start();
        }

        public static Worker GetWoker(string s)
        {
            foreach(Worker wk in workerList)
            {
                if (wk.name == s)
                {
                    return wk;
                }
            }
            return null;
        }
    }
}

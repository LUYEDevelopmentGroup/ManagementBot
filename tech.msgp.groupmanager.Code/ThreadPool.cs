using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CQ2IOT
{
    public class pThreadPool
    {
        public int min_size, max_size;
        public int size => mainpool.Count();
        public int queuelen => workloads.Count();
        public int excepttionlen => exceptionqueue.Count();
        public int queue_max_len;
        public event stateChange onWorkloadStartProcess;
        public event stateChange onWorkloadStopProcess;

        private readonly List<pThread> mainpool;
        private readonly List<Workload> workloads;
        private readonly Dictionary<Guid, Exception> exceptionqueue;
        public int exceptionmaxlen;
        public int busythread { get; private set; }

        public delegate void stateChange(Guid workload);

        private class Workload
        {
            public workload work;
            public bool claimed = false;
            public Guid id;
        }

        private class pThread
        {
            public Thread t;
            public bool busy = false;
            public bool kill = false;
            public pThread(pThreadPool pool)
            {
                t = new Thread(new ThreadStart(() => { pool.workerthread(this); }));
                t.Start();
            }
        }

        public delegate void workload();

        private void doNothing(Guid id) { }

        public pThreadPool(int queue_max_len = 50, int min = 10, int max = 20, int max_exception_queue = 10)
        {
            busythread = 0;
            min_size = min;
            max_size = max;
            this.queue_max_len = queue_max_len;
            exceptionmaxlen = max_exception_queue;
            onWorkloadStartProcess += doNothing;
            onWorkloadStopProcess += doNothing;
            mainpool = new List<pThread>();
            workloads = new List<Workload>();
            exceptionqueue = new Dictionary<Guid, Exception>();
            do
            {
                mainpool.Add(new pThread(this));
            } while (mainpool.Count < min_size);
        }
        private void workerthread(pThread p)
        {
            try
            {
                while (!p.kill)
                {
                    Thread.Sleep(1);
                    p.busy = false;
                    Workload selected = null;
                    lock (workloads)
                    {
                        foreach (Workload wl in workloads)
                        {
                            if (!wl.claimed)
                            {
                                wl.claimed = true;
                                selected = wl;
                                break;
                            }
                        }
                    }
                    if (selected != null)
                    {
                        p.busy = true;
                        busythread++;
                        try
                        {
                            try
                            {
                                onWorkloadStartProcess.Invoke(selected.id);
                                selected.work();
                                onWorkloadStopProcess.Invoke(selected.id);
                            }
                            catch (Exception err)
                            {
                                if (exceptionqueue.Count() <= exceptionmaxlen)
                                {
                                    exceptionqueue.Add(selected.id, err);
                                }
                            }
                            lock (workloads)
                            {
                                workloads.Remove(selected);
                            }

                            busythread--;
                        }
                        catch
                        {
                            busythread--;
                            p.busy = false;
                            mainpool.Remove(p);//线程挂啦！删掉！
                            throw;
                        }
                    }
                }
            }
            catch
            {
                mainpool.Remove(p);//线程挂啦！删掉！
                throw;
            }
        }

        public bool submitWorkload(workload work)
        {
            if (exceptionqueue.Count() >= exceptionmaxlen)
            {
                throw new Exception("Exception list too long. Call popException() or clearExceptions().");
            }
            if (workloads.Count >= queue_max_len)
            {
                return false;//队列超长，停止发送
            }

            Workload wl = new Workload() { claimed = false, work = work, id = Guid.NewGuid() };
            lock (workloads)
            {
                workloads.Add(wl);
            }

            if (busythread == mainpool.Count)//全忙
            {
                if (mainpool.Count < max_size)
                {//池未满
                    mainpool.Add(new pThread(this));//加线程
                }
            }
            return true;
        }

#pragma warning disable CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
        public Dictionary<Guid, Exception>? popException()
#pragma warning restore CS8632 // 只能在 "#nullable" 注释上下文内的代码中使用可为 null 的引用类型的注释。
        {
            Dictionary<Guid, Exception> tmpp = exceptionqueue;
            return exceptionqueue;
        }

        public void clearExceptions()
        {
            exceptionqueue.Clear();
        }
    }
}

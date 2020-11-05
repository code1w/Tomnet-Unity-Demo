/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/15
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using TomNet.Sockets;
using TomNet.Common;
using TomNet.Protocol;

namespace TomNet.Core
{
    public class ThreadManager
    {
        private bool running = false;
        private Thread inThread;
        private bool inHasQueuedItems = false;
        private Queue<Hashtable> inThreadQueue = new Queue<Hashtable>();
        private object inQueueLocker = new object();
        private Thread outThread;
        private bool outHasQueuedItems = false;
        private Queue<Hashtable> outThreadQueue = new Queue<Hashtable>();
        private object outQueueLocker = new object();

        private static void Sleep(int ms)
        {
            Thread.Sleep(ms);
        }

        private void InThread()
        {
            while (running)
            {
                Sleep(5);
                if (!inHasQueuedItems)
                {
                    continue;
                }
                lock (inQueueLocker)
                {
                    while (inThreadQueue.Count > 0)
                    {
                        Hashtable item = inThreadQueue.Dequeue();
                        ProcessItem(item);
                    }
                    inHasQueuedItems = false;
                }
            }
        }

        private void OutThread()
        {
            while (running)
            {
                Sleep(5);
                if (!outHasQueuedItems)
                {
                    continue;
                }
                lock (outQueueLocker)
                {
                    while (outThreadQueue.Count > 0)
                    {
                        Hashtable item = outThreadQueue.Dequeue();
                        ProcessOutItem(item);
                    }
                    outHasQueuedItems = false;
                }
            }
        }

        private void ProcessOutItem(Hashtable item)
        {
            object obj = item["callback"];
            WriteBinaryDataDelegate writeBinaryDataDelegate = obj as WriteBinaryDataDelegate;
            if (writeBinaryDataDelegate != null)
            {
                ByteBuffer binData = item["data"] as ByteBuffer;
                IMsgHeader header = item["header"] as IMsgHeader;
                bool udp = (bool)item["udp"];
                writeBinaryDataDelegate(header, binData, udp);
            }
        }

        private void ProcessItem(Hashtable item)
        {
            object cb = item["callback"];
            OnDataDelegate onDataDelegate = cb as OnDataDelegate;
            if (onDataDelegate != null)
            {
                ByteBuffer msg = (ByteBuffer)item["data"];
                onDataDelegate(msg);
            }
            else
            {
                (cb as ParameterizedThreadStart)?.Invoke(item);
            }
        }

        public void Start()
        {
            if (!running)
            {
                running = true;
                if (inThread == null)
                {
                    inThread = new Thread(InThread);
                    inThread.IsBackground = true;
                    inThread.Start();
                }
                if (outThread == null)
                {
                    outThread = new Thread(OutThread);
                    outThread.IsBackground = true;
                    outThread.Start();
                }
            }
        }

        public void Stop()
        {
            Thread thread = new Thread(StopThread);
            thread.Start();
        }

        private void StopThread()
        {
            running = false;
            if (inThread != null)
            {
                inThread.Join();
            }
            if (outThread != null)
            {
                outThread.Join();
            }
            inThread = null;
            outThread = null;
        }


        public void EnqueueDataCall(OnDataDelegate callback, ByteBuffer data)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["callback"] = callback;
            hashtable["data"] = data;
            lock (inQueueLocker)
            {
                inThreadQueue.Enqueue(hashtable);
                inHasQueuedItems = true;
            }
        }

        public void EnqueueCustom(ParameterizedThreadStart callback, Hashtable data)
        {
            data["callback"] = callback;
            lock (inQueueLocker)
            {
                inThreadQueue.Enqueue(data);
                inHasQueuedItems = true;
            }
        }

        public void EnqueueSend(WriteBinaryDataDelegate callback, IMsgHeader header, ByteBuffer data, bool udp)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["callback"] = callback;
            hashtable["header"] = header;
            hashtable["data"] = data;
            hashtable["udp"] = udp;
            lock (outQueueLocker)
            {
                outThreadQueue.Enqueue(hashtable);
                outHasQueuedItems = true;
            }
        }

        public void EnqueueSend(WriteBinaryDataDelegate callback, ByteBuffer data)
        {
            Hashtable hashtable = new Hashtable();
            hashtable["callback"] = callback;
            hashtable["data"] = data;
            lock (outQueueLocker)
            {
                outThreadQueue.Enqueue(hashtable);
                outHasQueuedItems = true;
            }
        }
    }
}

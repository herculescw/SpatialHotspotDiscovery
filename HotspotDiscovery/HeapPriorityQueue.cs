using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public class HeapPriorityQueue<T>
    {
        private static int DEFAULTCAPACITY = 1024;

        private KeyValueEntry<int, T>[] queue;

        private Dictionary<T, int> entryToPos;

        private int count;

        public HeapPriorityQueue()
            : this(DEFAULTCAPACITY)
        {
        }

        public HeapPriorityQueue(int capacity)
        {
            queue = new KeyValueEntry<int, T>[capacity];
            entryToPos = new Dictionary<T, int>(capacity);
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public Boolean IsEmpty()
        {
            return Count == 0;
        }

        /// <summary>
        /// Operation: O(1)
        /// </summary>
        public void Clear()
        {
            count = 0;
            queue = new KeyValueEntry<int, T>[DEFAULTCAPACITY];
            entryToPos.Clear();
        }

        /// <summary>
        /// Operation: O(1)
        /// </summary>
        public bool Contains(T element)
        {
            return entryToPos.ContainsKey(element);
        }

        /// <summary>
        /// Operation: O(1)
        /// </summary>
        public int PeekPriority()
        {
            if (IsEmpty())
            {
                return 0;
            }

            return queue[0].Key;
        }

        /// <summary>
        /// Operation: O(1)
        /// </summary>
        public T Peek()
        {
            if (IsEmpty())
            {
                return default(T);
            }

            return queue[0].Value;
        }

        /// <summary>
        /// If the queue is empty we do just return null
        /// 
        /// Operation: O(log n)
        /// </summary>
        public T Dequeue()
        {
            if (IsEmpty())
            {
                return default(T);
            }

            KeyValueEntry<int, T> result = queue[0];

            queue[0] = queue[count - 1];
            RemovePosition(result.Value);
            UpdatePosition(queue[0].Value, 0);
            queue[count - 1] = null;
            count--;
            BubbleDown(0);

            return result.Value;
        }

        /// <summary>
        /// Operation: O(log n)
        /// 
        /// InvalidOperationException: If we add an element which is alread in the Queue
        /// </summary>
        public void Enqueue(T element, int priority)
        {
            if (Contains(element))
            {
                throw new InvalidOperationException("Cannot add an element which is already in the queue");
            }

            if (count == queue.Length)
            {
                KeyValueEntry<int, T>[] oldQueue = queue;
                queue = new KeyValueEntry<int, T>[oldQueue.Length * 3 / 2 + 1];
                Array.Copy(oldQueue, queue, oldQueue.Length);
            }

            queue[count] = new KeyValueEntry<int, T>(priority, element);
            UpdatePosition(element, count);
            count++;

            BubbleUp(count - 1);
        }

        /// <summary>
        /// Operation: O(log n)
        /// 
        /// InvalidOperationException: If we want to update the priority of an element which is not in the queue
        /// </summary>
        public void UpdatePriority(T element, int newPrio)
        {
            if (!entryToPos.ContainsKey(element))
            {
                throw new InvalidOperationException("Cannot update the priority of an element which is not in the queue");
            }

            int pos = entryToPos[element];
            int oldPrio = queue[pos].Key;

            if (oldPrio == newPrio)
            {
                return;
            }

            queue[pos].Key = newPrio;

            if (oldPrio < newPrio)
            {
                BubbleDown(pos);
            }
            else
            {
                BubbleUp(pos);
            }
        }

        /// <summary>
        /// Moving root to correct location
        /// </summary>
        private void BubbleDown(int i)
        {
            int minPos;
            KeyValueEntry<int, T> min;
            int left;
            int right;

            while (true)
            {
                minPos = i;
                min = queue[i];
                left = 2 * i + 1;
                right = 2 * i + 2;

                if (left < count && queue[left].Key < min.Key)
                {
                    minPos = left;
                    min = queue[left];
                }

                if (right < count && queue[right].Key < min.Key)
                {
                    minPos = right;
                    min = queue[right];
                }

                if (min == queue[i])
                {
                    break;
                }
                else
                {
                    // swap
                    queue[minPos] = queue[i];
                    queue[i] = min;

                    UpdatePosition(queue[minPos].Value, minPos);
                    UpdatePosition(queue[i].Value, i);

                    i = minPos;
                }
            }
        }

        /// <summary>
        /// Moving last element of last level to correct location
        /// </summary>
        private void BubbleUp(int i)
        {
            int n = i;
            int up = (n - 1) / 2;

            while (up >= 0 && queue[up].Key > queue[n].Key)
            {
                var entry = queue[up];
                queue[up] = queue[n];
                queue[n] = entry;

                UpdatePosition(queue[n].Value, n);
                UpdatePosition(queue[up].Value, up);

                n = up;
                up = (up - 1) / 2;
            }
        }

        private void UpdatePosition(T element, int pos)
        {
            if (entryToPos.ContainsKey(element))
            {
                entryToPos.Remove(element);
            }

            entryToPos.Add(element, pos);
        }

        private void RemovePosition(T element)
        {
            entryToPos.Remove(element);
        }
    }
}


public class KeyValueEntry<K, V>
{
    public K Key
    {
        get;
        set;
    }

    public V Value
    {
        get;
        set;
    }

    public KeyValueEntry(K key, V value)
    {
        Key = key;
        Value = value;
    }
}


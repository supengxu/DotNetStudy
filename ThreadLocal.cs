using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Study.Threading
{
    public class ThreadLocal<T>
    {
        private static int _nextHashCode = 0;
        private static readonly object _lock = new object();
        
        private readonly int _threadLocalHashCode;
        
        protected virtual T InitialValue()
        {
            return default(T);
        }
        
        public static ThreadLocal<T> WithInitial(Func<T> supplier)
        {
            return new SuppliedThreadLocal<T>(supplier);
        }
        
        public ThreadLocal()
        {
            _threadLocalHashCode = NextHashCode();
        }
        
        private static int NextHashCode()
        {
            lock (_lock)
            {
                int current = _nextHashCode;
                _nextHashCode = current + 0x61c88647;
                return current;
            }
        }
        
        public T Get()
        {
            ThreadLocalMap map = GetMap(Thread.CurrentThread);
            if (map != null)
            {
                ThreadLocalMap.Entry e = map.GetEntry(this);
                if (e != null)
                {
                    return (T)e.Value;
                }
            }
            return SetInitialValue(Thread.CurrentThread);
        }
        
        private T SetInitialValue(Thread t)
        {
            T value = InitialValue();
            ThreadLocalMap map = GetMap(t);
            if (map != null)
            {
                map.Set(this, value);
            }
            else
            {
                CreateMap(t, value);
            }
            return value;
        }
        
        public void Set(T value)
        {
            ThreadLocalMap map = GetMap(Thread.CurrentThread);
            if (map != null)
            {
                map.Set(this, value);
            }
            else
            {
                CreateMap(Thread.CurrentThread, value);
            }
        }
        
        public void Remove()
        {
            ThreadLocalMap m = GetMap(Thread.CurrentThread);
            m?.Remove(this);
        }
        
        protected virtual ThreadLocalMap GetMap(Thread t)
        {
            return t.GetThreadLocalMap();
        }
        
        protected virtual void CreateMap(Thread t, T firstValue)
        {
            t.SetThreadLocalMap(new ThreadLocalMap(this, firstValue));
        }
        
        internal int ThreadLocalHashCode => _threadLocalHashCode;
        
        sealed class SuppliedThreadLocal : ThreadLocal<T>
        {
            private readonly Func<T> _supplier;
            
            internal SuppliedThreadLocal(Func<T> supplier)
            {
                _supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
            }
            
            protected override T InitialValue()
            {
                return _supplier();
            }
        }
        
        public class ThreadLocalMap
        {
            public class Entry : WeakReference<ThreadLocal<T>>
            {
                public object Value { get; set; }
                
                public Entry(ThreadLocal<T> key, object value) : base(key)
                {
                    Value = value;
                }
            }
            
            private const int InitialCapacity = 16;
            private Entry[] _table;
            private int _size;
            private int _threshold;
            
            public ThreadLocalMap(ThreadLocal<T> firstKey, object firstValue)
            {
                _table = new Entry[InitialCapacity];
                int index = firstKey.ThreadLocalHashCode & (InitialCapacity - 1);
                _table[index] = new Entry(firstKey, firstValue);
                _size = 1;
                SetThreshold(InitialCapacity);
            }
            
            private void SetThreshold(int len)
            {
                _threshold = len * 2 / 3;
            }
            
            private static int NextIndex(int i, int len)
            {
                return ((i + 1 < len) ? i + 1 : 0);
            }
            
            private static int PrevIndex(int i, int len)
            {
                return ((i - 1 >= 0) ? i - 1 : len - 1);
            }
            
            public Entry GetEntry(ThreadLocal<T> key)
            {
                int index = key.ThreadLocalHashCode & (_table.Length - 1);
                Entry e = _table[index];
                if (e != null && e.Target == key)
                {
                    return e;
                }
                return GetEntryAfterMiss(key, index, e);
            }
            
            private Entry GetEntryAfterMiss(ThreadLocal<T> key, int i, Entry e)
            {
                Entry[] tab = _table;
                int len = tab.Length;
                
                while (e != null)
                {
                    ThreadLocal<T> k = e.Target;
                    if (k == key)
                    {
                        return e;
                    }
                    if (k == null)
                    {
                        ExpungeStaleEntry(i);
                    }
                    else
                    {
                        i = NextIndex(i, len);
                    }
                    e = tab[i];
                }
                return null;
            }
            
            public void Set(ThreadLocal<T> key, object value)
            {
                Entry[] tab = _table;
                int len = tab.Length;
                int i = key.ThreadLocalHashCode & (len - 1);
                
                for (Entry e = tab[i]; e != null; e = tab[i = NextIndex(i, len)])
                {
                    ThreadLocal<T> k = e.Target;
                    if (k == key)
                    {
                        e.Value = value;
                        return;
                    }
                    if (k == null)
                    {
                        ReplaceStaleEntry(key, value, i);
                        return;
                    }
                }
                
                tab[i] = new Entry(key, value);
                _size++;
                if (!CleanSomeSlots(i, _size) && _size >= _threshold)
                {
                    Rehash();
                }
            }
            
            public void Remove(ThreadLocal<T> key)
            {
                Entry[] tab = _table;
                int len = tab.Length;
                int i = key.ThreadLocalHashCode & (len - 1);
                
                for (Entry e = tab[i]; e != null; e = tab[i = NextIndex(i, len)])
                {
                    if (e.Target == key)
                    {
                        e.Clear();
                        ExpungeStaleEntry(i);
                        return;
                    }
                }
            }
            
            private void ReplaceStaleEntry(ThreadLocal<T> key, object value, int staleSlot)
            {
                Entry[] tab = _table;
                int len = tab.Length;
                Entry e;
                
                int slotToExpunge = staleSlot;
                for (int i = PrevIndex(staleSlot, len); (e = tab[i]) != null; i = PrevIndex(i, len))
                {
                    if (e.Target == null)
                    {
                        slotToExpunge = i;
                    }
                }
                
                for (int i = NextIndex(staleSlot, len); (e = tab[i]) != null; i = NextIndex(i, len))
                {
                    if (e.Target == key)
                    {
                        e.Value = value;
                        tab[i] = tab[staleSlot];
                        tab[staleSlot] = e;
                        
                        if (slotToExpunge == staleSlot)
                        {
                            slotToExpunge = i;
                        }
                        CleanSomeSlots(ExpungeStaleEntry(slotToExpunge), len);
                        return;
                    }
                    if (e.Target == null && slotToExpunge == staleSlot)
                    {
                        slotToExpunge = i;
                    }
                }
                
                tab[staleSlot].Value = null;
                tab[staleSlot] = new Entry(key, value);
                
                if (slotToExpunge != staleSlot)
                {
                    CleanSomeSlots(ExpungeStaleEntry(slotToExpunge), len);
                }
            }
            
            private int ExpungeStaleEntry(int staleSlot)
            {
                Entry[] tab = _table;
                int len = tab.Length;
                
                tab[staleSlot].Value = null;
                tab[staleSlot] = null;
                _size--;
                
                Entry e;
                int i;
                for (i = NextIndex(staleSlot, len); (e = tab[i]) != null; i = NextIndex(i, len))
                {
                    ThreadLocal<T> k = e.Target;
                    if (k == null)
                    {
                        e.Value = null;
                        tab[i] = null;
                        _size--;
                    }
                    else
                    {
                        int h = k.ThreadLocalHashCode & (len - 1);
                        if (h != i)
                        {
                            tab[i] = null;
                            while (tab[h] != null)
                            {
                                h = NextIndex(h, len);
                            }
                            tab[h] = e;
                        }
                    }
                }
                return i;
            }
            
            private bool CleanSomeSlots(int i, int n)
            {
                bool removed = false;
                Entry[] tab = _table;
                int len = tab.Length;
                
                do
                {
                    i = NextIndex(i, len);
                    Entry e = tab[i];
                    if (e != null && e.Target == null)
                    {
                        n = len;
                        removed = true;
                        i = ExpungeStaleEntry(i);
                    }
                } while ((n >>>= 1) != 0);
                
                return removed;
            }
            
            private void Rehash()
            {
                ExpungeStaleEntries();
                
                if (_size >= _threshold - _threshold / 4)
                {
                    Resize();
                }
            }
            
            private void Resize()
            {
                Entry[] oldTab = _table;
                int oldLen = oldTab.Length;
                int newLen = oldLen * 2;
                Entry[] newTab = new Entry[newLen];
                int count = 0;
                
                for (int i = 0; i < oldLen; i++)
                {
                    Entry e = oldTab[i];
                    if (e != null)
                    {
                        ThreadLocal<T> k = e.Target;
                        if (k == null)
                        {
                            e.Value = null;
                        }
                        else
                        {
                            int h = k.ThreadLocalHashCode & (newLen - 1);
                            while (newTab[h] != null)
                            {
                                h = NextIndex(h, newLen);
                            }
                            newTab[h] = e;
                            count++;
                        }
                    }
                }
                
                SetThreshold(newLen);
                _size = count;
                _table = newTab;
            }
            
            private void ExpungeStaleEntries()
            {
                Entry[] tab = _table;
                int len = tab.Length;
                for (int j = 0; j < len; j++)
                {
                    Entry e = tab[j];
                    if (e != null && e.Target == null)
                    {
                        ExpungeStaleEntry(j);
                    }
                }
            }
        }
        
        public new class InheritableThreadLocal : ThreadLocal<T>
        {
            protected override ThreadLocalMap GetMap(Thread t)
            {
                return t.GetInheritableThreadLocals();
            }
            
            protected override void CreateMap(Thread t, T firstValue)
            {
                t.SetInheritableThreadLocalMap(new ThreadLocalMap(this, firstValue));
            }
            
            protected virtual T ChildValue(T parentValue)
            {
                return parentValue;
            }
        }
    }
    
    internal static class ThreadLocalExtensions
    {
        private static readonly ConcurrentDictionary<int, ThreadLocalMap> _threadLocals = new ConcurrentDictionary<int, ThreadLocalMap>();
        private static readonly ConcurrentDictionary<int, ThreadLocalMap> _inheritableThreadLocals = new ConcurrentDictionary<int, ThreadLocalMap>();
        
        public static ThreadLocalMap GetThreadLocalMap(this Thread thread)
        {
            return _threadLocals.GetOrDefault(thread.ManagedThreadId);
        }
        
        public static void SetThreadLocalMap(this Thread thread, ThreadLocalMap map)
        {
            _threadLocals[thread.ManagedThreadId] = map;
        }
        
        public static ThreadLocalMap GetInheritableThreadLocals(this Thread thread)
        {
            return _inheritableThreadLocals.GetOrDefault(thread.ManagedThreadId);
        }
        
        public static void SetInheritableThreadLocalMap(this Thread thread, ThreadLocalMap map)
        {
            _inheritableThreadLocals[thread.ManagedThreadId] = map;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic
{
    class HistoryBuffer<T> where T: class
    {
        T[] buffer;
        int start, end;
        int curPos;

        public HistoryBuffer(int size)
        {
            if (size < 2)
                throw new Exception("Too short ring buffer");
            buffer = new T[size];
            start = end = 0;
        }

        public T Top()
        {
            if (end == start)
                return null;
            return buffer[curPos == 0 ? buffer.Length - 1 : curPos - 1];
        }

        public void Add(T value)
        {
            buffer[curPos] = value;
            ++curPos;
            if (curPos >= buffer.Length)
                curPos = 0;
            if (curPos == start)
            {
                ++start;
                if (start >= buffer.Length)
                    start = 0;
            }
            end = curPos;
            //Debug.Log($"Add {curPos} ({start}:{end})");
        }

        public T Back()
        {
            if (curPos == start)
                return null;
            --curPos;
            if (curPos < 0)
                curPos = buffer.Length - 1;
            //Debug.Log($"Back {curPos} ({start}:{end})");
            return Top();
        }

        public T Forward()
        {
            if (curPos == end)
                return null;
            ++curPos;
            if (curPos >= buffer.Length)
                curPos = 0;
            //Debug.Log($"Forward {curPos} ({start}:{end})");
            return Top();
        }
    }
}

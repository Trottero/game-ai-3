using System;

namespace Models
{
    public struct QueuedAction<T>
    {
        public Action<T> Action;
        public T Payload;
    }
}
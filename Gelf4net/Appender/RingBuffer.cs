using System;

namespace AsynchronousLog4NetAppenders
{
    public class RingBuffer<T> : IQueue<T>
    {
        private readonly Object lockObject = new Object();
        private readonly T[] buffer;
        private readonly Int32 size;
        private Int32 readIndex;
        private Int32 writeIndex;
        private Boolean bufferFull;

        public event Action<Object, EventArgs> BufferOverflow;

        public RingBuffer( Int32 size )
        {
            this.size = size;
            buffer = new T[size];
        }

        public void Enqueue( T item )
        {
            lock( lockObject )
            {
                buffer[writeIndex] = item;
                writeIndex = (++writeIndex) % size;
                if( bufferFull )
                {
                    if( BufferOverflow != null )
                    {
                        BufferOverflow( this, EventArgs.Empty );
                    }
                    readIndex = writeIndex;
                }
                else if( writeIndex == readIndex )
                {
                    bufferFull = true;
                }
            }
        }

        public Boolean TryDequeue( out T ret )
        {
            if( readIndex == writeIndex && !bufferFull )
            {
                ret = default(T);
                return false;
            }
            lock( lockObject )
            {
                if( readIndex == writeIndex && !bufferFull )
                {
                    ret = default(T);
                    return false;
                }

                ret = buffer[readIndex];
                readIndex = (++readIndex) % size;
                bufferFull = false;
                return true;
            }
        }
    }
}
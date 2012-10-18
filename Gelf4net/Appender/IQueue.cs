using System;

namespace AsynchronousLog4NetAppenders
{
    public interface IQueue<T>
    {
        void Enqueue( T item );

        Boolean TryDequeue( out T ret );
    }
}
using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using AsynchronousLog4NetAppenders;
using log4net.Core;
using log4net.Util;

namespace Esilog.Gelf4net.Appender
{
    internal class Gelf4NetAsyncAppender : Gelf4NetAppender
    {
        private RingBuffer<LoggingEvent> pendingAppends;

        private readonly ManualResetEvent manualResetEvent;

        private Boolean shuttingDown;
        private Boolean hasFinished;
        private Boolean forceStop;
        private Boolean logBufferOverflow;
        private Int32 bufferOverflowCounter;
        
        private DateTime lastLoggedBufferOverflow;

        private Int32 queueSizeLimit = 1000;

        public Int32 QueueSizeLimit
        {
            get
            {
                return queueSizeLimit;
            }
            set
            {
                queueSizeLimit = value;
            }
        }

        public Gelf4NetAsyncAppender()
        {
            manualResetEvent = new ManualResetEvent( false );
        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
            pendingAppends = new RingBuffer<LoggingEvent>( QueueSizeLimit );
            pendingAppends.BufferOverflow += OnBufferOverflow;
            StartAppendTask();
        }

        protected override void Append( LoggingEvent[] loggingEvents )
        {
            Array.ForEach( loggingEvents, Append );
        }

        protected override void Append( LoggingEvent loggingEvent )
        {
            if( FilterEvent( loggingEvent ) )
            {
                pendingAppends.Enqueue( loggingEvent );
            }
        }

        protected override void OnClose()
        {
            shuttingDown = true;
            manualResetEvent.WaitOne( TimeSpan.FromSeconds( 5 ) );
            var loggerType = GetType();

            if( !hasFinished )
            {
                forceStop = true;
                base.Append(
                    new LoggingEvent(
                        new LoggingEventData
                        {
                            Level = Level.Error,
                            Message =
                                "Unable to clear out the Gelf4NetAsyncAppender buffer in the allotted time, forcing a shutdown",
                            TimeStamp = DateTime.UtcNow,
                            Identity = String.Empty,
                            ExceptionString = String.Empty,
                            UserName = WindowsIdentity.GetCurrent() != null
                                           ? WindowsIdentity.GetCurrent().Name
                                           : String.Empty,
                            Domain = AppDomain.CurrentDomain.FriendlyName,
                            ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                            LocationInfo =
                                new LocationInfo(
                                loggerType != null ? loggerType.Name : "Unknown logger", "LogAppenderError",
                                "Gelf4NetAsyncAppender.cs", "86" ),
                            LoggerName = loggerType != null ? loggerType.FullName : "Unknown logger",
                            Properties = new PropertiesDictionary(),
                        } )
                    );
            }

            base.OnClose();
        }

        private void StartAppendTask()
        {
            if( !shuttingDown )
            {
                var appendTask = new Task( AppendLoggingEvents, TaskCreationOptions.LongRunning );
                appendTask.LogErrors( LogAppenderError ).ContinueWith( x => StartAppendTask() ).LogErrors(
                    LogAppenderError ).ContinueWith( task=>
                                                     {
                                                         if( task.Exception != null )
                                                         {
                                                             
                                                         }
                                                     },TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously );
                appendTask.Start();
            }
        }

        private void LogAppenderError( String logMessage, Exception exception )
        {
            var loggerType = GetType();
            base.Append(
                new LoggingEvent(
                    new LoggingEventData
                    {
                        Level = Level.Error,
                        Message = "Appender exception: " + logMessage,
                        TimeStamp = DateTime.UtcNow,
                        Identity = String.Empty,
                        ExceptionString = exception != null ? exception.ToString() : String.Empty,
                        UserName =
                            WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : String.Empty,
                        Domain = AppDomain.CurrentDomain.FriendlyName,
                        ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                        LocationInfo =
                            new LocationInfo(
                            loggerType != null ? loggerType.Name : "Unknown logger", "LogAppenderError",
                            "Gelf4NetAsyncAppender.cs", "122" ),
                        LoggerName = loggerType != null ? loggerType.FullName : "Unknown logger",
                        Properties = new PropertiesDictionary(),
                    } ) );
        }

        private void AppendLoggingEvents()
        {
            LoggingEvent loggingEventToAppend;
            while( !shuttingDown )
            {
                if( logBufferOverflow )
                {
                    LogBufferOverflowError();
                    logBufferOverflow = false;
                    bufferOverflowCounter = 0;
                    lastLoggedBufferOverflow = DateTime.UtcNow;
                }

                while( !pendingAppends.TryDequeue( out loggingEventToAppend ) )
                {
                    Thread.Sleep( 10 );
                    if( shuttingDown )
                    {
                        break;
                    }
                }

                if( loggingEventToAppend == null )
                {
                    continue;
                }

                try
                {
                    base.Append( loggingEventToAppend );
                }
                catch
                {
                    // No faults should be propagated outside task.
                }
            }

            while( pendingAppends.TryDequeue( out loggingEventToAppend ) && !forceStop )
            {
                try
                {
                    base.Append( loggingEventToAppend );
                }
                catch
                {
                    // No faults should be propagated outside task.
                }
            }
            hasFinished = true;
            manualResetEvent.Set();
        }

        private void LogBufferOverflowError()
        {
            var loggerType = GetType();
            base.Append(
                new LoggingEvent(
                    new LoggingEventData
                    {
                        Level = Level.Error,
                        Message =
                            String.Format(
                                "Buffer overflow. {0} logging events have been lost in the last 30 seconds. [QueueSizeLimit: {1}]",
                                bufferOverflowCounter, QueueSizeLimit ),
                        TimeStamp = DateTime.UtcNow,
                        Identity = String.Empty,
                        ExceptionString = String.Empty,
                        UserName =
                            WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : String.Empty,
                        Domain = AppDomain.CurrentDomain.FriendlyName,
                        ThreadName = Thread.CurrentThread.ManagedThreadId.ToString(),
                        LocationInfo =
                            new LocationInfo(
                            loggerType != null ? loggerType.Name : "Unknown logger", "LogAppenderError",
                            "Gelf4NetAsyncAppender.cs", "122" ),
                        LoggerName = loggerType != null ? loggerType.FullName : "Unknown logger",
                        Properties = new PropertiesDictionary(),
                    } ) );
        }

        private void OnBufferOverflow( Object sender, EventArgs eventArgs )
        {
            bufferOverflowCounter++;
            if( logBufferOverflow == false )
            {
                if( lastLoggedBufferOverflow < DateTime.UtcNow.AddSeconds( -30 ) )
                {
                    logBufferOverflow = true;
                }
            }
        }
    }
}
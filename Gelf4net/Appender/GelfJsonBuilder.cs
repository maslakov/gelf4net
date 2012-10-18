using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Esilog.Gelf4net.Appender
{
    internal class GelfJsonBuilder
    {
        private static Int32 SHORT_MESSAGE_LENGTH = 250;
        private const String GELF_VERSION = "1.0";
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0,
                                                      DateTimeKind.Utc);


        internal String BuildFromLoggingEvent(
            String message, log4net.Core.LoggingEvent loggingEvent, String hostName, String facility,
            Boolean isConfiguredToIncludeLocationInformation, Dictionary<String, String> innerAdditionalFields )
        {
            var fullMessage = GetFullMessage( message, loggingEvent );
            var gelfMessage = new GelfMessage
            {
                Facility = (facility ?? "GELF"),
                File =
                    isConfiguredToIncludeLocationInformation ? loggingEvent.LocationInformation.FileName : String.Empty,
                FullMesage = fullMessage,
                Host = hostName,
                Level = GetSyslogSeverity( loggingEvent.Level ),
                Line =
                    isConfiguredToIncludeLocationInformation
                        ? loggingEvent.LocationInformation.LineNumber
                        : String.Empty,
                ShortMessage = GetShortMessage( fullMessage ),
                TimeStamp = GetUnixTimestamp( loggingEvent.TimeStamp ),
                Version = GELF_VERSION,
            };

            return GetGelfJsonMessage( loggingEvent, innerAdditionalFields, gelfMessage );
        }

        private Decimal GetUnixTimestamp(DateTime time)
        {
            var unixRef = new DateTime(1970, 1, 1, 0, 0, 0);
            return (Decimal)(time.Ticks - unixRef.Ticks) / 10000000;
        }

        private String GetFullMessage( String message, log4net.Core.LoggingEvent loggingEvent )
        {
            var fullMessage = message;
            if( loggingEvent.ExceptionObject != null )
            {
                fullMessage = String.Format(
                    "{0} - {1}. {2}. {3}.", fullMessage, loggingEvent.ExceptionObject.Source,
                    loggingEvent.ExceptionObject.Message, loggingEvent.ExceptionObject.StackTrace );
            }
            return fullMessage;
        }

        private static String GetShortMessage( String fullMessage )
        {
            return (fullMessage.Length > SHORT_MESSAGE_LENGTH)
                       ? fullMessage.Substring( 0, SHORT_MESSAGE_LENGTH - 1 )
                       : fullMessage;
        }

        private String GetGelfJsonMessage(
            log4net.Core.LoggingEvent loggingEvent, Dictionary<String, String> innerAdditionalFields,
            GelfMessage gelfMessage )
        {
            var gelfJsonMessage = JsonConvert.SerializeObject( gelfMessage );
            var jsonObject = JObject.Parse( gelfJsonMessage );
            AddInnerAdditionalFields( jsonObject, innerAdditionalFields );
            AddLoggingEventAdditionalFields( jsonObject, loggingEvent );
            return jsonObject.ToString();
        }

        private void AddInnerAdditionalFields( JObject jsonObject, Dictionary<String, String> innerAdditionalFields )
        {
            if( innerAdditionalFields == null )
            {
                return;
            }
            foreach( var item in innerAdditionalFields )
            {
                AddAdditionalFields( item.Key, item.Value, jsonObject );
            }
        }

        private void AddLoggingEventAdditionalFields( JObject jsonObject, LoggingEvent loggingEvent )
        {
            if( loggingEvent.Properties == null )
            {
                return;
            }
            foreach( DictionaryEntry item in loggingEvent.Properties )
            {
                var key = item.Key as String;
                if( key != null )
                {
                    AddAdditionalFields( key, item.Value as String, jsonObject );
                }
            }
        }

        private void AddAdditionalFields( String key, String value, JObject jsonObject )
        {
            if( key == null )
            {
                return;
            }

            if( !key.StartsWith( "_" ) )
            {
                key = String.Format( "_{0}", key );
            }

            if( key != "_id" )
            {
                key = Regex.Replace( key, "[\\W]", "" );
                jsonObject.Add( key, value );
            }
        }

        private Int32 GetSyslogSeverity( log4net.Core.Level level )
        {
            if( level == log4net.Core.Level.Alert )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Alert;
            }

            if( level == log4net.Core.Level.Critical || level == log4net.Core.Level.Fatal )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Critical;
            }

            if( level == log4net.Core.Level.Debug )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Debug;
            }

            if( level == log4net.Core.Level.Emergency )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Emergency;
            }

            if( level == log4net.Core.Level.Error )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Error;
            }

            if( level == log4net.Core.Level.Fine
                || level == log4net.Core.Level.Finer
                || level == log4net.Core.Level.Finest
                || level == log4net.Core.Level.Info
                || level == log4net.Core.Level.Off )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Informational;
            }

            if( level == log4net.Core.Level.Notice
                || level == log4net.Core.Level.Verbose
                || level == log4net.Core.Level.Trace )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Notice;
            }

            if( level == log4net.Core.Level.Severe )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Emergency;
            }

            if( level == log4net.Core.Level.Warn )
            {
                return (Int32)LocalSyslogAppender.SyslogSeverity.Warning;
            }

            return (Int32)LocalSyslogAppender.SyslogSeverity.Debug;
        }
    }
}
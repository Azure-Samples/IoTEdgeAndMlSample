//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace turbofanRouter
{
    public class TurbofanMessage
    {
        private const string CorrelationIdName = "CorrelationId";
        private const string ConnectionDeviceIdName = "ConnectionDeviceId";
        private const string CreationTimeUtcName = "CreationTimeUtc";
        private const string EnqueuedTimeUtcName = "EnqueuedTimeUtc";
        private const string PredictedRulKeyName = "PredictedRul";
        private const string CycleTimeKeyName = "CycleTime";
        private const string DeviceIdKeyName = "DeviceId";

        private string messageJson;
        private Dictionary<string, double> parsedMessage;
        private Dictionary<string, string> messageProperties = new Dictionary<string, string>();

        /// <summary>
        /// Parsed dictionary of values from the device message
        /// </summary>
        public Dictionary<string, double> Message
        {
            get
            {
                if (parsedMessage == null)
                {
                    parsedMessage = new Dictionary<string, double>();
                }
                return parsedMessage;
            }
        }

        /// <summary>
        /// Correlation Id for tracking message through various modules
        /// </summary>
        /// <value></value>
        public string CorrelationId { get => messageProperties[CorrelationIdName]; }

        /// <summary>
        /// DeviceId corresponding to the IoT Hub Device that created the message
        /// </summary>
        public string DeviceId { get => messageProperties[ConnectionDeviceIdName]; }

        public DateTimeOffset CreationTimeUtc
        {
            get
            {
                DateTimeOffset.TryParse(messageProperties[CreationTimeUtcName], out DateTimeOffset creationTime);
                return creationTime;
            }
        }

        public DateTimeOffset EnqueuedTimeUtc
        {
            get
            {
                DateTimeOffset.TryParse(messageProperties[EnqueuedTimeUtcName], out DateTimeOffset enqueueTime);
                return enqueueTime;
            }
        }

        /// <summary>
        /// Indicates that the message has been received back from the classifier 
        /// module and has a Remaining Useful Life classification
        /// </summary>
        public bool HasRemainingRul { get => Message.ContainsKey(PredictedRulKeyName); }

        /// <summary>
        /// Used to create a Message to send through the hub reflecting current state of 
        /// this object
        /// </summary>
        public Message CreateClassifierMessage()
        {
            if (parsedMessage.Count == 0)
            {
                throw new ApplicationException("parsedMessage is empty; no message to send");
            }
            
            try
            {
                Message message = CreateDeviceClientMessage(ToJson(parsedMessage));
                return message;
            }
            catch
            {
                Logger.Log("Couldn't create device message from turbofanMessage", LogSeverity.Error);
                throw;
            }
        }

        /// <summary>
        /// Creates a message to send upstream with a minimum set of properties
        /// </summary>
        public Message CreateRemainingLifeMessage()
        {
            if (parsedMessage.Count == 0)
            {
                throw new ApplicationException("parsedMessage is empty, no message to send");
            }

            try
            {
                Dictionary<string, object> rulMessageData = GetRulMessageData();
                Message message = CreateDeviceClientMessage(ToJson(rulMessageData));
                return message;
            }
            catch
            {
                Logger.Log("Couldn't create RUL message from turbofanMessage", LogSeverity.Error);
                throw;
            }
        }

        private Message CreateDeviceClientMessage(string messageData)
        {
            Message message = new Message(Encoding.UTF8.GetBytes(messageData));
            
            // IMPORTANT
            // set content encoding and type to enable routing by message body
            message.ContentEncoding = "utf-8";
            message.ContentType = "application/json";

            copyPropertiesToMessage(message);
            return message;
        }

        public Message CreateAvroWriterMessage()
        {
            try
            {
                var avroMessage = new AvroPersistMessage(this);
                Message message = CreateDeviceClientMessage(ToJson(avroMessage));
                return message;
            }
            catch
            {
                Logger.Log("Couldn't create AvroMessage from turbofanMessage", LogSeverity.Error);
                throw;
            }
        }

        public TurbofanMessage(Message message)
        {
            byte[] messageBytes = message.GetBytes();
            if (messageBytes == null)
            {
                Logger.Log("Received message was empty", LogSeverity.Warning);
                return;
            }

            try
            {
                messageJson = Encoding.UTF8.GetString(messageBytes);
                parsedMessage = FromJson(messageJson);
                AddMessageProperties(message);

                Logger.Log($"Received message: {messageJson}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load message: {messageJson ?? "<<null>>"}", LogSeverity.Error);
                Logger.Log($"Got exception: {ex.Message}", LogSeverity.Error);
            }
        }


        private void copyPropertiesToMessage(Message message)
        {
            foreach (KeyValuePair<string, string> kvp in messageProperties)
            {
                message.Properties.Add(kvp.Key, kvp.Value);
            }

            if (message.Properties.ContainsKey(CorrelationIdName))
            {
                message.CorrelationId = message.Properties[CorrelationIdName];
            }
        }

        private Dictionary<string, object> GetRulMessageData()
        {
            Dictionary<string, Object> rulMessageData = new Dictionary<string, object>();
            rulMessageData.Add(ConnectionDeviceIdName, messageProperties[ConnectionDeviceIdName]);
            rulMessageData.Add(CorrelationIdName, messageProperties[CorrelationIdName]);
            rulMessageData.Add(PredictedRulKeyName, parsedMessage[PredictedRulKeyName]);
            rulMessageData.Add(CycleTimeKeyName, parsedMessage[CycleTimeKeyName]);

            return rulMessageData;
        }

        private void AddMessageProperties(Message message)
        {
            foreach (var prop in message.Properties)
            {
                messageProperties.Add(prop.Key, prop.Value);
            }

            if (message.CreationTimeUtc != null
                && !messageProperties.ContainsKey(CreationTimeUtcName))
            {
                messageProperties.Add(CreationTimeUtcName, message.CreationTimeUtc.ToString());
            }

            if (message.EnqueuedTimeUtc != null
                && !messageProperties.ContainsKey(EnqueuedTimeUtcName))
            {
                messageProperties.Add(EnqueuedTimeUtcName, message.EnqueuedTimeUtc.ToString());
            }

            if (!messageProperties.ContainsKey(CorrelationIdName))
            {
                string correlationId = String.IsNullOrWhiteSpace(message.CorrelationId)
                    ? Guid.NewGuid().ToString()
                    : message.CorrelationId;
                messageProperties.Add(CorrelationIdName, correlationId);
            }

            if (!String.IsNullOrWhiteSpace(message.ConnectionDeviceId)
                && !messageProperties.ContainsKey(ConnectionDeviceIdName))
            {
                messageProperties.Add(ConnectionDeviceIdName, message.ConnectionDeviceId);
            }
        }

        private static Dictionary<string, double> FromJson(string json)
        {
            // classifier will return an array of length 1 so just strip the [] when present
            json = json.Trim(new char[] { '[', ']' });
            return JsonConvert.DeserializeObject<Dictionary<string, double>>(json, JsonConverter.Settings);
        }

        private static string ToJson(Dictionary<string, double> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary, JsonConverter.Settings);
        }

        private static string ToJson(Dictionary<string, object> dictionary)
        {
            return JsonConvert.SerializeObject(dictionary, JsonConverter.Settings);
        }

        private static string ToJson(AvroPersistMessage avroMessage)
        {
            return JsonConvert.SerializeObject(avroMessage, JsonConverter.Settings);
        }
    }

    internal static class JsonConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class AvroPersistMessage
    {
        public string CorrelationId;

        public string ConnectionDeviceId;

        public DateTimeOffset CreationTimeUtc;

        public DateTimeOffset EnqueuedTimeUtc;

        public Dictionary<string, double> Body;

        internal AvroPersistMessage(TurbofanMessage turbofanMessage)
        {
            CorrelationId = turbofanMessage.CorrelationId;
            ConnectionDeviceId = turbofanMessage.DeviceId;
            CreationTimeUtc = turbofanMessage.CreationTimeUtc;
            EnqueuedTimeUtc = turbofanMessage.EnqueuedTimeUtc;
            Body = turbofanMessage.Message;
        }
    }
}
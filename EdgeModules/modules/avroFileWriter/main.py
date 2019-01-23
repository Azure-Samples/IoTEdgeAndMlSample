#*********************************************************
#
# Copyright (c) Microsoft. All rights reserved.
# This code is licensed under the MIT License (MIT).
# THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
# ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
# IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
# PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
#
#*********************************************************

import random
import time
import sys
import iothub_client
import filemanager as fm
import json
from uploader import Uploader
from configreader import ConfigReader

# pylint: disable=E0611
from iothub_client import IoTHubModuleClient, IoTHubClientError, IoTHubTransportProvider
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError

AVRO_FILE_ROOT_DIR = "/avrofiles"
CONFIG_FILE_PATH = "/app/iotconfig/config.yaml"
MESSAGE_TIMEOUT_MILLISECONDS = 10000
RECEIVE_CALLBACKS = 0
SEND_CALLBACKS = 0

INPUT_ENDPOINT_NAME = "avroModuleInput"

# Currently only MQTT is supported.
PROTOCOL = IoTHubTransportProvider.MQTT

# Callback received when the message that we're forwarding is processed.
def send_confirmation_callback(message, result, user_context):
    global SEND_CALLBACKS
    print ( "Confirmation[%d] received for message with result = %s" % (user_context, result) )
    map_properties = message.properties()
    key_value_pair = map_properties.get_internals()
    print ( "    Properties: %s" % key_value_pair )
    SEND_CALLBACKS += 1
    print ( "    Total calls confirmed: %d" % SEND_CALLBACKS )


# receives message from the input INPUT_ENDPOINT_NAME corresponds to route:
# "FROM /messages/modules/turbofanRouter/outputs/writeAvro INTO BrokeredEndpoint(\"/modules/avroFileWriter/inputs/avroModuleInput\")"
def avro_writer_message_callback(message, hubManager):
    message_buffer = message.get_bytearray()
    size = len(message_buffer)
    message_data = json.loads(message_buffer[:size].decode('utf-8'))
    print("Message received: %s" % message_data)
    fm.write_data_to_file(AVRO_FILE_ROOT_DIR, message_data)
    return IoTHubMessageDispositionResult.ACCEPTED

class HubManager(object):
    def __init__(
            self,
            protocol=IoTHubTransportProvider.MQTT):
        self.client_protocol = protocol
        self.client = IoTHubModuleClient()
        self.client.create_from_environment(protocol)

        # set the time until a message times out
        self.client.set_option("messageTimeout", MESSAGE_TIMEOUT_MILLISECONDS)
        
        self.client.set_message_callback(INPUT_ENDPOINT_NAME, avro_writer_message_callback, self)

    # Forwards the message received onto the next stage in the process.
    def forward_event_to_output(self, outputQueueName, event, send_context):
        self.client.send_event_async(
            outputQueueName, event, send_confirmation_callback, send_context)

def initialize_upload(protocol):
    config_reader = ConfigReader(CONFIG_FILE_PATH)
    uploader = Uploader(60, 
                        AVRO_FILE_ROOT_DIR, 
                        config_reader.get_connection_string())
    uploader.run_schedule()

def main(protocol):
    try:
        print ( "\nPython %s\n" % sys.version )
        print ( "Initialize avroFileWriter module" )

        hub_manager = HubManager(protocol)
        initialize_upload(protocol)

        print ( "Started the avroFileWriterModule using protocol %s..." % hub_manager.client_protocol )
        print ( "Waiting for messages. ")

        while True:
            time.sleep(1)

    except IoTHubError as iothub_error:
        print ( "Unexpected error %s from IoTHub" % iothub_error )
        return
    except KeyboardInterrupt:
        print ( "avroFileWriter module stopped" )

if __name__ == '__main__':
    main(PROTOCOL)
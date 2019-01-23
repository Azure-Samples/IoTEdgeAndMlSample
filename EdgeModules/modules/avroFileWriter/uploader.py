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

import glob
import json
import os
import sched
import time

# pylint: disable=E0611
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider
from iothub_client import IoTHubModuleClient, IoTHubClientError, IoTHubTransportProvider
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError

BLOB_CALLBACKS = 0
# message timeout in seconds
MESSAGE_TIMEOUT = 10000
# number of seconds to wait after last modification of a file before uploading
MODIFIED_FILE_TIMEOUT = 600

def blob_upload_conf_callback(result, user_context):
    global BLOB_CALLBACKS
    BLOB_CALLBACKS += 1
    if result == 0:
        # file uploaded so remove it
        os.remove(user_context)
        print ("Removed file %s after successful upload" % user_context)
    else:
        print ("Blob upload for[%s] received for message with result = %s" % (user_context, result))
    print ("Total calls confirmed: %d" % BLOB_CALLBACKS)



class Uploader(object):
    def __init__(self, 
                 publish_interval,
                 upload_files_root_dir,
                 connection_string):
        self.interval = publish_interval
        self.file_root_dir = upload_files_root_dir
        # create the scheduler
        self.s = sched.scheduler(time.time, time.sleep)
        # create the client
        self.client = IoTHubClient(connection_string, IoTHubTransportProvider.MQTT)
        self.client.set_option("messageTimeout", MESSAGE_TIMEOUT)

    def __upload_to_blob(self, destination_filename, source, size, user_context):
        # upload blob using IotHubClient
        self.client.upload_blob_async(
            destination_filename, source, size,
            blob_upload_conf_callback, user_context)

    def __upload_avro_files(self, sc): 
        # find all of the files written by the module
        file_search = os.path.join(self.file_root_dir, "**/*.avro")
        files = glob.glob(file_search,recursive=True)
        
        for file in files:
            # upload only files not modified in the last 10 minutes
            if (time.time() - os.path.getmtime(file)) > MODIFIED_FILE_TIMEOUT: 
                f = open(file, "rb")
                content = f.read()
                print ("Starting upload for file: %s" % file)
                self.__upload_to_blob(file[1:], content, len(content), file)

        # schedule this method to be called by scheduler after self.interval
        self.s.enter(self.interval, 1, self.__upload_avro_files, (sc,))

    def run_schedule(self):
        # __upload_avro_files to run after self.interval
        self.s.enter(self.interval, 1, self.__upload_avro_files, (self.s,))
        self.s.run()
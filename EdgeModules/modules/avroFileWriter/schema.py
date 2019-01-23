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

from fastavro import parse_schema


def get_parsed_schema():
    schema = {
        'name': 'EngineData',
        'type': 'record',
        'namespace': 'turbofan',
        'fields': [
            {'name': 'CorrelationId', 'type': 'string'},
            {'name': 'ConnectionDeviceId', 'type': 'string'},
            {'name': 'CreationTimeUtc', 'type': 'string'},
            {'name': 'EnqueuedTimeUtc', 'type': 'string'},
            {'name': 'Body', 'type':
                {'name': 'Body', 'type': 'record',
                        'fields': [
                            {'name': 'Sensor13', 'type': 'float'},
                            {'name': 'OperationalSetting1', 'type': 'float'},
                            {'name': 'Sensor6', 'type': 'float'},
                            {'name': 'Sensor11', 'type': 'float'},
                            {'name': 'Sensor9', 'type': 'float'},
                            {'name': 'Sensor4', 'type': 'float'},
                            {'name': 'Sensor14', 'type': 'float'},
                            {'name': 'Sensor18', 'type': 'float'},
                            {'name': 'Sensor12', 'type': 'float'},
                            {'name': 'Sensor2', 'type': 'float'},
                            {'name': 'Sensor17', 'type': 'float'},
                            {'name': 'OperationalSetting3', 'type': 'float'},
                            {'name': 'Sensor1', 'type': 'float'},
                            {'name': 'OperationalSetting2', 'type': 'float'},
                            {'name': 'Sensor20', 'type': 'float'},
                            {'name': 'DeviceId', 'type': 'float'},
                            {'name': 'Sensor5', 'type': 'float'},
                            {'name': 'PredictedRul', 'type': 'float'},
                            {'name': 'Sensor8', 'type': 'float'},
                            {'name': 'Sensor16', 'type': 'float'},
                            {'name': 'CycleTime', 'type': 'float'},
                            {'name': 'Sensor21', 'type': 'float'},
                            {'name': 'Sensor15', 'type': 'float'},
                            {'name': 'Sensor3', 'type': 'float'},
                            {'name': 'Sensor10', 'type': 'float'},
                            {'name': 'Sensor7', 'type': 'float'},
                            {'name': 'Sensor19', 'type': 'float'}
                        ]
                }
            }
        ]
    }
    return parse_schema(schema)

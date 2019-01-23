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

import click
from fastavro import reader
from os.path import isfile
import json


@click.command()
@click.argument('file', required=True, type=click.Path(exists=True))
def cli(file):
    """
    \b
    Read Azure IoT Hub Avro files
    Returns the content of Avro file FILE as a json string.

    Examples:

        \b
        hubavroreader c:\\temp\\05
        hubavroreader c:\\temp\\25.avro
    """

    if (isfile(file)):
        messages = load_avro_file(file)
        click.echo(json.dumps(messages, indent=4, sort_keys=True))
        return
    click.echo('Could not find file %s' % file)


def load_avro_file(avro_file_name):
    with open(avro_file_name, 'rb') as fo:
        rows = []
        avro_reader = reader(fo)
        for record in avro_reader:
            row = decode_dictionary_values(record)
            rows.append(row)
        return rows

def decode_dictionary_values(record):
    if (type(record) is not dict):
        return 
    for key, value in record.items():
        if (type(value) is bytes):
            record[key] = json.loads(value.decode())
        if (type(value) is dict):
            decode_dictionary_values(value)
    return record
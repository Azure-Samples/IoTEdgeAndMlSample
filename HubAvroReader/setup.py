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

"""
Read Azure IoT Hub messages from Avro files
"""
from setuptools import find_packages, setup

dependencies = ['click','fastavro']

setup(
    name='hubavroreader',
    version='0.0.1',
    url='https://github.com/Azure-Samples/IoTEdgeAndMlSample',
    license='MIT',
    author='microsoft',
    description='Read Azure IoT Hub messages from Avro files',
    packages=find_packages(),
    include_package_data=True,
    zip_safe=False,
    platforms='any',
    install_requires=dependencies,
    entry_points={
        'console_scripts': [
            'hubavroreader = hubavroreader.cli:cli',
        ],
    },
    classifiers=[
        'Development Status :: 3 - Alpha',
        'License :: OSI Approved :: MIT License',
    ]
)

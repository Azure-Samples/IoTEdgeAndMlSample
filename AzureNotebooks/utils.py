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
import pandas as pd

from fastavro import reader
from os.path import isfile
from multiprocessing.dummy import Pool as ThreadPool 

# parse connectionDeviceId and return the int part
# (e.g. Client_1 becomes 1)
def get_unit_num (unit_record):
    unit = unit_record["connectionDeviceId"]
    return int(unit.split('_')[1])

# create data row from avro file record
def load_cycle_row(record):
    json_body = record["Body"].decode()
    row = json.loads(json_body)
    row.update({'Unit': get_unit_num(record["SystemProperties"])})
    row.update({'QueueTime': pd.to_datetime(record["EnqueuedTimeUtc"])})
    return row

# add row to data frame
def append_df(base_df, append_df):
    if(base_df is None):
        base_df = pd.DataFrame(append_df)
    else:
        base_df = base_df.append(append_df, ignore_index=True)
    return base_df

# sort rows and columns in dataframe
def sort_and_index(index_data):
    #sort rows and reset index
    index_data.sort_values(by=['Unit', 'CycleTime'], inplace=True)
    index_data.reset_index(drop=True, inplace=True)
    
    #fix up column sorting for convenience in notebook
    sorted_cols = (["Unit","CycleTime", "QueueTime"] 
                   + ["OperationalSetting"+str(i) for i in range(1,4)] 
                   + ["Sensor"+str(i) for i in range(1,22)])

    return index_data[sorted_cols]

# load data from an avro file and return a dataframe
def load_avro_file(avro_file_name):
    with open(avro_file_name, 'rb') as fo:
        file_df = None
        avro_reader = reader(fo)
        print ("load records from file: %s" % avro_file_name)
        for record in avro_reader:
            row = load_cycle_row(record)
            file_df = append_df(base_df=file_df, append_df=[row])
        return file_df

# load data from all avro files in given dir 
def load_avro_directory(avro_dir_name):
    lst = glob.iglob(avro_dir_name, recursive=True)
    files = [x for x in lst if isfile(x)]
    pool = ThreadPool(4)
    results = pool.map(load_avro_file, files)
    pool.close()
    pool.join()

    dir_df = None
    for df in results:
        dir_df = append_df(base_df=dir_df, append_df=df)
    print("loaded %d records" % dir_df.shape[0])
    return sort_and_index(dir_df)

# add max cycle to each row in the data
def add_maxcycle(data_frame):
    # cleanup column if it already exists
    if 'MaxCycle' in data_frame.columns:
        data_frame.drop('MaxCycle', axis=1, inplace=True)

    total_cycles = data_frame.groupby(['Unit']).agg({'CycleTime' : 'max'}).reset_index()
    total_cycles.rename(columns = {'CycleTime' : 'MaxCycle'}, inplace = True)
    return data_frame.merge(total_cycles, how = 'left', left_on = 'Unit', right_on = 'Unit')

# return a remaining useful life class based on RUL
def classify_rul(rul):
     if (rul <= 25):
          return 'F25'
     elif (rul <= 75):
          return 'F75'
     elif (rul <= 150):
          return 'F150'
     else:
          return 'Full'
    
# add remaining useful life and remaing useful life class
# to each row in the data
def add_rul(data_frame):
    data_frame = add_maxcycle(data_frame)
    
    if 'RUL' in data_frame.columns:
        data_frame.drop('RUL', axis=1, inplace=True)
    data_frame['RUL'] = data_frame.apply(lambda r: int(r['MaxCycle'] - r['CycleTime']), axis = 1)

    if 'RulClass' in data_frame.columns:
        data_frame.drop('RulClass', axis=1, inplace=True)
    data_frame['RulClass'] = data_frame.apply(lambda r: classify_rul(r['RUL']), axis = 1)
    
    return data_frame


import yaml

class ConfigReader(object):
    def get_connection_string(self):
        return self.config['provisioning']['device_connection_string']
    
    def __load_config(self, config_file_path):
        with open(config_file_path, "r") as stream:
            try: 
                return yaml.safe_load(stream)
            except yaml.YAMLError as exc:
                print ("failed to load config file got exception %s" % exc )

    def __init__(self,
                 config_file_path):
        self.config = self.__load_config(config_file_path)
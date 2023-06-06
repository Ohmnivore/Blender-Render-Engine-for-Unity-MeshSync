from abc import ABC, abstractmethod


class Message:

    def __init__(self, msg_type, data):
        self.type = msg_type
        self.data = data


class IMessage(ABC):

    @classmethod
    @abstractmethod
    def get_type(self):
        pass


class IMessageHandle(ABC):

    @abstractmethod
    def try_parse(self, cls):
        pass

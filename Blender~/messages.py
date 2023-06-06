from enum import IntEnum, unique
from .comms.message import IMessage


@unique
class ServerMessageType(IntEnum):

    Heartbeat = 0
    DomainReload = 1


@unique
class ClientMessageType(IntEnum):

    ViewUpdated = 0
    ViewDestroyed = 1
    ObjectVisibility = 2


class Heartbeat(IMessage):

    @staticmethod
    def get_type():
        return ClientMessageType.Heartbeat

    def __init__(self):
        pass

    def initialize(self):
        return self

    def serialize(self):
        data = self.__dict__
        return data

    def post_deserialize(self):
        pass


class DomainReload(IMessage):

    @staticmethod
    def get_type():
        return ServerMessageType.DomainReload

    def __init__(self):
        pass

    def initialize(self):
        return self

    def serialize(self):
        data = self.__dict__
        return data

    def post_deserialize(self):
        pass


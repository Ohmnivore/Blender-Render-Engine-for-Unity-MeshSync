from abc import ABC, abstractmethod


class INetClient(ABC):

    @abstractmethod
    def add_connected_callback(self, cb):
        pass

    @abstractmethod
    def remove_connected_callback(self, cb):
        pass

    @abstractmethod
    def add_connection_failed_callback(self, cb):
        pass

    @abstractmethod
    def remove_connection_failed_callback(self, cb):
        pass

    @abstractmethod
    def add_disconnected_callback(self, cb):
        pass

    @abstractmethod
    def remove_disconnected_callback(self, cb):
        pass

    @abstractmethod
    def add_callback(self, cb):
        pass

    @abstractmethod
    def remove_callback(self, cb):
        pass

    @abstractmethod
    def send(self, msg):
        pass
